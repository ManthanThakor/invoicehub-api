using System.Security.Claims;
using Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceHub.API.Middleware;

/// <summary>
/// Resolves the current tenant from the JWT "tenantId" claim.
/// Skips anonymous endpoints (login, register, verify-email).
/// </summary>
public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    // Paths that do not require a valid tenant
    private static readonly HashSet<string> AnonymousPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/login",
        "/api/auth/register",
        "/api/auth/refresh",
        "/api/auth/forgot-password",
        "/api/auth/reset-password",
        "/api/auth/verify-email",
        "/api/auth/google",
        "/health",
        "/swagger"
    };

    public TenantMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx)
    {
        var path = ctx.Request.Path.Value ?? "";

        // Skip middleware for anonymous / infra paths
        var isAnonymous = AnonymousPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        if (!isAnonymous && ctx.User.Identity?.IsAuthenticated == true)
        {
            var tenantIdClaim = ctx.User.FindFirstValue("tenantId");

            if (!string.IsNullOrEmpty(tenantIdClaim) && Guid.TryParse(tenantIdClaim, out var tenantId))
            {
                // Validate tenant exists and is active
                var tenantRepo = ctx.RequestServices.GetRequiredService<ITenantRepository>();
                var tenant     = await tenantRepo.GetByIdAsync(tenantId);

                if (tenant is null || !tenant.IsActive)
                {
                    ctx.Response.StatusCode  = StatusCodes.Status403Forbidden;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.WriteAsJsonAsync(new
                    {
                        Success = false,
                        Message = "Your business account is inactive or has been suspended."
                    });
                    return;
                }

                // Make tenant info available to controllers via HttpContext.Items
                ctx.Items["TenantId"]   = tenantId;
                ctx.Items["TenantName"] = tenant.BusinessName;
            }
        }

        await _next(ctx);
    }
}