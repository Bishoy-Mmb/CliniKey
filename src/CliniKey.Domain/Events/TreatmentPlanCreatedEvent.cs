using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.Events;

public sealed record TreatmentPlanCreatedEvent(
    Guid TreatmentPlanId,
    DateTime OccurredOnUtc) : IDomainEvent;
