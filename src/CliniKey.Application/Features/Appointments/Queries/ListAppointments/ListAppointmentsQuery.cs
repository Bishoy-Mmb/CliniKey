using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Application.DTOs;

namespace CliniKey.Application.Features.Appointments.Queries.ListAppointments;

public sealed record ListAppointmentsQuery(
    Guid? PatientId,
    Guid? DentistId,
    DateTime? StartDate,
    DateTime? EndDate,
    int Page = 1,
    int PageSize = 10) : IQuery<IReadOnlyList<AppointmentResponse>>;
