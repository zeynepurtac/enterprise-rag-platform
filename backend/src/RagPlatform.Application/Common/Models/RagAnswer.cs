namespace RagPlatform.Application.Common.Models;

/// <summary>
/// Non-streaming answer produced by the RAG pipeline.
/// </summary>
public sealed record RagAnswer(string Answer, IReadOnlyList<RagCitation> Citations);
