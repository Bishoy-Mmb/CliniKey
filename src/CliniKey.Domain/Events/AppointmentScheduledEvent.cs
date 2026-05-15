using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.Events;

public sealed record AppointmentScheduledEvent(
    Guid AppointmentId,
    DateTime OccurredOnUtc) : IDomainEvent;
