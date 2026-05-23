using CliniKey.Application.Abstractions.Tenancy;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using System.Data.Common;

namespace CliniKey.Infrastructure.Persistence;

internal sealed class TenantConnectionInterceptor : DbConnectionInterceptor
{
    private readonly ITenantContext _tenantContext;

    public TenantConnectionInterceptor(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        SetSearchPath(connection);
        base.ConnectionOpened(connection, eventData);
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await SetSearchPathAsync(connection, cancellationToken);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    private void SetSearchPath(DbConnection connection)
    {
        if (!_tenantContext.IsResolved || string.IsNullOrWhiteSpace(_tenantContext.SchemaName))
        {
            return;
        }

        using var command = connection.CreateCommand();
        command.CommandText = $"SET search_path TO {PostgresIdentifier.QuoteSchema(_tenantContext.SchemaName)}, shared, public;";
        command.ExecuteNonQuery();
    }

    private async Task SetSearchPathAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        if (!_tenantContext.IsResolved || string.IsNullOrWhiteSpace(_tenantContext.SchemaName))
        {
            return;
        }

        await using var command = connection.CreateCommand();
        command.CommandText = $"SET search_path TO {PostgresIdentifier.QuoteSchema(_tenantContext.SchemaName)}, shared, public;";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
