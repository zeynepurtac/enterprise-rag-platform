namespace RagPlatform.Application.Common.Models;

/// <summary>
/// Result of splitting a raw document into overlapping windows of text.
/// </summary>
public sealed record TextChunk(int Index, int PageNumber, string Content, int TokenCount);
