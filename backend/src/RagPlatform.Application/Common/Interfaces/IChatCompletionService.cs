using RagPlatform.Application.Common.Models;

namespace RagPlatform.Application.Common.Interfaces;

/// <summary>
/// Thin abstraction over the chat/completions endpoint of the LLM provider
/// (Ollama or OpenAI-compatible).
/// </summary>
public interface IChatCompletionService
{
    Task<string> CompleteAsync(
        string systemPrompt,
        IReadOnlyList<ChatTurn> history,
        string userPrompt,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> StreamAsync(
        string systemPrompt,
        IReadOnlyList<ChatTurn> history,
        string userPrompt,
        CancellationToken cancellationToken = default);
}
