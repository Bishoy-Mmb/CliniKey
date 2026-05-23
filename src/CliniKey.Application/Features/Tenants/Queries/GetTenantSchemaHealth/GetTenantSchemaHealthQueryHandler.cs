using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Application.Abstractions.Tenancy;
using CliniKey.Domain.Repositories;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Application.Features.Tenants.Queries.GetTenantSchemaHealth;

internal sealed class GetTenantSchemaHealthQueryHandler : IQueryHandler<GetTenantSchemaHealthQuery, TenantSchemaHealthResponse>
{
    private readonly IClinicRepository _clinicRepository;
    private readonly ITenantMigrationService _tenantMigrationService;

    public GetTenantSchemaHealthQueryHandler(
        IClinicRepository clinicRepository,
        ITenantMigrationService tenantMigrationService)
    {
        _clinicRepository = clinicRepository;
        _tenantMigrationService = tenantMigrationService;
    }

    public async Task<Result<TenantSchemaHealthResponse>> Handle(
        GetTenantSchemaHealthQuery request,
        CancellationToken cancellationToken)
    {
        var clinics = await _clinicRepository.ListAllAsync(null, null, null, cancellationToken);

        return new TenantSchemaHealthResponse(
            _tenantMigrationService.ExpectedMigration,
            clinics
                .Select(c => new TenantSchemaHealthItemResponse(
                    c.Id,
                    c.SchemaName,
                    c.Status.ToString(),
                    c.SchemaHealthStatus.ToString(),
                    c.CurrentMigration,
                    c.LastSchemaVerifiedAtUtc,
                    null))
                .ToList()
                .AsReadOnly());
    }
}
