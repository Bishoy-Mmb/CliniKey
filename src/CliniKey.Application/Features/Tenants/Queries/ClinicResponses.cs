using CliniKey.Domain.Entities;

namespace CliniKey.Application.Features.Tenants.Queries;

public sealed record ClinicResponse(
    Guid ClinicId,
    string Name,
    string Phone,
    string Address,
    string SchemaName,
    string Status,
    string ProvisioningStatus,
    string SchemaHealthStatus,
    string? CurrentMigration,
    DateTime? LastSchemaVerifiedAtUtc,
    DateTime? DeactivatedAtUtc,
    Guid? DeactivatedByUserId,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc)
{
    public static ClinicResponse FromClinic(Clinic clinic)
    {
        return new ClinicResponse(
            clinic.Id,
            clinic.Name,
            clinic.Phone.Value,
            clinic.Address,
            clinic.SchemaName,
            clinic.Status.ToString(),
            clinic.ProvisioningStatus.ToString(),
            clinic.SchemaHealthStatus.ToString(),
            clinic.CurrentMigration,
            clinic.LastSchemaVerifiedAtUtc,
            clinic.DeactivatedAtUtc,
            clinic.DeactivatedByUserId,
            clinic.CreatedAtUtc,
            clinic.UpdatedAtUtc);
    }
}

public sealed record ClinicListItemResponse(
    Guid ClinicId,
    string Name,
    string Phone,
    string Address,
    string SchemaName,
    string Status,
    string ProvisioningStatus,
    string SchemaHealthStatus,
    DateTime? LastSchemaVerifiedAtUtc)
{
    public static ClinicListItemResponse FromClinic(Clinic clinic)
    {
        return new ClinicListItemResponse(
            clinic.Id,
            clinic.Name,
            clinic.Phone.Value,
            clinic.Address,
            clinic.SchemaName,
            clinic.Status.ToString(),
            clinic.ProvisioningStatus.ToString(),
            clinic.SchemaHealthStatus.ToString(),
            clinic.LastSchemaVerifiedAtUtc);
    }
}

public sealed record ClinicListResponse(
    IReadOnlyList<ClinicListItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
