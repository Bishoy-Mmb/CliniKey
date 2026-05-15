using CliniKey.Domain.Enums;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.Events;

public sealed record AppointmentStatusChangedEvent(
    Guid AppointmentId,
    AppointmentStatus OldStatus,
    AppointmentStatus NewStatus,
    DateTime OccurredOnUtc) : IDomainEvent;
