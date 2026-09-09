using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RagPlatform.Application.Common.Interfaces;
using RagPlatform.Application.Documents;
using RagPlatform.Infrastructure.BackgroundTasks;

namespace RagPlatform.Api.Controllers;

[ApiController]
[Route("api/documents")]
[Produces("application/json")]
public sealed class DocumentsController : ControllerBase
{
    private const long MaxFileBytes = 200L * 1024 * 1024;

    private readonly IDocumentIngestionService _ingestion;
    private readonly IApplicationDbContext _db;
    private readonly IBackgroundTaskQueue _queue;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IDocumentIngestionService ingestion,
        IApplicationDbContext db,
        IBackgroundTaskQueue queue,
        ILogger<DocumentsController> logger)
    {
        _ingestion = ingestion;
        _db = db;
        _queue = queue;
        _logger = logger;
    }

    /// <summary>Returns all documents, newest first.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<DocumentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DocumentDto>>> List(CancellationToken cancellationToken)
    {
        var documents = await _db.Documents
            .AsNoTracking()
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(documents.Select(DocumentDto.FromEntity));
    }

    /// <summary>Returns a single document by id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var document = await _db.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        return document is null ? NotFound() : Ok(DocumentDto.FromEntity(document));
    }

    /// <summary>Uploads a PDF and queues it for asynchronous ingestion.</summary>
    [HttpPost]
    [RequestSizeLimit(MaxFileBytes)]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DocumentDto>> Upload(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "No file was provided." });
        }

        var isPdf = file.ContentType.Contains("pdf", StringComparison.OrdinalIgnoreCase)
                    || file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
        if (!isPdf)
        {
            return BadRequest(new { error = "Only PDF files are supported." });
        }

        DocumentDto dto;
        await using (var stream = file.OpenReadStream())
        {
            dto = await _ingestion.RegisterAsync(
                stream, file.FileName, file.ContentType, file.Length, cancellationToken);
        }

        // Ingestion (extraction + embedding + upsert) runs on the background worker.
        await _queue.EnqueueAsync(async (services, ct) =>
        {
            var ingestion = services.GetRequiredService<IDocumentIngestionService>();
            await ingestion.ProcessAsync(dto.Id, ct);
        });

        _logger.LogInformation("Queued document {DocumentId} for processing.", dto.Id);
        return AcceptedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    /// <summary>Deletes a document, its chunks and its vectors.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _ingestion.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
