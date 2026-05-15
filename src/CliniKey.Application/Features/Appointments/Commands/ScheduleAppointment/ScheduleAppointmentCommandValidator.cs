using FluentValidation;

namespace CliniKey.Application.Features.Appointments.Commands.ScheduleAppointment;

public sealed class ScheduleAppointmentCommandValidator : AbstractValidator<ScheduleAppointmentCommand>
{
    public ScheduleAppointmentCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DentistId).NotEmpty();
        RuleFor(x => x.StartTime).NotEmpty().Must(start => start > DateTime.UtcNow)
            .WithMessage("Start time must be in the future.");
        RuleFor(x => x.EndTime).NotEmpty().GreaterThan(x => x.StartTime);
    }
}
