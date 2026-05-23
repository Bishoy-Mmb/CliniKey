using FluentValidation;

namespace CliniKey.Application.Features.Tenants.Commands.DeactivateClinic;

public sealed class DeactivateClinicCommandValidator : AbstractValidator<DeactivateClinicCommand>
{
    public DeactivateClinicCommandValidator()
    {
        RuleFor(x => x.ClinicId)
            .NotEmpty().WithMessage("Clinic ID is required.");
    }
}
