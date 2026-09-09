using RagPlatform.Domain.Enums;

namespace RagPlatform.Application.Common.Models;

/// <summary>
/// A prior turn passed to the LLM to preserve short conversational context.
/// </summary>
public sealed record ChatTurn(ChatRole Role, string Content);
