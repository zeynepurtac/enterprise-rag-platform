using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RagPlatform.Application.Chat;
using RagPlatform.Application.Common.Interfaces;
using RagPlatform.Application.Common.Models;
using RagPlatform.Domain.Entities;
using RagPlatform.Domain.Enums;

namespace RagPlatform.Api.Controllers;

[ApiController]
[Route("api/chat")]
public sealed class ChatController : ControllerBase
{
    private static readonly JsonSerializerOptions StreamJson = new(JsonSerializerDefaults.Web);

    private readonly IRagService _rag;
    private readonly IApplicationDbContext _db;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IRagService rag, IApplicationDbContext db, ILogger<ChatController> logger)
    {
        _rag = rag;
        _db = db;
        _logger = logger;
    }

    /// <summary>Answers a question over the documents and returns the full result.</summary>
    [HttpPost]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChatResponse>> Ask(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest(new { error = "Question must not be empty." });
        }

        var history = MapHistory(request.History);
        var answer = await _rag.AskAsync(request.Question, request.DocumentId, history, cancellationToken);

        await PersistTurnAsync(request, answer.Answer, cancellationToken);

        return Ok(new ChatResponse(answer.Answer, answer.Citations));
    }

    /// <summary>Streams the answer token-by-token as Server-Sent Events.</summary>
    [HttpPost("stream")]
    public async Task Stream([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        if (string.IsNullOrWhiteSpace(request.Question))
        {
            await WriteEventAsync(new { type = "error", message = "Question must not be empty." }, cancellationToken);
            return;
        }

        var history = MapHistory(request.History);
        var answerBuilder = new StringBuilder();

        try
        {
            await foreach (var evt in _rag.StreamAsync(
                request.Question, request.DocumentId, history, cancellationToken))
            {
                if (evt.Type == "token" && evt.Token is not null)
                {
                    answerBuilder.Append(evt.Token);
                }

                await WriteEventAsync(evt, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected — nothing to do.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Streaming chat failed.");
            await WriteEventAsync(new { type = "error", message = ex.Message }, cancellationToken);
        }

        if (answerBuilder.Length > 0)
        {
            await PersistTurnAsync(request, answerBuilder.ToString(), cancellationToken);
        }
    }

    private async Task WriteEventAsync(object payload, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload, StreamJson);
        await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }

    private static IReadOnlyList<ChatTurn> MapHistory(IEnumerable<ChatHistoryItem> history)
        => history
            .Where(h => !string.IsNullOrWhiteSpace(h.Content))
            .Select(h => new ChatTurn(
                h.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase)
                    ? ChatRole.Assistant
                    : ChatRole.User,
                h.Content))
            .ToList();

    private async Task PersistTurnAsync(ChatRequest request, string answer, CancellationToken cancellationToken)
    {
        _db.ChatMessages.Add(new ChatMessage
        {
            DocumentId = request.DocumentId,
            Role = ChatRole.User,
            Content = request.Question
        });
        _db.ChatMessages.Add(new ChatMessage
        {
            DocumentId = request.DocumentId,
            Role = ChatRole.Assistant,
            Content = answer
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}
