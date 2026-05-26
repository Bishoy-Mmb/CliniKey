using CliniKey.Domain.Enums;
using CliniKey.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CliniKey.Tests.Infrastructure;

[Trait("Category", "Integration")]
public sealed class TenantDapperConnectionTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();

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
        await using var dataSource = NpgsqlDataSource.Create(_postgres.GetConnectionString());
        var tenancyOptions = Options.Create(new TenancyOptions());
        var migrationService = new TenantMigrationService(dataSource, tenancyOptions);
        (await migrationService.ApplyMigrationsAsync(schemaName)).IsSuccess.Should().BeTrue();
        var tenantContext = new TenantContext();
        tenantContext.Resolve(Guid.NewGuid(), schemaName, TenantStatus.Active, TenantSchemaHealthStatus.Healthy);
        var factory = new DbConnectionFactory(dataSource, tenantContext, tenancyOptions);

        using var connection = factory.CreateTenantConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SHOW search_path;";

        var searchPath = (string?)command.ExecuteScalar();

        searchPath.Should().Be($"{schemaName}, shared, public");
    }
}
