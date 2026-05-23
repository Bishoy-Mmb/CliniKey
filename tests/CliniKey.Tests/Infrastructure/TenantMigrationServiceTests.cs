using CliniKey.Application.Abstractions.Tenancy;
using CliniKey.Domain.Errors;
using CliniKey.Infrastructure.Persistence;
using FluentAssertions;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CliniKey.Tests.Infrastructure;

[Trait("Category", "Integration")]
public sealed class TenantMigrationServiceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();
    private readonly TenancyOptions _options = new() { ProvisioningLockKey = 424242 };

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task ApplyPendingMigrationsAsync_NewSchema_CreatesBaselineAndReportsDrift()
    {
        var service = CreateService();
        var clinicId = Guid.NewGuid();

        var result = await service.ApplyPendingMigrationsAsync(
            [new TenantMigrationTarget(clinicId, "tenant_migration_success", IncludeInactive: true)]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].ClinicId.Should().Be(clinicId);
        result.Value[0].Status.Should().Be("Succeeded");
        result.Value[0].PreviousMigration.Should().BeNull();
        result.Value[0].CurrentMigration.Should().Be(TenantMigrationService.BaselineMigration);
        (await TableExistsAsync("tenant_migration_success", "patients")).Should().BeTrue();
        (await GetColumnDataTypeAsync("tenant_migration_success", "treatment_items", "tooth_code"))
            .Should().Be("integer");
        (await ConstraintExistsAsync(
            "tenant_migration_success",
            "invoice_lines",
            "FK_invoice_lines_invoices_invoice_id")).Should().BeTrue();
        (await ConstraintExistsAsync(
            "tenant_migration_success",
            "payments",
            "FK_payments_invoices_invoice_id")).Should().BeTrue();
        (await ConstraintExistsAsync(
            "tenant_migration_success",
            "treatment_items",
            "FK_treatment_items_treatment_plans_treatment_plan_id")).Should().BeTrue();
        (await IndexExistsAsync("tenant_migration_success", "IX_invoice_lines_invoice_id")).Should().BeTrue();
        (await IndexExistsAsync("tenant_migration_success", "IX_payments_invoice_id")).Should().BeTrue();
        (await IndexExistsAsync("tenant_migration_success", "IX_treatment_items_treatment_plan_id")).Should().BeTrue();
    }

    [Fact]
    public async Task ApplyPendingMigrationsAsync_InvalidSchema_ReturnsFailedResult()
    {
        var service = CreateService();

        var result = await service.ApplyPendingMigrationsAsync(
            [new TenantMigrationTarget(Guid.NewGuid(), "bad-schema", IncludeInactive: true)]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].Status.Should().Be("Failed");
        result.Value[0].Message.Should().Be(TenantErrors.MigrationFailed.Description);
    }

    [Fact]
    public async Task ApplyPendingMigrationsAsync_WhenLockHeld_ReturnsMigrationAlreadyRunning()
    {
        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using var command = new NpgsqlCommand(
            $"SELECT pg_advisory_xact_lock({_options.ProvisioningLockKey});",
            connection,
            transaction);
        await command.ExecuteNonQueryAsync();

        var service = CreateService();
        var result = await service.ApplyPendingMigrationsAsync(
            [new TenantMigrationTarget(Guid.NewGuid(), "tenant_lock_waiting", IncludeInactive: true)]);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.MigrationAlreadyRunning);
    }

    private TenantMigrationService CreateService()
    {
        return new TenantMigrationService(_postgres.GetConnectionString(), _options);
    }

    private async Task<bool> TableExistsAsync(string schemaName, string tableName)
    {
        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.tables
                WHERE table_schema = @schema_name
                  AND table_name = @table_name
            );
            """,
            connection);
        command.Parameters.AddWithValue("schema_name", schemaName);
        command.Parameters.AddWithValue("table_name", tableName);

        return (bool)(await command.ExecuteScalarAsync() ?? false);
    }

    private async Task<string?> GetColumnDataTypeAsync(string schemaName, string tableName, string columnName)
    {
        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT data_type
            FROM information_schema.columns
            WHERE table_schema = @schema_name
              AND table_name = @table_name
              AND column_name = @column_name;
            """,
            connection);
        command.Parameters.AddWithValue("schema_name", schemaName);
        command.Parameters.AddWithValue("table_name", tableName);
        command.Parameters.AddWithValue("column_name", columnName);

        return (string?)await command.ExecuteScalarAsync();
    }

    private async Task<bool> ConstraintExistsAsync(string schemaName, string tableName, string constraintName)
    {
        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT EXISTS (
                SELECT 1
                FROM pg_constraint c
                JOIN pg_class t ON t.oid = c.conrelid
                JOIN pg_namespace n ON n.oid = t.relnamespace
                WHERE n.nspname = @schema_name
                  AND t.relname = @table_name
                  AND c.conname = @constraint_name
            );
            """,
            connection);
        command.Parameters.AddWithValue("schema_name", schemaName);
        command.Parameters.AddWithValue("table_name", tableName);
        command.Parameters.AddWithValue("constraint_name", constraintName);

        return (bool)(await command.ExecuteScalarAsync() ?? false);
    }

    private async Task<bool> IndexExistsAsync(string schemaName, string indexName)
    {
        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT EXISTS (
                SELECT 1
                FROM pg_indexes
                WHERE schemaname = @schema_name
                  AND indexname = @index_name
            );
            """,
            connection);
        command.Parameters.AddWithValue("schema_name", schemaName);
        command.Parameters.AddWithValue("index_name", indexName);

        return (bool)(await command.ExecuteScalarAsync() ?? false);
    }
}
