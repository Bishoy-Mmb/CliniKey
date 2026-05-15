using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Domain.Enums;

namespace CliniKey.Application.Features.Appointments.Commands.ChangeStatus;

public sealed record ChangeAppointmentStatusCommand(
    Guid AppointmentId,
    AppointmentStatus NewStatus) : ICommand;
