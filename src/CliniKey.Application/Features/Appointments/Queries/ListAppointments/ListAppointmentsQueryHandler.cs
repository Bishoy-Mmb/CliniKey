using System.Data;
using CliniKey.Application.Abstractions.Data;
using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Application.DTOs;
using CliniKey.SharedKernel.Primitives;
using Dapper;

namespace CliniKey.Application.Features.Appointments.Queries.ListAppointments;

internal sealed class ListAppointmentsQueryHandler : IQueryHandler<ListAppointmentsQuery, IReadOnlyList<AppointmentResponse>>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public ListAppointmentsQueryHandler(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<Result<IReadOnlyList<AppointmentResponse>>> Handle(ListAppointmentsQuery request, CancellationToken cancellationToken)
    {
        using IDbConnection connection = _dbConnectionFactory.CreateConnection();

        var sql = @"
            SELECT
                id AS Id,
                patient_id AS PatientId,
                dentist_id AS DentistId,
                start_time AS StartTime,
                end_time AS EndTime,
                status AS Status,
                notes AS Notes
            FROM appointments
            WHERE 1=1";

        var parameters = new DynamicParameters();

        if (request.PatientId.HasValue)
        {
            sql += " AND patient_id = @PatientId";
            parameters.Add("PatientId", request.PatientId.Value);
        }

        if (request.DentistId.HasValue)
        {
            sql += " AND dentist_id = @DentistId";
            parameters.Add("DentistId", request.DentistId.Value);
        }

        if (request.StartDate.HasValue)
        {
            sql += " AND start_time >= @StartDate";
            parameters.Add("StartDate", request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            sql += " AND end_time <= @EndDate";
            parameters.Add("EndDate", request.EndDate.Value);
        }

        sql += @" ORDER BY start_time ASC
                 OFFSET @Offset LIMIT @Limit";

        parameters.Add("Offset", (request.Page - 1) * request.PageSize);
        parameters.Add("Limit", request.PageSize);

        var appointments = await connection.QueryAsync<AppointmentResponse>(
            sql,
            parameters);

        return appointments.ToList().AsReadOnly();
    }
}
