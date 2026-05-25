using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.Errors;

public static class TenantErrors
{
    public static readonly Error AlreadyActive = Error.Conflict(
        "Tenant.AlreadyActive",
        "Tenant is already active.");

    public static readonly Error AlreadyInactive = Error.Conflict(
        "Tenant.AlreadyInactive",
        "Tenant is already inactive.");

    public static readonly Error AlreadySuspended = Error.Conflict(
        "Tenant.AlreadySuspended",
        "Tenant is already suspended.");

    public static readonly Error InvalidName = Error.Validation(
        "Tenant.InvalidName",
        "Tenant name is invalid.");

    public static readonly Error InvalidSchemaName = Error.Validation(
        "Tenant.InvalidSchemaName",
        "Tenant schema name is invalid.");

    public static readonly Error InvalidMigration = Error.Validation(
        "Tenant.InvalidMigration",
        "Tenant migration value is invalid.");

    public static readonly Error InvalidSchemaHealth = Error.Validation(
        "Tenant.InvalidSchemaHealth",
        "Tenant schema health status is invalid.");

    public static readonly Error ClinicTenantMismatch = Error.Validation(
        "Tenant.ClinicTenantMismatch",
        "Clinic does not belong to this tenant.");

    public static readonly Error DuplicatePhone = Error.Conflict(
        "Tenant.DuplicatePhone",
        "Another clinic already uses this phone number.");

    public static readonly Error InvalidClaim = Error.Unauthorized(
        "Tenant.InvalidClaim",
        "A valid tenant_id claim is required.");

    public static readonly Error NotFound = Error.Unauthorized(
        "Tenant.NotFound",
        "The requested tenant could not be resolved.");

    public static readonly Error Inactive = Error.Forbidden(
        "Tenant.Inactive",
        "The tenant is inactive.");

    public static readonly Error NotProvisioned = Error.Conflict(
        "Tenant.NotProvisioned",
        "The tenant is not fully provisioned.");

    public static readonly Error SchemaUnhealthy = Error.Conflict(
        "Tenant.SchemaUnhealthy",
        "The tenant schema is not ready for use.");

    public static readonly Error ProvisioningFailed = Error.Failure(
        "Tenant.ProvisioningFailed",
        "Tenant schema provisioning failed and rollback completed.");

    public static readonly Error MigrationAlreadyRunning = Error.Conflict(
        "Tenant.MigrationAlreadyRunning",
        "Another tenant migration run is already in progress.");

    public static readonly Error MigrationFailed = Error.Failure(
        "Tenant.MigrationFailed",
        "One or more tenant schema migrations failed.");
}
