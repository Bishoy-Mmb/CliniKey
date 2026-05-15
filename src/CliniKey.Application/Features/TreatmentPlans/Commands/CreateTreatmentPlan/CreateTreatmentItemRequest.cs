namespace CliniKey.Application.Features.TreatmentPlans.Commands.CreateTreatmentPlan;

public sealed record CreateTreatmentItemRequest(
    string ToothCode,
    string ProcedureName,
    decimal EstimatedCost);
