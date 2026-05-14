using Microsoft.AspNetCore.Http;

namespace CliniKey.API.Middleware;

public sealed class TenantResolutionMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdValues))
        {
            var tenantId = tenantIdValues.FirstOrDefault();
            context.Items["TenantId"] = tenantId;
        }

        await next(context);
    }
}
