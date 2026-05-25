using System.ComponentModel.DataAnnotations;

namespace CliniKey.Infrastructure.Persistence;

public sealed class TenancyOptions
{
    public const string SectionName = "Tenancy";
    public const string DefaultSharedSchema = "shared";
    public const string DefaultTenantSchemaPrefix = "tenant_";

    [Required]
    [StringLength(63, MinimumLength = 1)]
    [RegularExpression(@"^[a-z][a-z0-9_]*$")]
    public string SharedSchema { get; set; } = DefaultSharedSchema;

    [Required]
    [StringLength(31, MinimumLength = 1)]
    [RegularExpression(@"^[a-z][a-z0-9_]*$")]
    public string TenantSchemaPrefix { get; set; } = DefaultTenantSchemaPrefix;

    [Range(1, 3600)]
    public int TenantRegistryCacheSeconds { get; set; } = 30;

    [Range(1, long.MaxValue)]
    public long ProvisioningLockKey { get; set; } = 3003;

    public bool RunTenantMigrationsOnStartup { get; set; }
}
