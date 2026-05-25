using CliniKey.Domain.Entities;
using CliniKey.Domain.Enums;
using CliniKey.Domain.ValueObjects;
using CliniKey.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CliniKey.Tests.Infrastructure;

[Trait("Category", "Integration")]
public sealed class TenantSchemaSwitchingTests : IAsyncLifetime
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
    public async Task EfCore_ResolvedTenantSearchPath_IsolatesPatientsBySchema()
    {
        const string tenantA = "tenant_ef_a";
        const string tenantB = "tenant_ef_b";
        await using var dataSource = NpgsqlDataSource.Create(_postgres.GetConnectionString());
        var migrationService = new TenantMigrationService(dataSource, Options.Create(new TenancyOptions()));
        (await migrationService.ApplyMigrationsAsync(tenantA)).IsSuccess.Should().BeTrue();
        (await migrationService.ApplyMigrationsAsync(tenantB)).IsSuccess.Should().BeTrue();

        await using (var contextA = CreateContext(tenantA))
        {
            var name = PatientName.Create("Ahmed", "Hassan").Value;
            var phone = PhoneNumber.Create("01012345678").Value;
            contextA.Patients.Add(Patient.Create(name, phone, new DateOnly(1990, 5, 15), Gender.Male, _clock));
            await contextA.SaveChangesAsync();
        }

        await using var contextB = CreateContext(tenantB);
        var patientsInB = await contextB.Patients.AsNoTracking().ToListAsync();

        patientsInB.Should().BeEmpty("tenant B must not see tenant A rows through a pooled EF connection");
    }

    private AppDbContext CreateContext(string schemaName)
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
