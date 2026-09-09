using RagPlatform.Domain.Common;

namespace RagPlatform.Domain.Entities;

/// <summary>
/// A contiguous slice of text extracted from a <see cref="Document"/>.
/// Each chunk is embedded and stored as a single point in the vector database,
/// where the vector point id equals <see cref="BaseEntity.Id"/>.
/// </summary>
public class DocumentChunk : BaseEntity
{
    public Guid DocumentId { get; set; }

    public Document? Document { get; set; }

    public int ChunkIndex { get; set; }

    public int PageNumber { get; set; }

    public int TokenCount { get; set; }

    public string Content { get; set; } = string.Empty;
}
