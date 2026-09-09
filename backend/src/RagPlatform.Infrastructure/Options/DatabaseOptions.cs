namespace RagPlatform.Infrastructure.Options;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>"postgres" or "sqlite".</summary>
    public string Provider { get; set; } = "postgres";

    public string ConnectionString { get; set; } =
        "Host=postgres;Port=5432;Database=ragplatform;Username=rag;Password=ragpassword";
}
