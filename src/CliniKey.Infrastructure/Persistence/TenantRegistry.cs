using CliniKey.Application.Abstractions.Tenancy;
using CliniKey.Domain.Enums;
using CliniKey.Domain.Errors;
using CliniKey.SharedKernel.Primitives;
using Dapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Npgsql;

namespace CliniKey.Infrastructure.Persistence;

internal sealed class TenantRegistry : ITenantRegistry
{
    private static readonly TimeSpan MinimumCacheDuration = TimeSpan.FromSeconds(1);

    private readonly NpgsqlDataSource _dataSource;
    private readonly IMemoryCache _cache;
    private readonly TenancyOptions _options;

    public TenantRegistry(NpgsqlDataSource dataSource, IMemoryCache cache, IOptions<TenancyOptions> options)
    {
        _dataSource = dataSource;
        _cache = cache;
        _options = options.Value;
    }

    public async Task<Result<TenantRegistryEntry>> ResolveAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(tenantId);
        if (_cache.TryGetValue(cacheKey, out TenantRegistryEntry? cached) && cached is not null)
        {
            return Validate(cached);
        }

        var sharedSchema = PostgresIdentifier.QuoteSchema(_options.SharedSchema);
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        var tenant = await connection.QueryFirstOrDefaultAsync<TenantRegistryRow>(
            new CommandDefinition(
                $"""
                SELECT
                    id AS TenantId,
                    schema_name AS SchemaName,
                    status AS TenantStatus,
                    provisioning_status AS ProvisioningStatus,
                    schema_health_status AS SchemaHealthStatus,
                    current_migration AS CurrentMigration
                FROM {sharedSchema}.tenants
                WHERE id = @TenantId
                """,
                new { TenantId = tenantId },
                cancellationToken: cancellationToken));

        if (tenant is null)
        {
            return Result.Failure<TenantRegistryEntry>(TenantErrors.NotFound);
        }

        var entry = new TenantRegistryEntry(
            tenant.TenantId,
            tenant.SchemaName,
            Enum.Parse<TenantStatus>(tenant.TenantStatus),
            Enum.Parse<TenantProvisioningStatus>(tenant.ProvisioningStatus),
            Enum.Parse<TenantSchemaHealthStatus>(tenant.SchemaHealthStatus),
            tenant.CurrentMigration);

        _cache.Set(
            cacheKey,
            entry,
            TimeSpan.FromSeconds(Math.Max(_options.TenantRegistryCacheSeconds, (int)MinimumCacheDuration.TotalSeconds)));

        return Validate(entry);
    }

    public Task InvalidateAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        _cache.Remove(GetCacheKey(tenantId));
        return Task.CompletedTask;
    }

    private static Result<TenantRegistryEntry> Validate(TenantRegistryEntry entry)
    {
        if (entry.TenantStatus != TenantStatus.Active)
        {
            return Result.Failure<TenantRegistryEntry>(TenantErrors.Inactive);
        }

        if (entry.ProvisioningStatus != TenantProvisioningStatus.Provisioned)
        {
            return Result.Failure<TenantRegistryEntry>(TenantErrors.NotProvisioned);
        }

        if (entry.SchemaHealthStatus != TenantSchemaHealthStatus.Healthy)
        {
            return Result.Failure<TenantRegistryEntry>(TenantErrors.SchemaUnhealthy);
        }

        return entry;
    }

    private static string GetCacheKey(Guid tenantId) => $"tenant-registry:{tenantId:N}";

    private sealed record TenantRegistryRow(
        Guid TenantId,
        string SchemaName,
        string TenantStatus,
        string ProvisioningStatus,
        string SchemaHealthStatus,
        string? CurrentMigration);
}
