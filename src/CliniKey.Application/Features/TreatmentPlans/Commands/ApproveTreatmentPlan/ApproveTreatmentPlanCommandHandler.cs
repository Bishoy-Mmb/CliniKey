using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Domain.Errors;
using CliniKey.Domain.Repositories;
using CliniKey.SharedKernel.Interfaces;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Application.Features.TreatmentPlans.Commands.ApproveTreatmentPlan;

internal sealed class ApproveTreatmentPlanCommandHandler(
    ITreatmentPlanRepository treatmentPlanRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<ApproveTreatmentPlanCommand>
{
    public async Task<Result> Handle(ApproveTreatmentPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await treatmentPlanRepository.GetByIdAsync(request.TreatmentPlanId, cancellationToken);
        if (plan is null)
        {
            return Result.Failure(TreatmentPlanErrors.NotFound(request.TreatmentPlanId));
        }

        var result = plan.Approve();
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
