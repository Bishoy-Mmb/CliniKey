using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.Events;

public sealed record TenantProvisionedEvent(
    Guid TenantId,
    string SchemaName,
    string? CurrentMigration,
    DateTime OccurredOnUtc) : IDomainEvent;
