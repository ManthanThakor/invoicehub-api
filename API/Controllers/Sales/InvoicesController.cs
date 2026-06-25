using Application.DTOs;
using Application.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InvoiceHub.API.Controllers.Sales;

[ApiController]
[Route("api/invoices")]
[Authorize]
[Tags("Invoices")]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoices;

    public InvoicesController(IInvoiceService invoices) => _invoices = invoices;

    private Guid TenantId => Guid.Parse(User.FindFirstValue("tenantId")!);
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>List invoices with filtering, search, and pagination.</summary>
    [HttpGet]
    [Authorize(Policy = "AllRoles")]
    public async Task<ActionResult<ApiResponse<PagedResult<InvoiceListDto>>>> GetList(
        [FromQuery] InvoiceFilterDto filter, CancellationToken ct)
    {
        var result = await _invoices.GetListAsync(TenantId, filter, ct);
        return Ok(result);
    }

    /// <summary>Get invoice details including line items, GST breakdown, and payments.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "AllRoles")]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> Get(Guid id, CancellationToken ct)
    {
        var result = await _invoices.GetAsync(TenantId, id, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Create a new invoice or save as draft. Automatically deducts inventory.</summary>
    [HttpPost]
    [Authorize(Policy = "SalesUp")]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> Create(
        [FromBody] CreateInvoiceDto dto, CancellationToken ct)
    {
        var result = await _invoices.CreateAsync(TenantId, UserId, dto, ct);
        return result.Success ? CreatedAtAction(nameof(Get), new { id = result.Data!.Id }, result) : BadRequest(result);
    }

    /// <summary>Update an invoice (only Draft or Sent status can be edited).</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "SalesUp")]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> Update(
        Guid id, [FromBody] UpdateInvoiceDto dto, CancellationToken ct)
    {
        var result = await _invoices.UpdateAsync(TenantId, id, dto, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Send invoice PDF to the customer via email.</summary>
    [HttpPost("{id:guid}/send")]
    [Authorize(Policy = "SalesUp")]
    public async Task<ActionResult<ApiResponse<bool>>> Send(Guid id, CancellationToken ct)
    {
        var result = await _invoices.SendAsync(TenantId, id, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Cancel an invoice (only if no payments recorded).</summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = "ManagerUp")]
    public async Task<ActionResult<ApiResponse<bool>>> Cancel(Guid id, CancellationToken ct)
    {
        var result = await _invoices.CancelAsync(TenantId, id, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Mark overdue invoices — typically called by a background job.</summary>
    [HttpPost("mark-overdue")]
    [Authorize(Policy = "ManagerUp")]
    public async Task<ActionResult<ApiResponse<bool>>> MarkOverdue(CancellationToken ct)
    {
        var result = await _invoices.MarkOverdueAsync(TenantId, ct);
        return Ok(result);
    }

    /// <summary>Download invoice as PDF.</summary>
    [HttpGet("{id:guid}/pdf")]
    [Authorize(Policy = "AllRoles")]
    public async Task<IActionResult> DownloadPdf(Guid id, CancellationToken ct)
    {
        var result = await _invoices.GeneratePdfAsync(TenantId, id, ct);
        if (!result.Success) return NotFound(result);
        return File(result.Data!, "application/pdf", $"Invoice-{id}.pdf");
    }

    /// <summary>Get GST summary for a specific month/year (GSTR-1 data).</summary>
    [HttpGet("gst-summary")]
    [Authorize(Policy = "AccountantUp")]
    public async Task<ActionResult<ApiResponse<GSTSummaryDto>>> GetGSTSummary(
        [FromQuery] int month, [FromQuery] int year, CancellationToken ct)
    {
        var result = await _invoices.GetGSTSummaryAsync(TenantId, month, year, ct);
        return Ok(result);
    }
}
