using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.Events;

public sealed record ClinicProvisionedEvent(
    Guid ClinicId,
    string SchemaName,
    string? CurrentMigration,
    DateTime OccurredOnUtc) : IDomainEvent;
