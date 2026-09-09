using RagPlatform.Domain.Common;
using RagPlatform.Domain.Enums;

namespace RagPlatform.Domain.Entities;

/// <summary>
/// A source document (currently PDF) uploaded by a user and ingested into the
/// vector store. Acts as the aggregate root for its chunks.
/// </summary>
public class Document : BaseEntity
{
    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = "application/pdf";

    public long SizeBytes { get; set; }

    public int PageCount { get; set; }

    public int ChunkCount { get; set; }

    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;

    public string? ErrorMessage { get; set; }

    public DateTimeOffset? ProcessedAt { get; set; }

    public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
}
