namespace CliniKey.Domain.Enums;

public enum TenantSchemaHealthStatus
{
    Unknown = 1,
    Healthy = 2,
    Missing = 3,
    MigrationPending = 4,
    Unhealthy = 5
}
