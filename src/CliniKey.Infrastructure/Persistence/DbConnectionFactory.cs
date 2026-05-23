using System.Data;
using CliniKey.Application.Abstractions.Data;
using CliniKey.Application.Abstractions.Tenancy;
using Npgsql;

namespace CliniKey.Infrastructure.Persistence;

internal sealed class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;
    private readonly ITenantContext _tenantContext;

    public DbConnectionFactory(string connectionString, ITenantContext tenantContext)
    {
        _connectionString = connectionString;
        _tenantContext = tenantContext;
    }

    public IDbConnection CreateConnection()
    {
        var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        return connection;
    }

    public IDbConnection CreateTenantConnection()
    {
        if (!_tenantContext.IsResolved || string.IsNullOrWhiteSpace(_tenantContext.SchemaName))
        {
            throw new InvalidOperationException("Tenant context must be resolved before opening a tenant connection.");
        }

        var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $"SET search_path TO {PostgresIdentifier.QuoteSchema(_tenantContext.SchemaName)}, shared, public;";
        command.ExecuteNonQuery();

        return connection;
    }
}
