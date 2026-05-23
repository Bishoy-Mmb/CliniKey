using CliniKey.Domain.Enums;
using CliniKey.Infrastructure.Persistence;
using FluentAssertions;
using Testcontainers.PostgreSql;

namespace CliniKey.Tests.Infrastructure;

[Trait("Category", "Integration")]
public sealed class TenantDapperConnectionTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task CreateTenantConnection_SetsSearchPathForResolvedTenant()
    {
        const string schemaName = "tenant_dapper_a";
        var migrationService = new TenantMigrationService(_postgres.GetConnectionString());
        (await migrationService.ApplyMigrationsAsync(schemaName)).IsSuccess.Should().BeTrue();
        var tenantContext = new TenantContext();
        tenantContext.Resolve(Guid.NewGuid(), schemaName, ClinicStatus.Active, TenantSchemaHealthStatus.Healthy);
        var factory = new DbConnectionFactory(_postgres.GetConnectionString(), tenantContext);

        using var connection = factory.CreateTenantConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SHOW search_path;";

        var searchPath = (string?)command.ExecuteScalar();

        searchPath.Should().Be($"{schemaName}, shared, public");
    }
}
