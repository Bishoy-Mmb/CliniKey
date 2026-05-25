using CliniKey.Domain.Entities;
using CliniKey.Domain.Enums;
using CliniKey.Domain.Errors;
using CliniKey.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CliniKey.Tests.Infrastructure;

[Trait("Category", "Integration")]
public sealed class TenantLifecycleAccessTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 5, 23, 10, 0, 0, TimeSpan.Zero));

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task TenantRegistry_InactiveClinic_ReturnsInactiveWithoutResolvingSchema()
    {
        await using var sharedContext = CreateSharedContext();
        await sharedContext.Database.EnsureCreatedAsync();
        var clinicId = Guid.NewGuid();
        var clinic = Clinic.Create(
            clinicId,
            "Inactive Clinic",
            "01122222222",
            "15 Tahrir St",
            $"tenant_{clinicId:N}",
            _clock).Value;
        clinic.MarkProvisioned("202605230001_InitialTenantOperationalSchema");
        clinic.Deactivate(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));
        sharedContext.Clinics.Add(clinic);
        await sharedContext.SaveChangesAsync();

        using var cache = new MemoryCache(new MemoryCacheOptions());
        await using var dataSource = NpgsqlDataSource.Create(_postgres.GetConnectionString());
        var registry = new TenantRegistry(
            dataSource,
            cache,
            Options.Create(new TenancyOptions { TenantRegistryCacheSeconds = 5 }));

        var result = await registry.ResolveAsync(clinic.Id);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.Inactive);
    }

    private SharedDbContext CreateSharedContext()
    {
        var options = new DbContextOptionsBuilder<SharedDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        return new SharedDbContext(options);
    }
}
