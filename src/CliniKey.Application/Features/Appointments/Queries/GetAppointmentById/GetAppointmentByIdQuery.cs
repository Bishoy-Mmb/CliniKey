using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Application.DTOs;

namespace CliniKey.Application.Features.Appointments.Queries.GetAppointmentById;

public sealed record GetAppointmentByIdQuery(Guid AppointmentId) : IQuery<AppointmentResponse>;
