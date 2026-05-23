using CliniKey.Domain.Entities;
using CliniKey.Domain.Enums;
using CliniKey.Domain.Errors;
using CliniKey.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Time.Testing;
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
        var clinic = Clinic.Create(
            Guid.Parse("bbbbbbbb-1111-2222-3333-cccccccccccc"),
            "Inactive Clinic",
            "01122222222",
            "15 Tahrir St",
            "tenant_inactive_access",
            _clock).Value;
        clinic.MarkProvisioned("202605230001_InitialTenantOperationalSchema");
        clinic.Deactivate(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));
        sharedContext.Clinics.Add(clinic);
        await sharedContext.SaveChangesAsync();

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var registry = new TenantRegistry(
            _postgres.GetConnectionString(),
            cache,
            new TenancyOptions { TenantRegistryCacheSeconds = 5 });

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
