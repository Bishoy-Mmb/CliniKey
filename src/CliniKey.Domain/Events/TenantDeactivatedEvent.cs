using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.Events;

public sealed record TenantDeactivatedEvent(
    Guid TenantId,
    Guid? OperatorUserId,
    DateTime OccurredOnUtc) : IDomainEvent;
