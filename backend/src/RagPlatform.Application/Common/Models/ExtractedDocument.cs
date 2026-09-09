namespace RagPlatform.Application.Common.Models;

/// <summary>
/// Output of the document-processing stage: full page count plus the ordered
/// list of chunks ready to be embedded.
/// </summary>
public sealed record ExtractedDocument(int PageCount, IReadOnlyList<TextChunk> Chunks);
