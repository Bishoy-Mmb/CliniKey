using CliniKey.SharedKernel.Interfaces;

namespace CliniKey.SharedKernel.Primitives;

/// <summary>
/// Base class for all domain entities. Uses a strongly-typed ID to prevent
/// primitive obsession and accidental ID mix-ups across aggregate boundaries.
/// </summary>
public abstract class Entity<TId> : IEquatable<Entity<TId>>, IHasDomainEvents
    where TId : notnull
{
    public TId Id { get; protected init; } = default!;

    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void RaiseDomainEvent(IDomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();

    public bool Equals(Entity<TId>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override bool Equals(object? obj) =>
        obj is Entity<TId> entity && Equals(entity);

    public override int GetHashCode() =>
        EqualityComparer<TId>.Default.GetHashCode(Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        Equals(left, right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) =>
        !Equals(left, right);
}
