namespace RagPlatform.Infrastructure.Options;

public sealed class QdrantOptions
{
    public const string SectionName = "Qdrant";

    public string BaseUrl { get; set; } = "http://qdrant:6333";

    public string CollectionName { get; set; } = "document_chunks";

    public string? ApiKey { get; set; }
}
