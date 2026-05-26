using CliniKey.Domain.Entities;
using CliniKey.Domain.Enums;
using CliniKey.Infrastructure.Persistence;
using CliniKey.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Npgsql;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Testcontainers.PostgreSql;

namespace CliniKey.Tests.Infrastructure;

[Trait("Category", "Integration")]
public sealed class CrossTenantDentistQueryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();
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
    public async Task DentistRepository_ReturnsSharedDentistUnderDifferentTenantSearchPaths()
    {
        await using var dataSource = NpgsqlDataSource.Create(_postgres.GetConnectionString());

        await using (var sharedContext = CreateSharedContext())
        {
            await sharedContext.Database.EnsureCreatedAsync();
            var dentist = Dentist.Create("Dr. Shared", "General Dentistry", "LIC-SHARED-001", _clock).Value;
            sharedContext.Dentists.Add(dentist);
            await sharedContext.SaveChangesAsync();

            var migrationService = new TenantMigrationService(dataSource, Options.Create(new TenancyOptions()));
            await migrationService.ApplyMigrationsAsync("tenant_shared_a");
            await migrationService.ApplyMigrationsAsync("tenant_shared_b");

            await using var tenantAContext = CreateAppContext("tenant_shared_a");
            await using var tenantBContext = CreateAppContext("tenant_shared_b");

            var dentistFromA = await new DentistRepository(tenantAContext).GetByIdAsync(dentist.Id);
            var dentistFromB = await new DentistRepository(tenantBContext).GetByIdAsync(dentist.Id);

            dentistFromA.Should().NotBeNull();
            dentistFromB.Should().NotBeNull();
            dentistFromA!.LicenseNumber.Should().Be("LIC-SHARED-001");
            dentistFromB!.LicenseNumber.Should().Be("LIC-SHARED-001");
        }
    }

    [Fact]
    public async Task InviteStaffStyleWrite_PersistsDentistAndClinicDentistInSharedSchema()
    {
        string schemaName;

        await using (var sharedContext = CreateSharedContext())
        {
            await sharedContext.Database.EnsureCreatedAsync();
            var tenantId = Guid.NewGuid();
            var schemaNameLocal = $"tenant_{tenantId:N}";
            var tenant = Tenant.Create(tenantId, "Invite Practice", schemaNameLocal, _clock).Value;
            var clinic = Clinic.Create(
                Guid.NewGuid(),
                tenant.Id,
                "Invite Clinic",
                "01111111111",
                "15 Tahrir St",
                _clock).Value;
            schemaName = tenant.SchemaName;
            sharedContext.Tenants.Add(tenant);
            sharedContext.Clinics.Add(clinic);
            await sharedContext.SaveChangesAsync();
        }

        await using var dataSource = NpgsqlDataSource.Create(_postgres.GetConnectionString());
        await new TenantMigrationService(dataSource, Options.Create(new TenancyOptions())).ApplyMigrationsAsync(schemaName);

        await using (var tenantContext = CreateAppContext(schemaName))
        {
            var tenant = await tenantContext.Tenants.SingleAsync(t => t.SchemaName == schemaName);
            var clinic = await tenantContext.Clinics.SingleAsync(c => c.TenantId == tenant.Id);
            var dentist = Dentist.Create("Dr. Invite Shared", "Orthodontics", "LIC-SHARED-INVITE", _clock).Value;

            new DentistRepository(tenantContext).Add(dentist);
            clinic.AddDentist(dentist.Id).IsSuccess.Should().BeTrue();
            await tenantContext.SaveChangesAsync();
        }

        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        var sharedDentistCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM shared.dentists WHERE license_number = @LicenseNumber",
            new { LicenseNumber = "LIC-SHARED-INVITE" });
        var sharedAssociationCount = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM shared.clinic_dentists cd
            JOIN shared.dentists d ON d.id = cd.dentist_id
            WHERE d.license_number = @LicenseNumber
            """,
            new { LicenseNumber = "LIC-SHARED-INVITE" });
        var tenantDentistTable = await connection.ExecuteScalarAsync<string?>(
            "SELECT to_regclass(@RegClass)::text",
            new { RegClass = $"{schemaName}.dentists" });

        sharedDentistCount.Should().Be(1);
        sharedAssociationCount.Should().Be(1);
        tenantDentistTable.Should().BeNull();
    }

    private SharedDbContext CreateSharedContext()
    {
        var options = new DbContextOptionsBuilder<SharedDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        return new SharedDbContext(options);
    }

    private AppDbContext CreateAppContext(string schemaName)
    {
        var tenantContext = new TenantContext();
        tenantContext.Resolve(Guid.NewGuid(), schemaName, TenantStatus.Active, TenantSchemaHealthStatus.Healthy);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .AddInterceptors(new TenantConnectionInterceptor(tenantContext))
            .Options;

        return new AppDbContext(options);
    }
}
