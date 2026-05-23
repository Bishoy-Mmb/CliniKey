using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Application.Abstractions.Tenancy;

public sealed record TenantMigrationTarget(Guid ClinicId, string SchemaName, bool IncludeInactive);

public sealed record TenantMigrationResult(
    Guid ClinicId,
    string SchemaName,
    string Status,
    string? PreviousMigration,
    string? CurrentMigration,
    string? Message);

public interface ITenantMigrationService
{
    string ExpectedMigration { get; }

    Task<Result<string?>> ApplyMigrationsAsync(string schemaName, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<TenantMigrationResult>>> ApplyPendingMigrationsAsync(
        IReadOnlyCollection<TenantMigrationTarget> targets,
        CancellationToken cancellationToken = default);
}
