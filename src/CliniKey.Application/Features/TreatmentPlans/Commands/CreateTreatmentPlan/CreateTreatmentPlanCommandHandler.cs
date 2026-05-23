using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Domain.Entities;
using CliniKey.Domain.Repositories;
using CliniKey.Domain.ValueObjects;
using CliniKey.SharedKernel.Interfaces;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Application.Features.TreatmentPlans.Commands.CreateTreatmentPlan;

internal sealed class CreateTreatmentPlanCommandHandler(
    ITreatmentPlanRepository treatmentPlanRepository,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<CreateTreatmentPlanCommand, Guid>
{
    private const string DefaultCurrency = "EGP";

    public async Task<Result<Guid>> Handle(CreateTreatmentPlanCommand request, CancellationToken cancellationToken)
    {
        var items = new List<(ToothCode Tooth, string ProcedureName, Money EstimatedCost)>();

        foreach (var item in request.Items)
        {
            if (!int.TryParse(item.ToothCode, out var toothInt))
            {
                return Result.Failure<Guid>(Error.Validation("TreatmentPlan.InvalidTooth", "Tooth code must be numeric."));
            }

            var toothResult = ToothCode.Create(toothInt);
            if (toothResult.IsFailure)
            {
                return Result.Failure<Guid>(toothResult.Error);
            }

            var moneyResult = Money.Create(item.EstimatedCost, DefaultCurrency);
            if (moneyResult.IsFailure)
            {
                return Result.Failure<Guid>(moneyResult.Error);
            }

            items.Add((toothResult.Value, item.ProcedureName, moneyResult.Value));
        }

        var planResult = TreatmentPlan.Create(request.PatientId, request.DentistId, items, clock);
        if (planResult.IsFailure)
        {
            return Result.Failure<Guid>(planResult.Error);
        }

        treatmentPlanRepository.Add(planResult.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return planResult.Value.Id;
    }
}
