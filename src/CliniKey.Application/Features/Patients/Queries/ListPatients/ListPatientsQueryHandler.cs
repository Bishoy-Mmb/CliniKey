using CliniKey.Application.Abstractions.Data;
using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Application.DTOs;
using CliniKey.SharedKernel.Primitives;
using Dapper;

namespace CliniKey.Application.Features.Patients.Queries.ListPatients;

internal sealed class ListPatientsQueryHandler : IQueryHandler<ListPatientsQuery, IReadOnlyList<PatientResponse>>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public ListPatientsQueryHandler(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<Result<IReadOnlyList<PatientResponse>>> Handle(ListPatientsQuery request, CancellationToken cancellationToken)
    {
        using var connection = _dbConnectionFactory.CreateTenantConnection();

        var sql = """
            SELECT
                id AS Id,
                first_name AS FirstName,
                last_name AS LastName,
                phone AS PhoneNumber,
                date_of_birth AS DateOfBirth,
                gender AS Gender,
                insurance_details AS InsuranceDetails
            FROM patients
            WHERE is_deleted = false
            """;

        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            sql += " AND (first_name ILIKE @SearchTerm OR last_name ILIKE @SearchTerm OR phone ILIKE @SearchTerm)";
            parameters.Add("SearchTerm", $"%{request.SearchTerm}%");
        }

        sql += """
             ORDER BY last_name, first_name
             OFFSET @Offset LIMIT @PageSize
            """;

        parameters.Add("Offset", (request.Page - 1) * request.PageSize);
        parameters.Add("PageSize", request.PageSize);

        var patients = await connection.QueryAsync<PatientResponse>(sql, parameters);

        return patients.ToList().AsReadOnly();
    }
}
