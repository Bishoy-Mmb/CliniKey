using System.Security.Claims;
using CliniKey.API.Middleware;
using CliniKey.Application.Abstractions.Tenancy;
using CliniKey.Domain.Enums;
using CliniKey.Domain.Errors;
using CliniKey.SharedKernel.Primitives;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace CliniKey.Tests.API;

public class TenantResolutionMiddlewareTests
{
    private readonly ITenantRegistry _tenantRegistry;
    private readonly ITenantContextSetter _tenantContextSetter;
    private readonly TenantResolutionMiddleware _middleware;

    public TenantResolutionMiddlewareTests()
    {
        _tenantRegistry = Substitute.For<ITenantRegistry>();
        _tenantContextSetter = Substitute.For<ITenantContextSetter>();
        _middleware = new TenantResolutionMiddleware(_tenantRegistry, _tenantContextSetter);
    }

    [Fact]
    public async Task InvokeAsync_MissingTenantClaim_ReturnsUnauthorized()
    {
        var context = CreateContext(Array.Empty<Claim>());
        var nextCalled = false;

        await _middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_LoginPath_SkipsTenantResolution()
    {
        var context = CreateContext(Array.Empty<Claim>());
        context.Request.Path = "/api/v1/auth/login";
        var nextCalled = false;

        await _middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled.Should().BeTrue();
        await _tenantRegistry.DidNotReceive().ResolveAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokeAsync_ProtectedAuthPath_RequiresHealthyTenant()
    {
        var tenantId = Guid.NewGuid();
        var context = CreateContext([new Claim("tenant_id", tenantId.ToString())]);
        context.Request.Path = "/api/v1/auth/invite";
        _tenantRegistry.ResolveAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<TenantRegistryEntry>(TenantErrors.Inactive));

        await _middleware.InvokeAsync(context, _ => Task.CompletedTask);

        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        await _tenantRegistry.Received(1).ResolveAsync(tenantId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokeAsync_InvalidTenant_ReturnsUnauthorized()
    {
        var tenantId = Guid.NewGuid();
        var context = CreateContext([new Claim("tenant_id", tenantId.ToString())]);
        _tenantRegistry.ResolveAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<TenantRegistryEntry>(TenantErrors.NotFound));

        await _middleware.InvokeAsync(context, _ => Task.CompletedTask);

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task InvokeAsync_InactiveTenant_ReturnsForbidden()
    {
        var tenantId = Guid.NewGuid();
        var context = CreateContext([new Claim("tenant_id", tenantId.ToString())]);
        _tenantRegistry.ResolveAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<TenantRegistryEntry>(TenantErrors.Inactive));

        await _middleware.InvokeAsync(context, _ => Task.CompletedTask);

        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task InvokeAsync_HealthyTenant_ResolvesContextAndCallsNext()
    {
        var tenantId = Guid.NewGuid();
        var entry = new TenantRegistryEntry(
            tenantId,
            "tenant_ab12cd34",
            ClinicStatus.Active,
            TenantSchemaHealthStatus.Healthy,
            CurrentMigration: null);
        var context = CreateContext([new Claim("tenant_id", tenantId.ToString())]);
        var nextCalled = false;
        _tenantRegistry.ResolveAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(entry));

        await _middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled.Should().BeTrue();
        context.Items["TenantId"].Should().Be(tenantId);
        context.Items["TenantSchema"].Should().Be("tenant_ab12cd34");
        _tenantContextSetter.Received(1).Resolve(
            tenantId,
            "tenant_ab12cd34",
            ClinicStatus.Active,
            TenantSchemaHealthStatus.Healthy);
    }

    private static DefaultHttpContext CreateContext(IEnumerable<Claim> claims)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/api/v1/patients";
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        return context;
    }
}
