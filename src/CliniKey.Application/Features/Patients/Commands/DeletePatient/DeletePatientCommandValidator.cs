using FluentValidation;

namespace CliniKey.Application.Features.Patients.Commands.DeletePatient;

public sealed class DeletePatientCommandValidator : AbstractValidator<DeletePatientCommand>
{
    public DeletePatientCommandValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("Patient ID is required.");
    }
}
