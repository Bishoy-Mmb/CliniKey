using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.Events;

public sealed record ClinicDeactivatedEvent(
    Guid ClinicId,
    Guid? OperatorUserId,
    DateTime OccurredOnUtc) : IDomainEvent;
