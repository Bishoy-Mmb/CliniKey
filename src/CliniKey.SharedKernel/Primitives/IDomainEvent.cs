using MediatR;

namespace CliniKey.SharedKernel.Primitives;

/// <summary>
/// Marker interface for domain events. Extends MediatR's INotification
/// to enable decoupled, in-process event handling via the mediator pipeline.
/// </summary>
public interface IDomainEvent : INotification
{
    DateTime OccurredOnUtc { get; }
}
