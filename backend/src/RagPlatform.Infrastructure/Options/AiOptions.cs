namespace RagPlatform.Infrastructure.Options;

/// <summary>
/// Provider settings for the LLM / embedding endpoints. Both Ollama and OpenAI
/// are supported through the same OpenAI-compatible REST surface, so switching
/// providers is purely a matter of configuration (base url, key, model names).
/// </summary>
public sealed class AiOptions
{
    public const string SectionName = "Ai";

    /// <summary>Base url of an OpenAI-compatible API (Ollama: http://ollama:11434/v1).</summary>
    public string BaseUrl { get; set; } = "http://ollama:11434/v1";

    /// <summary>Optional bearer key (required for OpenAI, empty for local Ollama).</summary>
    public string? ApiKey { get; set; }

    public string ChatModel { get; set; } = "llama3.2";

    public string EmbeddingModel { get; set; } = "nomic-embed-text";

    /// <summary>Vector length produced by <see cref="EmbeddingModel"/>.</summary>
    public int EmbeddingDimensions { get; set; } = 768;

    public int TimeoutSeconds { get; set; } = 300;
}
