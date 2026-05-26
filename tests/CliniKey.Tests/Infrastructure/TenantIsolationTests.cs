using CliniKey.Domain.Entities;
using CliniKey.Domain.ValueObjects;
using CliniKey.Domain.Enums;
using CliniKey.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Testcontainers.PostgreSql;

namespace CliniKey.Tests.Infrastructure;

[Trait("Category", "Integration")]
public sealed class TenantIsolationTests : IAsyncLifetime
{
    private readonly FakeTimeProvider _clock;
    private readonly DateTimeOffset _fixedTime = new(2026, 5, 21, 10, 0, 0, TimeSpan.Zero);
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .Build();

    public TenantIsolationTests()
    {
        _clock = new FakeTimeProvider(_fixedTime);
    }

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
        var baseConnectionString = _postgres.GetConnectionString();

        // Create the schema first using the base connection
        var tempOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(baseConnectionString)
            .Options;
        using (var tempContext = new AppDbContext(tempOptions))
        {
#pragma warning disable EF1002 // Schema identifiers are hardcoded test constants, not user input
            tempContext.Database.ExecuteSqlRaw(
                string.Concat("CREATE SCHEMA IF NOT EXISTS \"", schemaName, "\""));
#pragma warning restore EF1002
        }

        var connectionString = $"{baseConnectionString};Search Path={schemaName}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task CrossTenantQuery_ReturnsZeroRows()
    {
        const string tenantA = "tenant_a";
        const string tenantB = "tenant_b";

        await using var contextA = CreateDbContext(tenantA);

        // Generate and execute DDL to create tables in tenant A schema
        var ddl = contextA.Database.GenerateCreateScript();
        await contextA.Database.ExecuteSqlRawAsync(ddl);
        var tenantOnlyDdl = string.Join(
            ';',
            ddl.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Where(statement => !statement.Contains("shared.", StringComparison.OrdinalIgnoreCase))) + ';';

        var name = PatientName.Create("Ahmed", "Hassan").Value;
        var phone = PhoneNumber.Create("01012345678").Value;
        var patient = Patient.Create(name, phone, new DateOnly(1990, 5, 15), Gender.Male, _clock);

        contextA.Patients.Add(patient);
        await contextA.SaveChangesAsync();

        var patientsInA = await contextA.Patients.AsNoTracking().ToListAsync();
        patientsInA.Should().HaveCount(1);

        await using var contextB = CreateDbContext(tenantB);

        // Shared tables are database-wide; only tenant-scoped tables are recreated per schema.
        await contextB.Database.ExecuteSqlRawAsync(tenantOnlyDdl);

        var patientsInB = await contextB.Patients.AsNoTracking().ToListAsync();
        patientsInB.Should().BeEmpty("tenant B must not see tenant A data");
    }
}
