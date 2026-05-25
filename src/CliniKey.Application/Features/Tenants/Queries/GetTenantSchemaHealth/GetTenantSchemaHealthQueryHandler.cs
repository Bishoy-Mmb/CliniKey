using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Application.Abstractions.Tenancy;
using CliniKey.Domain.Repositories;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Application.Features.Tenants.Queries.GetTenantSchemaHealth;

internal sealed class GetTenantSchemaHealthQueryHandler : IQueryHandler<GetTenantSchemaHealthQuery, TenantSchemaHealthResponse>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantMigrationService _tenantMigrationService;

    public GetTenantSchemaHealthQueryHandler(
        ITenantRepository tenantRepository,
        ITenantMigrationService tenantMigrationService)
    {
        _tenantRepository = tenantRepository;
        _tenantMigrationService = tenantMigrationService;
    }

    public async Task<Result<TenantSchemaHealthResponse>> Handle(
        GetTenantSchemaHealthQuery request,
        CancellationToken cancellationToken)
    {
        var tenants = await _tenantRepository.ListAllAsync(null, null, null, cancellationToken);

        return new TenantSchemaHealthResponse(
            _tenantMigrationService.ExpectedMigration,
            tenants
                .Select(t => new TenantSchemaHealthItemResponse(
                    t.Id,
                    t.SchemaName,
                    t.Status.ToString(),
                    t.SchemaHealthStatus.ToString(),
                    t.CurrentMigration,
                    t.LastSchemaVerifiedAtUtc,
                    null))
                .ToList()
                .AsReadOnly());
    }
}
