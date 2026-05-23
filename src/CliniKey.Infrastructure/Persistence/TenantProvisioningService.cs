using CliniKey.Application.Abstractions.Tenancy;
using CliniKey.Domain.Entities;
using CliniKey.Domain.Errors;
using CliniKey.SharedKernel.Primitives;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CliniKey.Infrastructure.Persistence;

internal sealed class TenantProvisioningService : ITenantProvisioningService
{
    private readonly string _connectionString;
    private readonly ITenantMigrationService _tenantMigrationService;
    private readonly SharedDbContext _sharedDbContext;
    private readonly TimeProvider _clock;
    private readonly TenancyOptions _options;

    public TenantProvisioningService(
        string connectionString,
        ITenantMigrationService tenantMigrationService,
        SharedDbContext sharedDbContext,
        TimeProvider clock,
        TenancyOptions options)
    {
        _connectionString = connectionString;
        _tenantMigrationService = tenantMigrationService;
        _sharedDbContext = sharedDbContext;
        _clock = clock;
        _options = options;
    }

    public async Task<Result<string?>> ProvisionAsync(
        Clinic clinic,
        Guid? operatorUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            await ExecuteAsync(
                connection,
                transaction,
                $"SELECT pg_advisory_xact_lock({_options.ProvisioningLockKey});",
                cancellationToken);

            var schema = PostgresIdentifier.QuoteSchema(clinic.SchemaName);
            await ExecuteAsync(connection, transaction, $"CREATE SCHEMA IF NOT EXISTS {schema};", cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            await AddAuditLogAsync(clinic, "CreateSchema", "Succeeded", null, operatorUserId, cancellationToken);

            var migrationResult = await _tenantMigrationService.ApplyMigrationsAsync(clinic.SchemaName, cancellationToken);
            if (migrationResult.IsFailure)
            {
                await DropSchemaAsync(clinic.SchemaName, cancellationToken);
                await AddAuditLogAsync(clinic, "ApplyMigrations", "Failed", migrationResult.Error.Description, operatorUserId, cancellationToken);
                return Result.Failure<string?>(TenantErrors.ProvisioningFailed);
            }

            await AddAuditLogAsync(clinic, "ApplyMigrations", "Succeeded", migrationResult.Value, operatorUserId, cancellationToken);
            return migrationResult.Value;
        }
        catch (Exception ex) when (ex is NpgsqlException or DbUpdateException or ArgumentException)
        {
            await DropSchemaAsync(clinic.SchemaName, cancellationToken);
            await AddAuditLogAsync(clinic, "Onboard", "Failed", ex.Message, operatorUserId, cancellationToken);
            return Result.Failure<string?>(TenantErrors.ProvisioningFailed);
        }
    }

    public async Task RecordLifecycleAuditAsync(
        Clinic clinic,
        string operation,
        string status,
        string? message,
        Guid? operatorUserId,
        CancellationToken cancellationToken = default)
    {
        await AddAuditLogAsync(clinic, operation, status, message, operatorUserId, cancellationToken);
    }

    private async Task AddAuditLogAsync(
        Clinic clinic,
        string operation,
        string status,
        string? message,
        Guid? operatorUserId,
        CancellationToken cancellationToken)
    {
        _sharedDbContext.TenantProvisioningAuditLogs.Add(TenantProvisioningAuditLog.Create(
            clinic.Id,
            clinic.SchemaName,
            operation,
            status,
            message,
            operatorUserId,
            _clock));

        await _sharedDbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task DropSchemaAsync(string schemaName, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            var schema = PostgresIdentifier.QuoteSchema(schemaName);
            await using var command = new NpgsqlCommand($"DROP SCHEMA IF EXISTS {schema} CASCADE;", connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch
        {
            // Best-effort cleanup; the caller receives the provisioning failure.
        }
    }

    private static async Task ExecuteAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
