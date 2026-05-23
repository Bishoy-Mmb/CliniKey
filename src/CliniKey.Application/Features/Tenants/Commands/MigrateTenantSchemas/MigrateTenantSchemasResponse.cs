namespace CliniKey.Application.Features.Tenants.Commands.MigrateTenantSchemas;

public sealed record MigrateTenantSchemasResponse(
    DateTime StartedAtUtc,
    DateTime FinishedAtUtc,
    string ExpectedMigration,
    IReadOnlyList<TenantMigrationResultResponse> Results);

public sealed record TenantMigrationResultResponse(
    Guid ClinicId,
    string SchemaName,
    string Status,
    string? PreviousMigration,
    string? CurrentMigration,
    string? Message);
