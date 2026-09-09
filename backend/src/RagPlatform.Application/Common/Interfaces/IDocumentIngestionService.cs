using RagPlatform.Application.Documents;

namespace RagPlatform.Application.Common.Interfaces;

/// <summary>
/// End-to-end ingestion: persist the document record, extract + chunk text,
/// embed each chunk and upsert the vectors. Runs in the background.
/// </summary>
public interface IDocumentIngestionService
{
    Task<DocumentDto> RegisterAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default);

    Task ProcessAsync(Guid documentId, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid documentId, CancellationToken cancellationToken = default);
}
