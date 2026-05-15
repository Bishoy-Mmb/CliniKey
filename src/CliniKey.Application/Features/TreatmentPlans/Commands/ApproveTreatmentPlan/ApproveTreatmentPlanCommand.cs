using CliniKey.Application.Abstractions.Messaging;

namespace CliniKey.Application.Features.TreatmentPlans.Commands.ApproveTreatmentPlan;

public sealed record ApproveTreatmentPlanCommand(Guid TreatmentPlanId) : ICommand;
