using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RagPlatform.Application.Common.Interfaces;
using RagPlatform.Application.Common.Models;
using RagPlatform.Application.Documents;
using RagPlatform.Domain.Entities;
using RagPlatform.Domain.Enums;
using RagPlatform.Infrastructure.Options;

namespace RagPlatform.Infrastructure.Documents;

/// <summary>
/// Coordinates the full ingestion pipeline: store the raw file, persist a
/// document record, extract and chunk the text, embed each chunk and upsert the
/// resulting vectors into Qdrant. Status transitions are persisted so the UI
/// can poll progress.
/// </summary>
public sealed class DocumentIngestionService : IDocumentIngestionService
{
    private readonly IApplicationDbContext _db;
    private readonly IDocumentProcessingService _processing;
    private readonly IEmbeddingService _embedding;
    private readonly IVectorSearchService _vectorSearch;
    private readonly StorageOptions _storage;
    private readonly ILogger<DocumentIngestionService> _logger;

    public DocumentIngestionService(
        IApplicationDbContext db,
        IDocumentProcessingService processing,
        IEmbeddingService embedding,
        IVectorSearchService vectorSearch,
        IOptions<StorageOptions> storage,
        ILogger<DocumentIngestionService> logger)
    {
        _db = db;
        _processing = processing;
        _embedding = embedding;
        _vectorSearch = vectorSearch;
        _storage = storage.Value;
        _logger = logger;
    }

    public async Task<DocumentDto> RegisterAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_storage.UploadsPath);

        var document = new Document
        {
            FileName = fileName,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/pdf" : contentType,
            SizeBytes = sizeBytes,
            Status = DocumentStatus.Pending
        };

        var storedPath = GetStoredPath(document.Id);
        await using (var target = File.Create(storedPath))
        {
            await fileStream.CopyToAsync(target, cancellationToken);
        }

        _db.Documents.Add(document);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Registered document {DocumentId} ({FileName}).", document.Id, fileName);
        return DocumentDto.FromEntity(document);
    }

    public async Task ProcessAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await _db.Documents
            .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);

        if (document is null)
        {
            _logger.LogWarning("Document {DocumentId} not found for processing.", documentId);
            return;
        }

        var storedPath = GetStoredPath(documentId);
        if (!File.Exists(storedPath))
        {
            document.Status = DocumentStatus.Failed;
            document.ErrorMessage = "Stored source file was not found.";
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        try
        {
            document.Status = DocumentStatus.Processing;
            await _db.SaveChangesAsync(cancellationToken);

            await _vectorSearch.EnsureCollectionAsync(cancellationToken);

            ExtractedDocument extracted;
            await using (var stream = File.OpenRead(storedPath))
            {
                extracted = _processing.Process(stream, document.FileName);
            }

            if (extracted.Chunks.Count == 0)
            {
                document.Status = DocumentStatus.Completed;
                document.PageCount = extracted.PageCount;
                document.ChunkCount = 0;
                document.ProcessedAt = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
                _logger.LogWarning("Document {DocumentId} produced no text chunks.", documentId);
                return;
            }

            var entities = extracted.Chunks
                .Select(c => new DocumentChunk
                {
                    DocumentId = document.Id,
                    ChunkIndex = c.Index,
                    PageNumber = c.PageNumber,
                    TokenCount = c.TokenCount,
                    Content = c.Content
                })
                .ToList();

            var embeddings = await _embedding.EmbedBatchAsync(
                entities.Select(e => e.Content).ToList(), cancellationToken);

            var records = new List<VectorRecord>(entities.Count);
            for (var i = 0; i < entities.Count; i++)
            {
                records.Add(new VectorRecord(
                    entities[i].Id,
                    embeddings[i],
                    document.Id,
                    document.FileName,
                    entities[i].ChunkIndex,
                    entities[i].PageNumber,
                    entities[i].Content));
            }

            _db.DocumentChunks.AddRange(entities);
            await _vectorSearch.UpsertAsync(records, cancellationToken);

            document.Status = DocumentStatus.Completed;
            document.PageCount = extracted.PageCount;
            document.ChunkCount = entities.Count;
            document.ProcessedAt = DateTimeOffset.UtcNow;
            document.ErrorMessage = null;

            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Processed document {DocumentId}: {Pages} pages, {Chunks} chunks.",
                documentId, extracted.PageCount, entities.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ingestion failed for document {DocumentId}.", documentId);
            document.Status = DocumentStatus.Failed;
            document.ErrorMessage = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await _db.Documents
            .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);

        if (document is null)
        {
            return;
        }

        await _vectorSearch.DeleteByDocumentAsync(documentId, cancellationToken);

        _db.Documents.Remove(document);
        await _db.SaveChangesAsync(cancellationToken);

        var storedPath = GetStoredPath(documentId);
        if (File.Exists(storedPath))
        {
            try
            {
                File.Delete(storedPath);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Could not delete stored file for {DocumentId}.", documentId);
            }
        }

        _logger.LogInformation("Deleted document {DocumentId}.", documentId);
    }

    private string GetStoredPath(Guid documentId)
        => Path.Combine(_storage.UploadsPath, $"{documentId}.pdf");
}
