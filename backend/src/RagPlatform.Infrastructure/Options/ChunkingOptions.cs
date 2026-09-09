namespace RagPlatform.Infrastructure.Options;

public sealed class ChunkingOptions
{
    public const string SectionName = "Chunking";

    /// <summary>Approximate target size of a chunk, expressed in tokens.</summary>
    public int MaxTokens { get; set; } = 450;

    /// <summary>Token overlap between consecutive chunks to preserve context.</summary>
    public int OverlapTokens { get; set; } = 80;
}
