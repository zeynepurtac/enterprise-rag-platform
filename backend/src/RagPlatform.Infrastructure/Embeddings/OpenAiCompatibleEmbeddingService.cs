using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using RagPlatform.Application.Common.Interfaces;
using RagPlatform.Infrastructure.Options;

namespace RagPlatform.Infrastructure.Embeddings;

/// <summary>
/// Embedding client that targets the OpenAI-compatible <c>/embeddings</c>
/// endpoint. Ollama exposes this surface at <c>/v1/embeddings</c>, so the same
/// implementation serves both local and cloud deployments.
/// </summary>
public sealed class OpenAiCompatibleEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _http;
    private readonly AiOptions _options;

    public OpenAiCompatibleEmbeddingService(HttpClient http, IOptions<AiOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public int Dimensions => _options.EmbeddingDimensions;

    public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var result = await EmbedBatchAsync(new[] { text }, cancellationToken);
        return result[0];
    }

    public async Task<IReadOnlyList<float[]>> EmbedBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        if (texts.Count == 0)
        {
            return Array.Empty<float[]>();
        }

        var payload = new EmbeddingRequest
        {
            Model = _options.EmbeddingModel,
            Input = texts.ToArray()
        };

        using var response = await _http.PostAsJsonAsync("embeddings", payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Embedding request failed ({(int)response.StatusCode}): {body}");
        }

        var parsed = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(
            cancellationToken: cancellationToken);

        if (parsed?.Data is null || parsed.Data.Count == 0)
        {
            throw new InvalidOperationException("Embedding response contained no data.");
        }

        return parsed.Data
            .OrderBy(d => d.Index)
            .Select(d => d.Embedding ?? Array.Empty<float>())
            .ToList();
    }

    private sealed class EmbeddingRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("input")]
        public string[] Input { get; set; } = Array.Empty<string>();
    }

    private sealed class EmbeddingResponse
    {
        [JsonPropertyName("data")]
        public List<EmbeddingData>? Data { get; set; }
    }

    private sealed class EmbeddingData
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("embedding")]
        public float[]? Embedding { get; set; }
    }
}
