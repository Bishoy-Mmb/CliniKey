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
        var clinic = await connection.QueryFirstOrDefaultAsync<TenantRegistryRow>(
            new CommandDefinition(
                $"""
                SELECT
                    id AS TenantId,
                    schema_name AS SchemaName,
                    status AS ClinicStatus,
                    schema_health_status AS SchemaHealthStatus,
                    current_migration AS CurrentMigration
                FROM {sharedSchema}.clinics
                WHERE id = @TenantId
                """,
                new { TenantId = tenantId },
                cancellationToken: cancellationToken));

        if (clinic is null)
        {
            return Result.Failure<TenantRegistryEntry>(TenantErrors.NotFound);
        }

        var entry = new TenantRegistryEntry(
            clinic.TenantId,
            clinic.SchemaName,
            Enum.Parse<ClinicStatus>(clinic.ClinicStatus),
            Enum.Parse<TenantSchemaHealthStatus>(clinic.SchemaHealthStatus),
            clinic.CurrentMigration);

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
        if (entry.ClinicStatus != ClinicStatus.Active)
        {
            return Result.Failure<TenantRegistryEntry>(TenantErrors.Inactive);
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
        string ClinicStatus,
        string SchemaHealthStatus,
        string? CurrentMigration);
}
