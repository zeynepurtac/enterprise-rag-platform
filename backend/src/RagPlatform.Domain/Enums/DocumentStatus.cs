namespace RagPlatform.Domain.Enums;

/// <summary>
/// Lifecycle of an ingested document as it moves through the RAG pipeline.
/// </summary>
public enum DocumentStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}
