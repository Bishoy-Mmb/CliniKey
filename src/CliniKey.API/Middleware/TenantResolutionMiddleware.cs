using Microsoft.AspNetCore.Http;

namespace CliniKey.API.Middleware;

public sealed class TenantResolutionMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var tenantId = context.User.FindFirst("tenant_id")?.Value;

#if DEBUG
        if (string.IsNullOrEmpty(tenantId) && context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdValues))
        {
            tenantId = tenantIdValues.FirstOrDefault();
        }
#endif

        if (!string.IsNullOrEmpty(tenantId))
        {
            context.Items["TenantId"] = tenantId;
        }

        await next(context);
    }
}
