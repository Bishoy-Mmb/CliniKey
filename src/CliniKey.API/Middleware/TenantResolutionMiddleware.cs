using Microsoft.AspNetCore.Http;

namespace CliniKey.API.Middleware;

public sealed class TenantResolutionMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // Simple tenant resolution for MVP. Reads from header.
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdValues))
        {
            var tenantId = tenantIdValues.FirstOrDefault();
            // In a real app, you would set this tenantId in a scoped service.
            context.Items["TenantId"] = tenantId;
        }

        await next(context);
    }
}
