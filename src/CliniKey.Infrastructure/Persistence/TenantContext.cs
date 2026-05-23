using CliniKey.Application.Abstractions.Tenancy;
using CliniKey.Domain.Enums;

namespace CliniKey.Infrastructure.Persistence;

internal sealed class TenantContext : ITenantContext, ITenantContextSetter
{
    public Guid? TenantId { get; private set; }
    public string? SchemaName { get; private set; }
    public ClinicStatus? ClinicStatus { get; private set; }
    public TenantSchemaHealthStatus? SchemaHealthStatus { get; private set; }
    public bool IsResolved => TenantId.HasValue && !string.IsNullOrWhiteSpace(SchemaName);

    public void Resolve(
        Guid tenantId,
        string schemaName,
        ClinicStatus clinicStatus,
        TenantSchemaHealthStatus schemaHealthStatus)
    {
        TenantId = tenantId;
        SchemaName = schemaName;
        ClinicStatus = clinicStatus;
        SchemaHealthStatus = schemaHealthStatus;
    }

    public void Clear()
    {
        TenantId = null;
        SchemaName = null;
        ClinicStatus = null;
        SchemaHealthStatus = null;
    }
}
