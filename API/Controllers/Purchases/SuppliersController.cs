using Application.DTOs;
using Application.Services.Purchases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InvoiceHub.API.Controllers.Purchases;

[ApiController]
[Route("api/suppliers")]
[Authorize]
[Tags("Suppliers")]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService _suppliers;

    public SuppliersController(ISupplierService suppliers) => _suppliers = suppliers;

    private Guid TenantId => Guid.Parse(User.FindFirstValue("tenantId")!);
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>List suppliers with filtering, search, and pagination.</summary>
    [HttpGet]
    [Authorize(Policy = "AllRoles")]
    public async Task<ActionResult<ApiResponse<PagedResult<SupplierListDto>>>> GetList(
        [FromQuery] SupplierFilterDto filter, CancellationToken ct)
    {
        var result = await _suppliers.GetListAsync(TenantId, filter, ct);
        return Ok(result);
    }

    /// <summary>Search suppliers by name/GSTIN for dropdowns.</summary>
    [HttpGet("search")]
    [Authorize(Policy = "AllRoles")]
    public async Task<ActionResult<ApiResponse<IEnumerable<SelectOptionDto>>>> Search(
        [FromQuery] string term, CancellationToken ct)
    {
        var result = await _suppliers.SearchAsync(TenantId, term, ct);
        return Ok(result);
    }

    /// <summary>Get supplier details by ID.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "AllRoles")]
    public async Task<ActionResult<ApiResponse<SupplierDto>>> Get(Guid id, CancellationToken ct)
    {
        var result = await _suppliers.GetAsync(TenantId, id, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Create a new supplier.</summary>
    [HttpPost]
    [Authorize(Policy = "ManagerUp")]
    public async Task<ActionResult<ApiResponse<SupplierDto>>> Create(
        [FromBody] CreateSupplierDto dto, CancellationToken ct)
    {
        var result = await _suppliers.CreateAsync(TenantId, UserId, dto, ct);
        return result.Success ? CreatedAtAction(nameof(Get), new { id = result.Data!.Id }, result) : BadRequest(result);
    }

    /// <summary>Update supplier details.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "ManagerUp")]
    public async Task<ActionResult<ApiResponse<SupplierDto>>> Update(
        Guid id, [FromBody] UpdateSupplierDto dto, CancellationToken ct)
    {
        var result = await _suppliers.UpdateAsync(TenantId, id, dto, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Delete a supplier (only if no purchase orders exist).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "ManagerUp")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id, CancellationToken ct)
    {
        var result = await _suppliers.DeleteAsync(TenantId, id, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
