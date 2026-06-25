using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Serilog;

namespace InvoiceHub.API.Filters;

/// <summary>
/// Enriches Serilog's LogContext with TenantId + UserId
/// so every log line automatically includes tenant context.
/// </summary>
public class TenantContextFilter : IActionFilter
{
    private readonly ILogger<TenantContextFilter> _log;
    public TenantContextFilter(ILogger<TenantContextFilter> log) => _log = log;

    public void OnActionExecuting(ActionExecutingContext ctx)
    {
        if (ctx.HttpContext.Items.TryGetValue("TenantId", out var tenantId))
            Serilog.Context.LogContext.PushProperty("TenantId", tenantId);

        if (ctx.HttpContext.Items.TryGetValue("TenantName", out var tenantName))
            Serilog.Context.LogContext.PushProperty("TenantName", tenantName);
    }

    public void OnActionExecuted(ActionExecutedContext ctx) { }
}