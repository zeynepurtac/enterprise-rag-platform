using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Options;
using RagPlatform.Application.Common.Interfaces;
using RagPlatform.Application.Common.Models;
using RagPlatform.Infrastructure.Options;

namespace RagPlatform.Infrastructure.Rag;

/// <summary>
/// Retrieval-Augmented Generation pipeline. Embeds the question, retrieves the
/// most relevant chunks from the vector store, assembles a grounded prompt and
/// delegates generation to the chat model. Supports buffered and streaming use.
/// </summary>
public sealed class RagService : IRagService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorSearchService _vectorSearch;
    private readonly IChatCompletionService _chat;
    private readonly RagOptions _options;

    public RagService(
        IEmbeddingService embeddingService,
        IVectorSearchService vectorSearch,
        IChatCompletionService chat,
        IOptions<RagOptions> options)
    {
        _embeddingService = embeddingService;
        _vectorSearch = vectorSearch;
        _chat = chat;
        _options = options.Value;
    }

    public async Task<RagAnswer> AskAsync(
        string question,
        Guid? documentId,
        IReadOnlyList<ChatTurn> history,
        CancellationToken cancellationToken = default)
    {
        var (hits, prompt) = await RetrieveAsync(question, documentId, cancellationToken);
        if (hits.Count == 0)
        {
            return new RagAnswer(
                "I could not find anything relevant in the uploaded documents to answer that.",
                Array.Empty<RagCitation>());
        }

        var answer = await _chat.CompleteAsync(_options.SystemPrompt, history, prompt, cancellationToken);
        return new RagAnswer(answer, BuildCitations(hits));
    }

    public async IAsyncEnumerable<RagStreamEvent> StreamAsync(
        string question,
        Guid? documentId,
        IReadOnlyList<ChatTurn> history,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var (hits, prompt) = await RetrieveAsync(question, documentId, cancellationToken);

        if (hits.Count == 0)
        {
            yield return RagStreamEvent.ForCitations(Array.Empty<RagCitation>());
            yield return RagStreamEvent.ForToken(
                "I could not find anything relevant in the uploaded documents to answer that.");
            yield return RagStreamEvent.Done();
            yield break;
        }

        yield return RagStreamEvent.ForCitations(BuildCitations(hits));

        await foreach (var token in _chat.StreamAsync(_options.SystemPrompt, history, prompt, cancellationToken))
        {
            yield return RagStreamEvent.ForToken(token);
        }

        yield return RagStreamEvent.Done();
    }

    private async Task<(IReadOnlyList<SearchHit> Hits, string Prompt)> RetrieveAsync(
        string question,
        Guid? documentId,
        CancellationToken cancellationToken)
    {
        var queryVector = await _embeddingService.EmbedAsync(question, cancellationToken);

        var rawHits = await _vectorSearch.SearchAsync(
            queryVector, _options.TopK, documentId, cancellationToken);

        var hits = rawHits.Where(h => h.Score >= _options.MinScore).ToList();
        if (hits.Count == 0)
        {
            return (hits, string.Empty);
        }

        var prompt = BuildPrompt(question, hits);
        return (hits, prompt);
    }

    private static string BuildPrompt(string question, IReadOnlyList<SearchHit> hits)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Use the following context to answer the question.");
        builder.AppendLine();
        builder.AppendLine("### Context");

        for (var i = 0; i < hits.Count; i++)
        {
            var hit = hits[i];
            builder.AppendLine($"[{i + 1}] (source: {hit.FileName}, page {hit.PageNumber})");
            builder.AppendLine(hit.Content);
            builder.AppendLine();
        }

        builder.AppendLine("### Question");
        builder.AppendLine(question);
        builder.AppendLine();
        builder.AppendLine("### Answer");

        return builder.ToString();
    }

    private static IReadOnlyList<RagCitation> BuildCitations(IReadOnlyList<SearchHit> hits)
        => hits.Select(h => new RagCitation(
                h.ChunkId,
                h.DocumentId,
                h.FileName,
                h.PageNumber,
                h.Score,
                Snippet(h.Content)))
            .ToList();

    private static string Snippet(string content)
    {
        const int maxLength = 240;
        var trimmed = content.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength] + "…";
    }
}
