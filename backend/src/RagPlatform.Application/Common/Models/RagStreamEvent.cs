namespace RagPlatform.Application.Common.Models;

/// <summary>
/// Discriminated event emitted while streaming a RAG answer. Either the
/// citations block (sent once up front) or an incremental token.
/// </summary>
public sealed record RagStreamEvent(
    string Type,
    string? Token = null,
    IReadOnlyList<RagCitation>? Citations = null)
{
    public static RagStreamEvent ForCitations(IReadOnlyList<RagCitation> citations) =>
        new("citations", Citations: citations);

    public static RagStreamEvent ForToken(string token) =>
        new("token", Token: token);

    public static RagStreamEvent Done() => new("done");
}
