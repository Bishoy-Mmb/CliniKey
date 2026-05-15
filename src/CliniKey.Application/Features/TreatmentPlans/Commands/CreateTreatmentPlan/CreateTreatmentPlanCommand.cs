using CliniKey.Application.Abstractions.Messaging;

namespace CliniKey.Application.Features.TreatmentPlans.Commands.CreateTreatmentPlan;

public sealed record CreateTreatmentPlanCommand(
    Guid PatientId,
    Guid DentistId,
    List<CreateTreatmentItemRequest> Items) : ICommand<Guid>;
