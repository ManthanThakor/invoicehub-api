using Application.DTOs;
using Application.Services.Catalog;
using InvoiceHub.Application.DTOs.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InvoiceHub.API.Controllers.Catalog;

[ApiController]
[Route("api/inventory")]
[Authorize]
[Tags("Inventory")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventory;

    public InventoryController(IInventoryService inventory) => _inventory = inventory;

    private Guid TenantId => Guid.Parse(User.FindFirstValue("tenantId")!);
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Get inventory movement history (optionally filtered by product).</summary>
    [HttpGet("movements")]
    [Authorize(Policy = "AccountantUp")]
    public async Task<ActionResult<ApiResponse<PagedResult<InventoryMovementDto>>>> GetMovements(
        [FromQuery] Guid? productId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _inventory.GetMovementsAsync(TenantId, productId, page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>Manually adjust stock for a product (increase/decrease/damaged/return).</summary>
    [HttpPost("products/{productId:guid}/adjust")]
    [Authorize(Policy = "ManagerUp")]
    public async Task<ActionResult<ApiResponse<bool>>> AdjustStock(
        Guid productId, [FromBody] StockAdjustmentDto dto, CancellationToken ct)
    {
        var result = await _inventory.AdjustStockAsync(TenantId, productId, UserId, dto, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Get all products that are at or below minimum stock level.</summary>
    [HttpGet("low-stock")]
    [Authorize(Policy = "AllRoles")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ProductListDto>>>> GetLowStock(
        CancellationToken ct)
    {
        var result = await _inventory.GetLowStockProductsAsync(TenantId, ct);
        return Ok(result);
    }

    /// <summary>Get stock valuation summary (total value, low-stock count, out-of-stock count).</summary>
    [HttpGet("valuation")]
    [Authorize(Policy = "AccountantUp")]
    public async Task<ActionResult<ApiResponse<StockValuationDto>>> GetValuation(CancellationToken ct)
    {
        var result = await _inventory.GetStockValuationAsync(TenantId, ct);
        return Ok(result);
    }
}
