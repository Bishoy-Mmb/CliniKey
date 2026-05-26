using System.Data;
using CliniKey.Application.Abstractions.Data;
using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Application.DTOs;
using CliniKey.Domain.Errors;
using CliniKey.SharedKernel.Primitives;
using Dapper;

namespace CliniKey.Application.Features.Appointments.Queries.GetAppointmentById;

internal sealed class GetAppointmentByIdQueryHandler : IQueryHandler<GetAppointmentByIdQuery, AppointmentResponse>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public GetAppointmentByIdQueryHandler(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<Result<AppointmentResponse>> Handle(GetAppointmentByIdQuery request, CancellationToken cancellationToken)
    {
        using IDbConnection connection = _dbConnectionFactory.CreateTenantConnection();

        const string sql = @"
            SELECT
                id AS Id,
                patient_id AS PatientId,
                dentist_id AS DentistId,
                start_time AS StartTime,
                end_time AS EndTime,
                status AS Status,
                notes AS Notes
            FROM appointments
            WHERE id = @AppointmentId";

        var appointment = await connection.QueryFirstOrDefaultAsync<AppointmentResponse>(
            sql,
            new { request.AppointmentId });

        if (appointment is null)
        {
            return Result.Failure<AppointmentResponse>(AppointmentErrors.NotFound);
        }

        return appointment;
    }
}
