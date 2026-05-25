using CliniKey.Domain.Entities;

namespace CliniKey.Application.Features.Tenants.Queries;

public sealed record TenantResponse(
    Guid TenantId,
    Guid ClinicId,
    string Name,
    string Phone,
    string Address,
    string SchemaName,
    string Status,
    string TenantStatus,
    string ProvisioningStatus,
    string SchemaHealthStatus,
    string? CurrentMigration,
    DateTime? LastSchemaVerifiedAtUtc,
    DateTime? TenantDeactivatedAtUtc,
    Guid? TenantDeactivatedByUserId,
    DateTime? DeactivatedAtUtc,
    Guid? DeactivatedByUserId,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc)
{
    public static TenantResponse FromTenantAndClinic(Tenant tenant, Clinic clinic)
    {
        return new TenantResponse(
            tenant.Id,
            clinic.Id,
            clinic.Name,
            clinic.Phone.Value,
            clinic.Address,
            tenant.SchemaName,
            clinic.Status.ToString(),
            tenant.Status.ToString(),
            tenant.ProvisioningStatus.ToString(),
            tenant.SchemaHealthStatus.ToString(),
            tenant.CurrentMigration,
            tenant.LastSchemaVerifiedAtUtc,
            tenant.DeactivatedAtUtc,
            tenant.DeactivatedByUserId,
            clinic.DeactivatedAtUtc,
            clinic.DeactivatedByUserId,
            clinic.CreatedAtUtc,
            clinic.UpdatedAtUtc);
    }
}

public sealed record TenantListItemResponse(
    Guid TenantId,
    Guid ClinicId,
    string Name,
    string Phone,
    string Address,
    string SchemaName,
    string Status,
    string TenantStatus,
    string ProvisioningStatus,
    string SchemaHealthStatus,
    DateTime? LastSchemaVerifiedAtUtc)
{
    public static TenantListItemResponse FromTenant(Tenant tenant)
    {
        var clinic = tenant.Clinics.OrderBy(c => c.CreatedAtUtc).First();
        return new TenantListItemResponse(
            tenant.Id,
            clinic.Id,
            clinic.Name,
            clinic.Phone.Value,
            clinic.Address,
            tenant.SchemaName,
            clinic.Status.ToString(),
            tenant.Status.ToString(),
            tenant.ProvisioningStatus.ToString(),
            tenant.SchemaHealthStatus.ToString(),
            tenant.LastSchemaVerifiedAtUtc);
    }
}

public sealed record TenantListResponse(
    IReadOnlyList<TenantListItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
