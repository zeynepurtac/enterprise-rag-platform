using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RagPlatform.Domain.Entities;

namespace RagPlatform.Infrastructure.Persistence.Configurations;

public sealed class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("chat_messages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Role).HasConversion<int>();
        builder.Property(x => x.Content).IsRequired();
        builder.HasIndex(x => x.DocumentId);
        builder.HasIndex(x => x.CreatedAt);
    }
}
