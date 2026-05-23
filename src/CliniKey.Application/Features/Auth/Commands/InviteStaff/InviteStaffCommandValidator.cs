using FluentValidation;
using CliniKey.Application.Constants;
using CliniKey.Application.Extensions;
using CliniKey.Domain.Entities;

namespace CliniKey.Application.Features.Auth.Commands.InviteStaff;

public sealed class InviteStaffCommandValidator : AbstractValidator<InviteStaffCommand>
{
    public InviteStaffCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email format is invalid.")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");

        RuleFor(x => x.Password)
            .MustBeStrongPassword();

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("FullName is required.")
            .MaximumLength(200).WithMessage("FullName must not exceed 200 characters.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .Must(role => role == Roles.Dentist || role == Roles.Receptionist)
            .WithMessage("Role must be either Dentist or Receptionist.");

        When(x => x.Role == Roles.Dentist, () =>
        {
            RuleFor(x => x.Specialization)
                .NotEmpty().WithMessage("Specialization is required for a Dentist.")
                .MaximumLength(Dentist.MaxSpecializationLength)
                .WithMessage($"Specialization must not exceed {Dentist.MaxSpecializationLength} characters.");
                
            RuleFor(x => x.LicenseNumber)
                .NotEmpty().WithMessage("LicenseNumber is required for a Dentist.")
                .MaximumLength(Dentist.MaxLicenseNumberLength)
                .WithMessage($"LicenseNumber must not exceed {Dentist.MaxLicenseNumberLength} characters.");
        });
    }
}
