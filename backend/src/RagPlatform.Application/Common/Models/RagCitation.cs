namespace RagPlatform.Application.Common.Models;

/// <summary>
/// A source reference surfaced alongside a generated answer so the user can
/// verify the grounding of the response.
/// </summary>
public sealed record RagCitation(
    Guid ChunkId,
    Guid DocumentId,
    string FileName,
    int PageNumber,
    float Score,
    string Snippet);
