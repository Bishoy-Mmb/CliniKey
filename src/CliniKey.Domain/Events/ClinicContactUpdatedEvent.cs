using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.Events;

public sealed record ClinicContactUpdatedEvent(Guid ClinicId, DateTime OccurredOnUtc) : IDomainEvent;
