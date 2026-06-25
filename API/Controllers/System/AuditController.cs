using Application.DTOs;
using Application.Services.System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InvoiceHub.API.Controllers.System;

[ApiController]
[Route("api/audit")]
[Authorize]
[Tags("Audit Logs")]
public class AuditController : ControllerBase
{
    private readonly IAuditService _audit;

    public AuditController(IAuditService audit) => _audit = audit;

    private Guid TenantId => Guid.Parse(User.FindFirstValue("tenantId")!);

    /// <summary>
    /// Get paginated audit logs for the tenant.
    /// Optionally filter by entity type (e.g. "Invoice", "Customer") and entity ID.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<PagedResult<AuditLogDto>>>> GetLogs(
        [FromQuery] string? entityType,
        [FromQuery] Guid? entityId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _audit.GetLogsAsync(TenantId, entityType, entityId, page, pageSize, ct);
        return Ok(result);
    }
}
