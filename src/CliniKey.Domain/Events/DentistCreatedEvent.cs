using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.Events;

public sealed record DentistCreatedEvent(Guid DentistId, DateTime OccurredOnUtc) : IDomainEvent;
