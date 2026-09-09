namespace RagPlatform.Application.Common.Models;

/// <summary>
/// A single point to be upserted into the vector database.
/// </summary>
public sealed record VectorRecord(
    Guid Id,
    float[] Vector,
    Guid DocumentId,
    string FileName,
    int ChunkIndex,
    int PageNumber,
    string Content);
