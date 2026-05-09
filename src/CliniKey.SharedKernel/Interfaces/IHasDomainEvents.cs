using CliniKey.SharedKernel.Primitives;

namespace CliniKey.SharedKernel.Interfaces;

public interface IHasDomainEvents
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
