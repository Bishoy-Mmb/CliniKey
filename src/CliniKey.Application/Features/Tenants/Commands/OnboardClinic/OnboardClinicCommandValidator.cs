using CliniKey.Domain.Entities;
using FluentValidation;

namespace CliniKey.Application.Features.Tenants.Commands.OnboardClinic;

public sealed class OnboardClinicCommandValidator : AbstractValidator<OnboardClinicCommand>
{
    public OnboardClinicCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Clinic name is required.")
            .MaximumLength(Clinic.MaxNameLength).WithMessage("Clinic name cannot exceed 200 characters.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^01[0125][0-9]{8}$").WithMessage("Invalid Egyptian mobile number format.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Clinic address is required.")
            .MaximumLength(Clinic.MaxAddressLength).WithMessage("Clinic address cannot exceed 500 characters.");
    }
}
