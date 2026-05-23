using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.Errors;

public static class TenantErrors
{
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
        "The clinic is inactive.");

    public static readonly Error SchemaUnhealthy = Error.Conflict(
        "Tenant.SchemaUnhealthy",
        "The clinic schema is not ready for use.");

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
