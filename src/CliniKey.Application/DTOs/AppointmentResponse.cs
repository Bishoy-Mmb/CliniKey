using CliniKey.Domain.Enums;

namespace CliniKey.Application.DTOs;

public sealed record AppointmentResponse(
    Guid Id,
    Guid PatientId,
    Guid DentistId,
    DateTime StartTime,
    DateTime EndTime,
    AppointmentStatus Status,
    string? Notes);
