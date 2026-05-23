namespace CliniKey.Infrastructure.Persistence;

public sealed class TenancyOptions
{
    public const string SectionName = "Tenancy";

    public string ConnectionString { get; set; } = string.Empty;
    public string SharedSchema { get; set; } = "shared";
    public string TenantSchemaPrefix { get; set; } = "tenant_";
    public int TenantRegistryCacheSeconds { get; set; } = 30;
    public long ProvisioningLockKey { get; set; } = 3003;
    public bool RunTenantMigrationsOnStartup { get; set; }
}
