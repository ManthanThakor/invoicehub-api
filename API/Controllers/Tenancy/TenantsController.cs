using Application.DTOs;
using Application.Services.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InvoiceHub.API.Controllers.Tenancy;

[ApiController]
[Route("api/tenant")]
[Authorize]
[Tags("Tenant")]
public class TenantsController : ControllerBase
{
    private readonly ITenantService _tenants;

    public TenantsController(ITenantService tenants) => _tenants = tenants;

    private Guid TenantId => Guid.Parse(User.FindFirstValue("tenantId")!);

    /// <summary>Get the current tenant's business profile.</summary>
    [HttpGet]
    [Authorize(Policy = "AllRoles")]
    public async Task<ActionResult<ApiResponse<TenantDto>>> Get(CancellationToken ct)
    {
        var result = await _tenants.GetAsync(TenantId, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Update business profile, GST, bank, and settings.</summary>
    [HttpPut]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<TenantDto>>> Update(
        [FromBody] UpdateTenantDto dto, CancellationToken ct)
    {
        var result = await _tenants.UpdateAsync(TenantId, dto, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Upload or replace business logo.</summary>
    [HttpPost("logo")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<string>>> UploadLogo(
        IFormFile file, CancellationToken ct)
    {
        var result = await _tenants.UploadLogoAsync(TenantId, file, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Delete business logo.</summary>
    [HttpDelete("logo")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteLogo(CancellationToken ct)
    {
        var result = await _tenants.DeleteLogoAsync(TenantId, ct);
        return Ok(result);
    }

    /// <summary>Get the dashboard summary (KPIs, recent insights, overdue counts).</summary>
    [HttpGet("dashboard")]
    [Authorize(Policy = "AllRoles")]
    public async Task<ActionResult<ApiResponse<DashboardSummaryDto>>> GetDashboard(CancellationToken ct)
    {
        var result = await _tenants.GetDashboardSummaryAsync(TenantId, ct);
        return Ok(result);
    }
}
