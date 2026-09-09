namespace RagPlatform.Infrastructure.Options;

public sealed class RagOptions
{
    public const string SectionName = "Rag";

    /// <summary>Number of chunks retrieved for each question.</summary>
    public int TopK { get; set; } = 5;

    /// <summary>Minimum cosine score a hit must reach to be used as context.</summary>
    public float MinScore { get; set; } = 0.25f;

    public string SystemPrompt { get; set; } =
        "You are an enterprise document-intelligence assistant. Answer the user's " +
        "question using ONLY the information contained in the provided context. " +
        "If the answer is not present in the context, say clearly that the documents " +
        "do not contain that information. Cite the sources you rely on using their " +
        "bracketed numbers, e.g. [1], [2]. Reply in the same language as the question.";
}
