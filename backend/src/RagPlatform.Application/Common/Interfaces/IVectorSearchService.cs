using RagPlatform.Application.Common.Models;

namespace RagPlatform.Application.Common.Interfaces;

/// <summary>
/// Persists embeddings and performs semantic (nearest-neighbour) search.
/// </summary>
public interface IVectorSearchService
{
    Task EnsureCollectionAsync(CancellationToken cancellationToken = default);

    Task UpsertAsync(IReadOnlyList<VectorRecord> records, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SearchHit>> SearchAsync(
        float[] queryVector,
        int limit,
        Guid? documentId = null,
        CancellationToken cancellationToken = default);

    Task DeleteByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
}
