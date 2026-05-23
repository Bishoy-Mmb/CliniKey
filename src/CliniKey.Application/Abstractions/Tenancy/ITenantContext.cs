using CliniKey.Domain.Enums;

namespace CliniKey.Application.Abstractions.Tenancy;

public interface ITenantContext
{
    Guid? TenantId { get; }
    string? SchemaName { get; }
    ClinicStatus? ClinicStatus { get; }
    TenantSchemaHealthStatus? SchemaHealthStatus { get; }
    bool IsResolved { get; }
}

public interface ITenantContextSetter
{
    void Resolve(
        Guid tenantId,
        string schemaName,
        ClinicStatus clinicStatus,
        TenantSchemaHealthStatus schemaHealthStatus);

    void Clear();
}
