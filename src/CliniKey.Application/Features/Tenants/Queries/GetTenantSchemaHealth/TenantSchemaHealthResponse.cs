namespace CliniKey.Application.Features.Tenants.Queries.GetTenantSchemaHealth;

public sealed record TenantSchemaHealthResponse(
    string ExpectedMigration,
    IReadOnlyList<TenantSchemaHealthItemResponse> Items);

public sealed record TenantSchemaHealthItemResponse(
    Guid ClinicId,
    string SchemaName,
    string ClinicStatus,
    string SchemaHealthStatus,
    string? CurrentMigration,
    DateTime? LastSchemaVerifiedAtUtc,
    string? FailureReason);
