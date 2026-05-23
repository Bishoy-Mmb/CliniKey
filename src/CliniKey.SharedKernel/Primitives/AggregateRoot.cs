namespace CliniKey.SharedKernel.Primitives;

/// <summary>
/// Marks an entity as an aggregate root — the single entry point for
/// persistence and consistency boundary enforcement.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    protected readonly TimeProvider Clock;

    public DateTime CreatedAtUtc { get; protected init; }
    public DateTime? UpdatedAtUtc { get; protected set; }

    protected AggregateRoot() { }

    protected AggregateRoot(TimeProvider clock)
    {
        Clock = clock;
        CreatedAtUtc = clock.GetUtcNow().UtcDateTime;
    }

    protected void MarkUpdated() => UpdatedAtUtc = Clock.GetUtcNow().UtcDateTime;
}
