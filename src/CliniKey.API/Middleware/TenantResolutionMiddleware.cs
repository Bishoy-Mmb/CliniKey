using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CliniKey.API.Middleware;

public sealed class TenantResolutionMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (!context.Request.Headers.TryGetValue("X-Tenant-Id", out var values)
            || string.IsNullOrWhiteSpace(values.FirstOrDefault()))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Tenant.Missing",
                Detail = "X-Tenant-Id header is required."
            });
            return;
        }

        context.Items["TenantId"] = values.First();
        await next(context);
    }
}
