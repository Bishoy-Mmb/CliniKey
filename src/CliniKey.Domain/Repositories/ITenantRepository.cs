using CliniKey.Domain.Entities;
using CliniKey.Domain.Enums;

namespace CliniKey.Domain.Repositories;

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Tenant?> GetBySchemaNameAsync(string schemaName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tenant>> ListAsync(
        TenantStatus? status = null,
        TenantSchemaHealthStatus? schemaHealthStatus = null,
        bool requireClinic = false,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tenant>> ListAllAsync(
        TenantStatus? status = null,
        TenantSchemaHealthStatus? schemaHealthStatus = null,
        IReadOnlyCollection<Guid>? tenantIds = null,
        CancellationToken cancellationToken = default);
    Task<int> CountAsync(
        TenantStatus? status = null,
        TenantSchemaHealthStatus? schemaHealthStatus = null,
        bool requireClinic = false,
        CancellationToken cancellationToken = default);
    void Add(Tenant tenant);
    void Remove(Tenant tenant);
}
