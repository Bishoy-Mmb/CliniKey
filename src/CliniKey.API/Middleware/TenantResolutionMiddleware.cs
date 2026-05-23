using System.Security.Claims;
using CliniKey.Application.Abstractions.Tenancy;
using CliniKey.SharedKernel.Primitives;
using Microsoft.AspNetCore.Mvc;

namespace CliniKey.API.Middleware;

public sealed class TenantResolutionMiddleware : IMiddleware
{
    private readonly ITenantRegistry _tenantRegistry;
    private readonly ITenantContextSetter _tenantContextSetter;

    public TenantResolutionMiddleware(
        ITenantRegistry tenantRegistry,
        ITenantContextSetter tenantContextSetter)
    {
        _tenantRegistry = tenantRegistry;
        _tenantContextSetter = tenantContextSetter;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (ShouldSkipTenantResolution(context))
        {
            await next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            await WriteProblemAsync(
                context,
                StatusCodes.Status401Unauthorized,
                "Tenant.InvalidClaim",
                "A valid tenant_id claim is required.");
            return;
        }

        var tenantClaim = context.User.FindFirstValue("tenant_id");
        if (!Guid.TryParse(tenantClaim, out var tenantId))
        {
            await WriteProblemAsync(
                context,
                StatusCodes.Status401Unauthorized,
                "Tenant.InvalidClaim",
                "A valid tenant_id claim is required.");
            return;
        }

        var result = await _tenantRegistry.ResolveAsync(tenantId, context.RequestAborted);
        if (result.IsFailure)
        {
            await WriteProblemAsync(
                context,
                GetStatusCode(result.Error),
                result.Error.Code,
                result.Error.Description);
            return;
        }

        _tenantContextSetter.Resolve(
            result.Value.TenantId,
            result.Value.SchemaName,
            result.Value.ClinicStatus,
            result.Value.SchemaHealthStatus);

        context.Items["TenantId"] = result.Value.TenantId;
        context.Items["TenantSchema"] = result.Value.SchemaName;

        await next(context);
    }

    private static bool ShouldSkipTenantResolution(HttpContext context)
    {
        var path = context.Request.Path;

        return path.Equals("/api/v1/auth/login", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/api/v1/auth/refresh", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/api/v1/tenants", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/openapi", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/scalar", StringComparison.OrdinalIgnoreCase);
    }

    private static int GetStatusCode(Error error)
    {
        return error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        int statusCode,
        string title,
        string detail)
    {
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail
        });
    }
}
