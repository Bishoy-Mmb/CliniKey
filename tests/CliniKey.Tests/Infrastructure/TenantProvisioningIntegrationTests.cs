using CliniKey.Application.Abstractions.Tenancy;
using CliniKey.Domain.Entities;
using CliniKey.Infrastructure.Persistence;
using CliniKey.SharedKernel.Primitives;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CliniKey.Tests.Infrastructure;

[Trait("Category", "Integration")]
public sealed class TenantProvisioningIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 5, 23, 10, 0, 0, TimeSpan.Zero));

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task ProvisionAsync_CreatesSchemaAndAppliesBaselineMigration()
    {
        await using var sharedContext = CreateSharedDbContext();
        await sharedContext.Database.EnsureCreatedAsync();
        var clinic = Clinic.Create("Cairo Dental Center", "01112345678", "15 Tahrir St", _clock).Value;
        sharedContext.Clinics.Add(clinic);
        await sharedContext.SaveChangesAsync();

        var migrationService = new TenantMigrationService(_postgres.GetConnectionString());
        var service = new TenantProvisioningService(
            migrationService,
            sharedContext,
            _clock,
            CreateTenancyOptions(),
            NullLogger<TenantProvisioningService>.Instance);

        var result = await service.ProvisionAsync(clinic, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(TenantMigrationService.BaselineMigration);
        (await SchemaExistsAsync(clinic.SchemaName)).Should().BeTrue();
        (await MigrationExistsAsync(clinic.SchemaName, TenantMigrationService.BaselineMigration)).Should().BeTrue();
    }

    [Fact]
    public async Task ProvisionAsync_WhenMigrationFails_DropsCreatedSchema()
    {
        await using var sharedContext = CreateSharedDbContext();
        await sharedContext.Database.EnsureCreatedAsync();
        var clinic = Clinic.Create("Alex Dental Center", "01112345679", "10 Sea Rd", _clock).Value;
        sharedContext.Clinics.Add(clinic);
        await sharedContext.SaveChangesAsync();

        var service = new TenantProvisioningService(
            new FailingTenantMigrationService(),
            sharedContext,
            _clock,
            CreateTenancyOptions(),
            NullLogger<TenantProvisioningService>.Instance);

        var result = await service.ProvisionAsync(clinic, null);

        result.IsFailure.Should().BeTrue();
        (await SchemaExistsAsync(clinic.SchemaName)).Should().BeFalse();
    }

    private SharedDbContext CreateSharedDbContext()
    {
        var options = new DbContextOptionsBuilder<SharedDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        return new SharedDbContext(options);
    }

    private IOptions<TenancyOptions> CreateTenancyOptions()
    {
        return Options.Create(new TenancyOptions { ConnectionString = _postgres.GetConnectionString() });
    }

    private async Task<bool> SchemaExistsAsync(string schemaName)
    {
        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT EXISTS (SELECT 1 FROM information_schema.schemata WHERE schema_name = @schema_name);",
            connection);
        command.Parameters.AddWithValue("schema_name", schemaName);
        return (bool)(await command.ExecuteScalarAsync() ?? false);
    }

    private async Task<bool> MigrationExistsAsync(string schemaName, string migrationId)
    {
        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            $"""
            SELECT EXISTS (
                SELECT 1
                FROM {PostgresIdentifier.QuoteSchema(schemaName)}."__EFMigrationsHistory"
                WHERE "MigrationId" = @migration_id);
            """,
            connection);
        command.Parameters.AddWithValue("migration_id", migrationId);
        return (bool)(await command.ExecuteScalarAsync() ?? false);
    }

    private sealed class FailingTenantMigrationService : ITenantMigrationService
    {
        public string ExpectedMigration => TenantMigrationService.BaselineMigration;

        public Task<Result<string?>> ApplyMigrationsAsync(string schemaName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.Failure<string?>(CliniKey.Domain.Errors.TenantErrors.MigrationFailed));
        }

        public Task<Result<IReadOnlyList<TenantMigrationResult>>> ApplyPendingMigrationsAsync(
            IReadOnlyCollection<TenantMigrationTarget> targets,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.Failure<IReadOnlyList<TenantMigrationResult>>(
                CliniKey.Domain.Errors.TenantErrors.MigrationFailed));
        }
    }
}
