namespace RagPlatform.Application.Common.Interfaces;

/// <summary>
/// Turns text into dense vector embeddings using the configured model.
/// </summary>
public interface IEmbeddingService
{
    Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<float[]>> EmbedBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default);

    int Dimensions { get; }
}
