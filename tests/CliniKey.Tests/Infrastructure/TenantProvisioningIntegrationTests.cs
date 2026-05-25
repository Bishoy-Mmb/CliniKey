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
        var tenant = CreateTenant("Cairo Dental Center");
        var clinic = CreateClinic(tenant.Id, "Cairo Dental Center", "01112345678", "15 Tahrir St");
        sharedContext.Tenants.Add(tenant);
        sharedContext.Clinics.Add(clinic);
        await sharedContext.SaveChangesAsync();

        await using var dataSource = NpgsqlDataSource.Create(_postgres.GetConnectionString());
        var tenancyOptions = CreateTenancyOptions();
        var migrationService = new TenantMigrationService(dataSource, tenancyOptions);
        var service = new TenantProvisioningService(
            dataSource,
            migrationService,
            sharedContext,
            _clock,
            tenancyOptions,
            NullLogger<TenantProvisioningService>.Instance);

        var result = await service.ProvisionAsync(tenant, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(TenantMigrationService.BaselineMigration);
        (await SchemaExistsAsync(tenant.SchemaName)).Should().BeTrue();
        (await MigrationExistsAsync(tenant.SchemaName, TenantMigrationService.BaselineMigration)).Should().BeTrue();
    }

    [Fact]
    public async Task ProvisionAsync_WhenMigrationFails_DropsCreatedSchema()
    {
        await using var sharedContext = CreateSharedDbContext();
        await sharedContext.Database.EnsureCreatedAsync();
        var tenant = CreateTenant("Alex Dental Center");
        var clinic = CreateClinic(tenant.Id, "Alex Dental Center", "01112345679", "10 Sea Rd");
        sharedContext.Tenants.Add(tenant);
        sharedContext.Clinics.Add(clinic);
        await sharedContext.SaveChangesAsync();

        await using var dataSource = NpgsqlDataSource.Create(_postgres.GetConnectionString());
        var service = new TenantProvisioningService(
            dataSource,
            new FailingTenantMigrationService(),
            sharedContext,
            _clock,
            CreateTenancyOptions(),
            NullLogger<TenantProvisioningService>.Instance);

        var result = await service.ProvisionAsync(tenant, null);

        result.IsFailure.Should().BeTrue();
        (await SchemaExistsAsync(tenant.SchemaName)).Should().BeFalse();
    }

    private SharedDbContext CreateSharedDbContext()
    {
        var options = new DbContextOptionsBuilder<SharedDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        return new SharedDbContext(options);
    }

    private Tenant CreateTenant(string name)
    {
        var tenantId = Guid.NewGuid();
        return Tenant.Create(tenantId, name, $"tenant_{tenantId:N}", _clock).Value;
    }

    private Clinic CreateClinic(Guid tenantId, string name, string phone, string address)
    {
        return Clinic.Create(
            Guid.NewGuid(),
            tenantId,
            name,
            phone,
            address,
            _clock).Value;
    }

    private IOptions<TenancyOptions> CreateTenancyOptions()
    {
        return Options.Create(new TenancyOptions());
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
