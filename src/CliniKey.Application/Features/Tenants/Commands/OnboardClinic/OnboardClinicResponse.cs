using CliniKey.Domain.Entities;

namespace CliniKey.Application.Features.Tenants.Commands.OnboardClinic;

public sealed record OnboardClinicResponse(
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
    DateTime CreatedAtUtc)
{
    public static OnboardClinicResponse FromTenantAndClinic(Tenant tenant, Clinic clinic)
    {
        return new OnboardClinicResponse(
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
            clinic.CreatedAtUtc);
    }
}
