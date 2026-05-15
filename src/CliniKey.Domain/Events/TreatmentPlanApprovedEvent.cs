using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.Events;

public sealed record TreatmentPlanApprovedEvent(
    Guid TreatmentPlanId,
    DateTime OccurredOnUtc) : IDomainEvent;
