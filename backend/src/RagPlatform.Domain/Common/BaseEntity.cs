namespace RagPlatform.Domain.Common;

/// <summary>
/// Base type for all persisted entities. Provides a strongly-typed identity
/// and a creation audit timestamp.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
