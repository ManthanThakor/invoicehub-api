using Application.DTOs;
using Application.Services.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InvoiceHub.API.Controllers.Catalog;

[ApiController]
[Route("api/product-categories")]
[Authorize]
[Tags("Product Categories")]
public class ProductCategoriesController : ControllerBase
{
    private readonly IProductCategoryService _categories;

    public ProductCategoriesController(IProductCategoryService categories) => _categories = categories;

    private Guid TenantId => Guid.Parse(User.FindFirstValue("tenantId")!);
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [Authorize(Policy = "AllRoles")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ProductCategoryDto>>>> GetAll(CancellationToken ct)
    {
        var result = await _categories.GetAllAsync(TenantId, ct);
        return Ok(result);
    }

    [HttpGet("search")]
    [Authorize(Policy = "AllRoles")]
    public async Task<ActionResult<ApiResponse<IEnumerable<SelectOptionDto>>>> Search(
        [FromQuery] string term, CancellationToken ct)
    {
        var result = await _categories.SearchAsync(TenantId, term, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "AllRoles")]
    public async Task<ActionResult<ApiResponse<ProductCategoryDto>>> Get(Guid id, CancellationToken ct)
    {
        var result = await _categories.GetAsync(TenantId, id, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    [Authorize(Policy = "ManagerUp")]
    public async Task<ActionResult<ApiResponse<ProductCategoryDto>>> Create(
        [FromBody] CreateProductCategoryDto dto, CancellationToken ct)
    {
        var result = await _categories.CreateAsync(TenantId, UserId, dto, ct);
        return result.Success
            ? CreatedAtAction(nameof(Get), new { id = result.Data!.Id }, result)
            : BadRequest(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "ManagerUp")]
    public async Task<ActionResult<ApiResponse<ProductCategoryDto>>> Update(
        Guid id, [FromBody] UpdateProductCategoryDto dto, CancellationToken ct)
    {
        var result = await _categories.UpdateAsync(TenantId, id, dto, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "ManagerUp")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id, CancellationToken ct)
    {
        var result = await _categories.DeleteAsync(TenantId, id, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
