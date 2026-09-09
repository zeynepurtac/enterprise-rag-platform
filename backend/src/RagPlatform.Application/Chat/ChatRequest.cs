namespace RagPlatform.Application.Chat;

/// <summary>
/// Inbound chat request. <see cref="DocumentId"/> is optional — when null the
/// question is answered against the entire knowledge base.
/// </summary>
public sealed class ChatRequest
{
    public string Question { get; set; } = string.Empty;

    public Guid? DocumentId { get; set; }

    public List<ChatHistoryItem> History { get; set; } = new();
}

public sealed class ChatHistoryItem
{
    public string Role { get; set; } = "user";

    public string Content { get; set; } = string.Empty;
}
