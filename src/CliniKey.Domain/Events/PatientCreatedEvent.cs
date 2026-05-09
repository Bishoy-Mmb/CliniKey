using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.Events;

public sealed record PatientCreatedEvent(Guid PatientId, DateTime OccurredOnUtc) : IDomainEvent;
