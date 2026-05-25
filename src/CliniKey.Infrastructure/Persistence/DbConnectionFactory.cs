using System.Data;
using CliniKey.Application.Abstractions.Data;
using CliniKey.Application.Abstractions.Tenancy;
using Microsoft.Extensions.Options;
using Npgsql;

namespace CliniKey.Infrastructure.Persistence;

internal sealed class DbConnectionFactory : IDbConnectionFactory
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ITenantContext _tenantContext;
    private readonly TenancyOptions _options;

    public DbConnectionFactory(NpgsqlDataSource dataSource, ITenantContext tenantContext, IOptions<TenancyOptions> options)
    {
        _dataSource = dataSource;
        _tenantContext = tenantContext;
        _options = options.Value;
    }

    public IDbConnection CreateConnection()
    {
        var connection = _dataSource.CreateConnection();
        connection.Open();
        return connection;
    }

    public IDbConnection CreateTenantConnection()
    {
        if (!_tenantContext.IsResolved || string.IsNullOrWhiteSpace(_tenantContext.SchemaName))
        {
            throw new InvalidOperationException("Tenant context must be resolved before opening a tenant connection.");
        }

        var connection = _dataSource.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $"SET search_path TO {PostgresIdentifier.QuoteSchema(_tenantContext.SchemaName)}, {PostgresIdentifier.QuoteSchema(_options.SharedSchema)}, public;";
        command.ExecuteNonQuery();

        return connection;
    }
}
