using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.Events;

public sealed record TenantActivatedEvent(Guid TenantId, DateTime OccurredOnUtc) : IDomainEvent;
