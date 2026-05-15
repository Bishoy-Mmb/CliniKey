using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Application.DTOs;

namespace CliniKey.Application.Features.TreatmentPlans.Queries.GetTreatmentPlanById;

public sealed record GetTreatmentPlanByIdQuery(Guid TreatmentPlanId) : IQuery<TreatmentPlanResponse>;
