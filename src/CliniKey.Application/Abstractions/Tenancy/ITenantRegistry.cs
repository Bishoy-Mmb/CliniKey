using CliniKey.Domain.Enums;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Application.Abstractions.Tenancy;

public sealed record TenantRegistryEntry(
    Guid TenantId,
    string SchemaName,
    TenantStatus TenantStatus,
    TenantProvisioningStatus ProvisioningStatus,
    TenantSchemaHealthStatus SchemaHealthStatus,
    string? CurrentMigration);

public interface ITenantRegistry
{
    Task<Result<TenantRegistryEntry>> ResolveAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task InvalidateAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
