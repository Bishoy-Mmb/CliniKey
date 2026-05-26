using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.Events;

public sealed record ClinicActivatedEvent(Guid ClinicId, DateTime OccurredOnUtc) : IDomainEvent;
