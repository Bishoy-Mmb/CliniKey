using FluentValidation;

namespace CliniKey.Application.Features.Patients.Commands.UpdatePatient;

public sealed class UpdatePatientCommandValidator : AbstractValidator<UpdatePatientCommand>
{
    public UpdatePatientCommandValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("Patient ID is required.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^01[0125][0-9]{8}$").WithMessage("Invalid Egyptian mobile number format.");
    }
}
