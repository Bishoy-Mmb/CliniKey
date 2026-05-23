using CliniKey.Domain.Entities;
using CliniKey.Domain.Enums;
using CliniKey.Domain.ValueObjects;
using CliniKey.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Testcontainers.PostgreSql;

namespace CliniKey.Tests.Infrastructure;

[Trait("Category", "Integration")]
public sealed class TenantConcurrentIsolationTests : IAsyncLifetime
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
    public async Task EfCore_ConcurrentRequestsAcrossTenTenants_DoNotShareSearchPathState()
    {
        var schemas = Enumerable.Range(0, 10)
            .Select(i => $"tenant_{i:00}")
            .ToArray();
        var migrationService = new TenantMigrationService(_postgres.GetConnectionString());

        foreach (var schema in schemas)
        {
            (await migrationService.ApplyMigrationsAsync(schema)).IsSuccess.Should().BeTrue();
        }

        await Task.WhenAll(schemas.Select((schema, index) => InsertPatientAsync(schema, index)));
        var counts = await Task.WhenAll(schemas.Select(CountPatientsAsync));

        counts.Should().OnlyContain(count => count == 1);
    }

    private async Task InsertPatientAsync(string schemaName, int index)
    {
        await using var context = CreateContext(schemaName);
        var name = PatientName.Create("Tenant", index.ToString("00")).Value;
        var phone = PhoneNumber.Create($"01012345{index:000}").Value;
        context.Patients.Add(Patient.Create(name, phone, new DateOnly(1990, 5, 15), Gender.Male, _clock));
        await context.SaveChangesAsync();
    }

    private async Task<int> CountPatientsAsync(string schemaName)
    {
        await using var context = CreateContext(schemaName);
        return await context.Patients.CountAsync();
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
