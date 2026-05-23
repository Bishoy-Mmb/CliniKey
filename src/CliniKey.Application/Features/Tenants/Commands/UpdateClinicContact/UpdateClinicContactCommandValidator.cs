using CliniKey.Domain.Entities;
using FluentValidation;

namespace CliniKey.Application.Features.Tenants.Commands.UpdateClinicContact;

public sealed class UpdateClinicContactCommandValidator : AbstractValidator<UpdateClinicContactCommand>
{
    public UpdateClinicContactCommandValidator()
    {
        RuleFor(x => x.ClinicId)
            .NotEmpty().WithMessage("Clinic ID is required.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^01[0125][0-9]{8}$").WithMessage("Invalid Egyptian mobile number format.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Clinic address is required.")
            .MaximumLength(Clinic.MaxAddressLength).WithMessage("Clinic address cannot exceed 500 characters.");
    }
}
