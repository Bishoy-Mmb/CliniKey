using FluentValidation;

namespace CliniKey.Application.Features.TreatmentPlans.Commands.CreateTreatmentPlan;

public sealed class CreateTreatmentPlanCommandValidator : AbstractValidator<CreateTreatmentPlanCommand>
{
    public CreateTreatmentPlanCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DentistId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty();

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ToothCode).NotEmpty();
            item.RuleFor(i => i.ProcedureName).NotEmpty().MaximumLength(200);
            item.RuleFor(i => i.EstimatedCost).GreaterThan(0);
        });
    }
}
