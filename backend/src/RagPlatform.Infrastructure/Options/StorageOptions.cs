namespace RagPlatform.Infrastructure.Options;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Directory where uploaded source files are persisted.</summary>
    public string UploadsPath { get; set; } = "/app/data/uploads";
}
