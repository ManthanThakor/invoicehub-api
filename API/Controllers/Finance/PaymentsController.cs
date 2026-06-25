using Application.DTOs;
using Application.Services.Finance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InvoiceHub.API.Controllers.Finance;

[ApiController]
[Route("api/payments")]
[Authorize]
[Tags("Payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _payments;

    public PaymentsController(IPaymentService payments) => _payments = payments;

    private Guid TenantId => Guid.Parse(User.FindFirstValue("tenantId")!);
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>List payments with filtering by invoice, customer, date, method, and status.</summary>
    [HttpGet]
    [Authorize(Policy = "AccountantUp")]
    public async Task<ActionResult<ApiResponse<PagedResult<PaymentListDto>>>> GetList(
        [FromQuery] PaymentFilterDto filter, CancellationToken ct)
    {
        var result = await _payments.GetListAsync(TenantId, filter, ct);
        return Ok(result);
    }

    /// <summary>Get payment details by ID.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "AccountantUp")]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> Get(Guid id, CancellationToken ct)
    {
        var result = await _payments.GetAsync(TenantId, id, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Record a payment against an invoice or purchase order.
    /// Automatically updates invoice/PO status to PartiallyPaid or Paid.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "AccountantUp")]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> Record(
        [FromBody] RecordPaymentDto dto, CancellationToken ct)
    {
        var result = await _payments.RecordAsync(TenantId, UserId, dto, ct);
        return result.Success ? CreatedAtAction(nameof(Get), new { id = result.Data!.Id }, result) : BadRequest(result);
    }

    /// <summary>Delete a payment and reverse the invoice/PO balance.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "ManagerUp")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id, CancellationToken ct)
    {
        var result = await _payments.DeleteAsync(TenantId, id, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
