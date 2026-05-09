using System.Data;
using CliniKey.Application.Abstractions.Data;
using Npgsql;

namespace CliniKey.Infrastructure.Persistence;

internal sealed class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IDbConnection CreateConnection()
    {
        var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        return connection;
    }
}
