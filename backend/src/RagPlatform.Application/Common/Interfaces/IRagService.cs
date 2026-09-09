using RagPlatform.Application.Common.Models;

namespace RagPlatform.Application.Common.Interfaces;

/// <summary>
/// Orchestrates retrieval-augmented generation: embed the question, retrieve
/// the most relevant chunks, build a grounded prompt and call the LLM.
/// </summary>
public interface IRagService
{
    Task<RagAnswer> AskAsync(
        string question,
        Guid? documentId,
        IReadOnlyList<ChatTurn> history,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<RagStreamEvent> StreamAsync(
        string question,
        Guid? documentId,
        IReadOnlyList<ChatTurn> history,
        CancellationToken cancellationToken = default);
}
