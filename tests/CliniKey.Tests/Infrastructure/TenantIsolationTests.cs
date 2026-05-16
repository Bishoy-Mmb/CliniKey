using CliniKey.Domain.Entities;
using CliniKey.Domain.ValueObjects;
using CliniKey.Domain.Enums;
using CliniKey.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace CliniKey.Tests.Infrastructure;

[Trait("Category", "Integration")]
public sealed class TenantIsolationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    private AppDbContext CreateDbContext(string schemaName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        var context = new AppDbContext(options);

#pragma warning disable EF1002 // Schema identifiers are hardcoded test constants, not user input
        context.Database.ExecuteSqlRaw(
            string.Concat("CREATE SCHEMA IF NOT EXISTS \"", schemaName, "\""));
        context.Database.ExecuteSqlRaw(
            string.Concat("SET search_path TO \"", schemaName, "\""));
#pragma warning restore EF1002

        return context;
    }

    [Fact]
    public async Task CrossTenantQuery_ReturnsZeroRows()
    {
        const string tenantA = "tenant_a";
        const string tenantB = "tenant_b";

        await using var contextA = CreateDbContext(tenantA);
        await contextA.Database.MigrateAsync();

        var name = PatientName.Create("Ahmed", "Hassan").Value;
        var phone = PhoneNumber.Create("01012345678").Value;
        var patient = Patient.Create(name, phone, new DateOnly(1990, 5, 15), Gender.Male, null);

        contextA.Patients.Add(patient);
        await contextA.SaveChangesAsync();

        var patientsInA = await contextA.Patients.AsNoTracking().ToListAsync();
        patientsInA.Should().HaveCount(1);

        await using var contextB = CreateDbContext(tenantB);
        await contextB.Database.MigrateAsync();

        var patientsInB = await contextB.Patients.AsNoTracking().ToListAsync();
        patientsInB.Should().BeEmpty("tenant B must not see tenant A data");
    }
}
