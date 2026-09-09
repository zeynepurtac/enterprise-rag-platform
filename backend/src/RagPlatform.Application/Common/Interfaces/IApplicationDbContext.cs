using Microsoft.EntityFrameworkCore;
using RagPlatform.Domain.Entities;

namespace RagPlatform.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the EF Core context so the Application layer stays free of
/// a hard dependency on Infrastructure.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Document> Documents { get; }
    DbSet<DocumentChunk> DocumentChunks { get; }
    DbSet<ChatMessage> ChatMessages { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
