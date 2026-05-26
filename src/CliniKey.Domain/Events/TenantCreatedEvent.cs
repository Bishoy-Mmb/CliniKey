using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.Events;

public sealed record TenantCreatedEvent(Guid TenantId, string SchemaName, DateTime OccurredOnUtc) : IDomainEvent;
