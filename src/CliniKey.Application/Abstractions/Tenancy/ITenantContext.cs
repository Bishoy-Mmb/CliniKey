using CliniKey.Domain.Enums;

namespace CliniKey.Application.Abstractions.Tenancy;

public interface ITenantContext
{
    Guid? TenantId { get; }
    string? SchemaName { get; }
    TenantStatus? TenantStatus { get; }
    TenantSchemaHealthStatus? SchemaHealthStatus { get; }
    bool IsResolved { get; }
}

public interface ITenantContextSetter
{
    void Resolve(
        Guid tenantId,
        string schemaName,
        TenantStatus tenantStatus,
        TenantSchemaHealthStatus schemaHealthStatus);

    void Clear();
}
