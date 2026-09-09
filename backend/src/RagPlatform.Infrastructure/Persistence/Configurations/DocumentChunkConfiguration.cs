using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RagPlatform.Domain.Entities;

namespace RagPlatform.Infrastructure.Persistence.Configurations;

public sealed class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.ToTable("document_chunks");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Content).IsRequired();
        builder.HasIndex(x => new { x.DocumentId, x.ChunkIndex });
    }
}
