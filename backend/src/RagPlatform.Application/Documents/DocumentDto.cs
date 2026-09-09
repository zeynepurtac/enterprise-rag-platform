using RagPlatform.Domain.Entities;

namespace RagPlatform.Application.Documents;

public sealed record DocumentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    int PageCount,
    int ChunkCount,
    string Status,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ProcessedAt)
{
    public static DocumentDto FromEntity(Document d) => new(
        d.Id,
        d.FileName,
        d.ContentType,
        d.SizeBytes,
        d.PageCount,
        d.ChunkCount,
        d.Status.ToString(),
        d.ErrorMessage,
        d.CreatedAt,
        d.ProcessedAt);
}
