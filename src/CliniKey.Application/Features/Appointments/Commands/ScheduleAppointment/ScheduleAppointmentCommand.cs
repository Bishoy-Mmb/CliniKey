using CliniKey.Application.Abstractions.Messaging;

namespace CliniKey.Application.Features.Appointments.Commands.ScheduleAppointment;

public sealed record ScheduleAppointmentCommand(
    Guid PatientId,
    Guid DentistId,
    DateTime StartTime,
    DateTime EndTime,
    string? Notes) : ICommand<Guid>;
