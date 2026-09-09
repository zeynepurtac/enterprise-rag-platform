namespace RagPlatform.Application.Common.Models;

/// <summary>
/// A single semantic-search match returned from the vector store.
/// </summary>
public sealed record SearchHit(
    Guid ChunkId,
    Guid DocumentId,
    string FileName,
    int ChunkIndex,
    int PageNumber,
    string Content,
    float Score);
