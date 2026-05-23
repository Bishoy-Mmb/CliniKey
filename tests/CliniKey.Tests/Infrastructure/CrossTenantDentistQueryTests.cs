using CliniKey.Domain.Entities;
using CliniKey.Domain.Enums;
using CliniKey.Infrastructure.Persistence;
using CliniKey.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Npgsql;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Testcontainers.PostgreSql;

namespace CliniKey.Tests.Infrastructure;

[Trait("Category", "Integration")]
public sealed class CrossTenantDentistQueryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();
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
        await using (var sharedContext = CreateSharedContext())
        {
            await sharedContext.Database.EnsureCreatedAsync();
            var dentist = Dentist.Create("Dr. Shared", "General Dentistry", "LIC-SHARED-001", _clock).Value;
            sharedContext.Dentists.Add(dentist);
            await sharedContext.SaveChangesAsync();

            await new TenantMigrationService(_postgres.GetConnectionString()).ApplyMigrationsAsync("tenant_shared_a");
            await new TenantMigrationService(_postgres.GetConnectionString()).ApplyMigrationsAsync("tenant_shared_b");

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
        await using (var sharedContext = CreateSharedContext())
        {
            await sharedContext.Database.EnsureCreatedAsync();
            var clinic = Clinic.Create(
                Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"),
                "Invite Clinic",
                "01111111111",
                "15 Tahrir St",
                "tenant_invite_shared",
                _clock).Value;
            sharedContext.Clinics.Add(clinic);
            await sharedContext.SaveChangesAsync();
        }

        await new TenantMigrationService(_postgres.GetConnectionString()).ApplyMigrationsAsync("tenant_invite_shared");

        await using (var tenantContext = CreateAppContext("tenant_invite_shared"))
        {
            var clinic = await tenantContext.Clinics.SingleAsync(c => c.SchemaName == "tenant_invite_shared");
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
            "SELECT to_regclass('tenant_invite_shared.dentists')::text");

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
        tenantContext.Resolve(Guid.NewGuid(), schemaName, ClinicStatus.Active, TenantSchemaHealthStatus.Healthy);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .AddInterceptors(new TenantConnectionInterceptor(tenantContext))
            .Options;

        return new AppDbContext(options);
    }
}
