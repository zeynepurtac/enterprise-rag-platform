using RagPlatform.Domain.Common;
using RagPlatform.Domain.Enums;

namespace RagPlatform.Domain.Entities;

/// <summary>
/// A single turn in a conversation. When <see cref="DocumentId"/> is set the
/// message belongs to a document-scoped conversation.
/// </summary>
public class ChatMessage : BaseEntity
{
    public Guid? DocumentId { get; set; }

    public ChatRole Role { get; set; }

    public string Content { get; set; } = string.Empty;
}
