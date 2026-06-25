using Application.DTOs;
using Application.Services.Purchases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InvoiceHub.API.Controllers.Purchases;

[ApiController]
[Route("api/purchase-orders")]
[Authorize]
[Tags("Purchase Orders")]
public class PurchaseOrdersController : ControllerBase
{
    private readonly IPurchaseService _purchases;

    public PurchaseOrdersController(IPurchaseService purchases) => _purchases = purchases;

    private Guid TenantId => Guid.Parse(User.FindFirstValue("tenantId")!);
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>List purchase orders with filtering, search, and pagination.</summary>
    [HttpGet]
    [Authorize(Policy = "AccountantUp")]
    public async Task<ActionResult<ApiResponse<PagedResult<PurchaseOrderListDto>>>> GetList(
        [FromQuery] PurchaseOrderFilterDto filter, CancellationToken ct)
    {
        var result = await _purchases.GetListAsync(TenantId, filter, ct);
        return Ok(result);
    }

    /// <summary>Get purchase order details including line items and payments.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "AccountantUp")]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> Get(Guid id, CancellationToken ct)
    {
        var result = await _purchases.GetAsync(TenantId, id, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Create a new purchase order or save as draft.</summary>
    [HttpPost]
    [Authorize(Policy = "ManagerUp")]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> Create(
        [FromBody] CreatePurchaseOrderDto dto, CancellationToken ct)
    {
        var result = await _purchases.CreateAsync(TenantId, UserId, dto, ct);
        return result.Success ? CreatedAtAction(nameof(Get), new { id = result.Data!.Id }, result) : BadRequest(result);
    }

    /// <summary>Update a purchase order (only Draft or Ordered can be edited).</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "ManagerUp")]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> Update(
        Guid id, [FromBody] UpdatePurchaseOrderDto dto, CancellationToken ct)
    {
        var result = await _purchases.UpdateAsync(TenantId, id, dto, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Mark a purchase order as received — updates inventory stock levels.</summary>
    [HttpPost("{id:guid}/mark-received")]
    [Authorize(Policy = "ManagerUp")]
    public async Task<ActionResult<ApiResponse<bool>>> MarkReceived(Guid id, CancellationToken ct)
    {
        var result = await _purchases.MarkReceivedAsync(TenantId, id, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Cancel a purchase order (only if no payments recorded).</summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = "ManagerUp")]
    public async Task<ActionResult<ApiResponse<bool>>> Cancel(Guid id, CancellationToken ct)
    {
        var result = await _purchases.CancelAsync(TenantId, id, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Download purchase order as PDF.</summary>
    [HttpGet("{id:guid}/pdf")]
    [Authorize(Policy = "AccountantUp")]
    public async Task<IActionResult> DownloadPdf(Guid id, CancellationToken ct)
    {
        var result = await _purchases.GeneratePdfAsync(TenantId, id, ct);
        if (!result.Success) return NotFound(result);
        return File(result.Data!, "application/pdf", $"PO-{id}.pdf");
    }
}
