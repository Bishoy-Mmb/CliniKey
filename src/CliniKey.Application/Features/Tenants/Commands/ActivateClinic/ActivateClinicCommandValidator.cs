using FluentValidation;

namespace CliniKey.Application.Features.Tenants.Commands.ActivateClinic;

public sealed class ActivateClinicCommandValidator : AbstractValidator<ActivateClinicCommand>
{
    public ActivateClinicCommandValidator()
    {
        RuleFor(x => x.ClinicId)
            .NotEmpty().WithMessage("Clinic ID is required.");
    }
}
