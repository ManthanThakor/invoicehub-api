using Application.DTOs;
using Application.Services.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InvoiceHub.API.Controllers.Catalog;

[ApiController]
[Route("api/products")]
[Authorize]
[Tags("Products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _products;

    public ProductsController(IProductService products) => _products = products;

    private Guid TenantId => Guid.Parse(User.FindFirstValue("tenantId")!);
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>List products with filtering, search, and pagination.</summary>
    [HttpGet]
    [Authorize(Policy = "AllRoles")]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductListDto>>>> GetList(
        [FromQuery] ProductFilterDto filter, CancellationToken ct)
    {
        var result = await _products.GetListAsync(TenantId, filter, ct);
        return Ok(result);
    }

    /// <summary>Search products by name/SKU/HSN for invoice line item dropdowns.</summary>
    [HttpGet("search")]
    [Authorize(Policy = "AllRoles")]
    public async Task<ActionResult<ApiResponse<IEnumerable<SelectOptionDto>>>> Search(
        [FromQuery] string term, CancellationToken ct)
    {
        var result = await _products.SearchAsync(TenantId, term, ct);
        return Ok(result);
    }

    /// <summary>Get all products below minimum stock level.</summary>
    [HttpGet("low-stock")]
    [Authorize(Policy = "AllRoles")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ProductListDto>>>> GetLowStock(
        CancellationToken ct)
    {
        var result = await _products.GetLowStockAsync(TenantId, ct);
        return Ok(result);
    }

    /// <summary>Get product details by ID.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "AllRoles")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> Get(Guid id, CancellationToken ct)
    {
        var result = await _products.GetAsync(TenantId, id, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Create a new product (with optional opening stock).</summary>
    [HttpPost]
    [Authorize(Policy = "ManagerUp")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> Create(
        [FromBody] CreateProductDto dto, CancellationToken ct)
    {
        var result = await _products.CreateAsync(TenantId, UserId, dto, ct);
        return result.Success ? CreatedAtAction(nameof(Get), new { id = result.Data!.Id }, result) : BadRequest(result);
    }

    /// <summary>Update product details (pricing, GST, stock thresholds).</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "ManagerUp")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> Update(
        Guid id, [FromBody] UpdateProductDto dto, CancellationToken ct)
    {
        var result = await _products.UpdateAsync(TenantId, id, dto, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Delete a product (only if not used in any invoice).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "ManagerUp")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id, CancellationToken ct)
    {
        var result = await _products.DeleteAsync(TenantId, id, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Upload or replace product image.</summary>
    [HttpPost("{id:guid}/image")]
    [Authorize(Policy = "ManagerUp")]
    public async Task<ActionResult<ApiResponse<string>>> UploadImage(
        Guid id, IFormFile file, CancellationToken ct)
    {
        var result = await _products.UploadImageAsync(TenantId, id, file, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
