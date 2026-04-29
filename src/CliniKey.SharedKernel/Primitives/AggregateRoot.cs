namespace CliniKey.SharedKernel.Primitives;

/// <summary>
/// Marks an entity as an aggregate root — the single entry point for
/// persistence and consistency boundary enforcement.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    public DateTime CreatedAtUtc { get; protected init; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; protected set; }

    protected void MarkUpdated() => UpdatedAtUtc = DateTime.UtcNow;
}
