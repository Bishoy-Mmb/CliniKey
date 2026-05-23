namespace CliniKey.Domain.Enums;

public enum TenantProvisioningStatus
{
    Pending = 1,
    Provisioning = 2,
    Provisioned = 3,
    Failed = 4,
    Migrating = 5
}
