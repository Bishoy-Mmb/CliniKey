using CliniKey.Domain.Entities;

namespace CliniKey.Application.Features.Tenants.Commands.OnboardClinic;

public sealed record OnboardClinicResponse(
    Guid ClinicId,
    string Name,
    string Phone,
    string Address,
    string SchemaName,
    string Status,
    string ProvisioningStatus,
    string SchemaHealthStatus,
    string? CurrentMigration,
    DateTime CreatedAtUtc)
{
    public static OnboardClinicResponse FromClinic(Clinic clinic)
    {
        return new OnboardClinicResponse(
            clinic.Id,
            clinic.Name,
            clinic.Phone.Value,
            clinic.Address,
            clinic.SchemaName,
            clinic.Status.ToString(),
            clinic.ProvisioningStatus.ToString(),
            clinic.SchemaHealthStatus.ToString(),
            clinic.CurrentMigration,
            clinic.CreatedAtUtc);
    }
}
