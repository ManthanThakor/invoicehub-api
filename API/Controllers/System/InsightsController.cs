using Application.DTOs;
using Application.Services.System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InvoiceHub.API.Controllers.System;

[ApiController]
[Route("api/insights")]
[Authorize]
[Tags("AI Insights")]
public class InsightsController : ControllerBase
{
    private readonly IInsightService _insights;

    public InsightsController(IInsightService insights) => _insights = insights;

    private Guid TenantId => Guid.Parse(User.FindFirstValue("tenantId")!);

    /// <summary>Get all unread AI insights for the current tenant.</summary>
    [HttpGet]
    [Authorize(Policy = "AllRoles")]
    public async Task<ActionResult<ApiResponse<IEnumerable<AIInsightDto>>>> GetInsights(
        CancellationToken ct)
    {
        var result = await _insights.GetInsightsAsync(TenantId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Trigger insight generation for the tenant.
    /// Usually called by a background job; Admin can also trigger manually.
    /// </summary>
    [HttpPost("generate")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Generate(CancellationToken ct)
    {
        await _insights.GenerateInsightsAsync(TenantId, ct);
        return Ok(new { Success = true, Message = "Insights generated successfully." });
    }

    /// <summary>Mark a specific insight as read.</summary>
    [HttpPatch("{id:guid}/read")]
    [Authorize(Policy = "AllRoles")]
    public async Task<ActionResult<ApiResponse<bool>>> MarkRead(Guid id, CancellationToken ct)
    {
        var result = await _insights.MarkReadAsync(TenantId, id, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Ask a free-form financial question — answered by AI using live business data.
    /// </summary>
    [HttpPost("ask")]
    [Authorize(Policy = "ManagerUp")]
    public async Task<ActionResult<ApiResponse<string>>> Ask(
        [FromBody] string question, CancellationToken ct)
    {
        var result = await _insights.AskFinancialQuestionAsync(TenantId, question, ct);
        return result.Success ? Ok(result) : StatusCode(503, result);
    }
}
