using RagPlatform.Application.Common.Models;

namespace RagPlatform.Application.Chat;

public sealed record ChatResponse(string Answer, IReadOnlyList<RagCitation> Citations);
