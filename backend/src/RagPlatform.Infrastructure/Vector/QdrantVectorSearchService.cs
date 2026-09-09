using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RagPlatform.Application.Common.Interfaces;
using RagPlatform.Application.Common.Models;
using RagPlatform.Infrastructure.Options;

namespace RagPlatform.Infrastructure.Vector;

/// <summary>
/// Vector store backed by Qdrant over its stable HTTP REST API. Point ids are
/// the chunk GUIDs; payloads carry enough metadata to render citations without
/// a second database round-trip.
/// </summary>
public sealed class QdrantVectorSearchService : IVectorSearchService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private readonly QdrantOptions _options;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<QdrantVectorSearchService> _logger;

    public QdrantVectorSearchService(
        HttpClient http,
        IOptions<QdrantOptions> options,
        IEmbeddingService embeddingService,
        ILogger<QdrantVectorSearchService> logger)
    {
        _http = http;
        _options = options.Value;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    private string Collection => _options.CollectionName;

    public async Task EnsureCollectionAsync(CancellationToken cancellationToken = default)
    {
        using var existing = await _http.GetAsync($"collections/{Collection}", cancellationToken);
        if (existing.StatusCode == HttpStatusCode.OK)
        {
            return;
        }

        var body = new
        {
            vectors = new
            {
                size = _embeddingService.Dimensions,
                distance = "Cosine"
            }
        };

        using var response = await _http.PutAsJsonAsync(
            $"collections/{Collection}", body, JsonOptions, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Failed to create Qdrant collection ({(int)response.StatusCode}): {error}");
        }

        _logger.LogInformation(
            "Created Qdrant collection '{Collection}' with dimension {Dim}.",
            Collection, _embeddingService.Dimensions);
    }

    public async Task UpsertAsync(
        IReadOnlyList<VectorRecord> records,
        CancellationToken cancellationToken = default)
    {
        if (records.Count == 0)
        {
            return;
        }

        var payload = new
        {
            points = records.Select(r => new
            {
                id = r.Id.ToString(),
                vector = r.Vector,
                payload = new Dictionary<string, object?>
                {
                    ["documentId"] = r.DocumentId.ToString(),
                    ["fileName"] = r.FileName,
                    ["chunkIndex"] = r.ChunkIndex,
                    ["pageNumber"] = r.PageNumber,
                    ["content"] = r.Content
                }
            })
        };

        using var response = await _http.PutAsJsonAsync(
            $"collections/{Collection}/points?wait=true", payload, JsonOptions, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Qdrant upsert failed ({(int)response.StatusCode}): {error}");
        }
    }

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(
        float[] queryVector,
        int limit,
        Guid? documentId = null,
        CancellationToken cancellationToken = default)
    {
        object? filter = documentId is null
            ? null
            : new
            {
                must = new[]
                {
                    new
                    {
                        key = "documentId",
                        match = new { value = documentId.Value.ToString() }
                    }
                }
            };

        var payload = new
        {
            vector = queryVector,
            limit,
            with_payload = true,
            filter
        };

        using var response = await _http.PostAsJsonAsync(
            $"collections/{Collection}/points/search", payload, JsonOptions, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Qdrant search failed ({(int)response.StatusCode}): {error}");
        }

        var parsed = await response.Content.ReadFromJsonAsync<SearchResponse>(
            JsonOptions, cancellationToken);

        if (parsed?.Result is null)
        {
            return Array.Empty<SearchHit>();
        }

        var hits = new List<SearchHit>(parsed.Result.Count);
        foreach (var point in parsed.Result)
        {
            var p = point.Payload;
            hits.Add(new SearchHit(
                ChunkId: Guid.TryParse(point.Id?.ToString(), out var cid) ? cid : Guid.Empty,
                DocumentId: Guid.TryParse(GetString(p, "documentId"), out var did) ? did : Guid.Empty,
                FileName: GetString(p, "fileName") ?? string.Empty,
                ChunkIndex: GetInt(p, "chunkIndex"),
                PageNumber: GetInt(p, "pageNumber"),
                Content: GetString(p, "content") ?? string.Empty,
                Score: point.Score));
        }

        return hits;
    }

    public async Task DeleteByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            filter = new
            {
                must = new[]
                {
                    new
                    {
                        key = "documentId",
                        match = new { value = documentId.ToString() }
                    }
                }
            }
        };

        using var response = await _http.PostAsJsonAsync(
            $"collections/{Collection}/points/delete?wait=true", payload, JsonOptions, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Qdrant delete for document {DocumentId} returned {Status}: {Error}",
                documentId, (int)response.StatusCode, error);
        }
    }

    private static string? GetString(Dictionary<string, JsonElement>? payload, string key)
        => payload is not null && payload.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int GetInt(Dictionary<string, JsonElement>? payload, string key)
    {
        if (payload is not null && payload.TryGetValue(key, out var value)
            && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var i))
        {
            return i;
        }
        return 0;
    }

    private sealed class SearchResponse
    {
        [JsonPropertyName("result")]
        public List<ScoredPoint>? Result { get; set; }
    }

    private sealed class ScoredPoint
    {
        [JsonPropertyName("id")]
        public object? Id { get; set; }

        [JsonPropertyName("score")]
        public float Score { get; set; }

        [JsonPropertyName("payload")]
        public Dictionary<string, JsonElement>? Payload { get; set; }
    }
}
