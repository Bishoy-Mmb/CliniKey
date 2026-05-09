using CliniKey.Application.Abstractions.Data;
using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Application.DTOs;
using CliniKey.Domain.Errors;
using CliniKey.SharedKernel.Primitives;
using Dapper;

namespace CliniKey.Application.Features.Patients.Queries.GetPatientById;

internal sealed class GetPatientByIdQueryHandler : IQueryHandler<GetPatientByIdQuery, PatientResponse>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public GetPatientByIdQueryHandler(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<Result<PatientResponse>> Handle(GetPatientByIdQuery request, CancellationToken cancellationToken)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        const string sql = """
            SELECT
                id AS Id,
                first_name AS FirstName,
                last_name AS LastName,
                phone AS PhoneNumber,
                date_of_birth AS DateOfBirth,
                gender AS Gender,
                insurance_details AS InsuranceDetails
            FROM patients
            WHERE id = @PatientId AND is_deleted = false
            """;

        var patient = await connection.QueryFirstOrDefaultAsync<PatientResponse>(
            sql,
            new { request.PatientId });

        if (patient is null)
        {
            return Result.Failure<PatientResponse>(PatientErrors.NotFound(request.PatientId));
        }

        return patient;
    }
}
