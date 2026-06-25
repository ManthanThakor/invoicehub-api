using Application.DTOs;
using Application.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InvoiceHub.API.Controllers.Sales;

[ApiController]
[Route("api/customers")]
[Authorize]
[Tags("Customers")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customers;

    public CustomersController(ICustomerService customers) => _customers = customers;

    private Guid TenantId => Guid.Parse(User.FindFirstValue("tenantId")!);
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>List customers with filtering, search, and pagination.</summary>
    [HttpGet]
    [Authorize(Policy = "AllRoles")]
    public async Task<ActionResult<ApiResponse<PagedResult<CustomerListDto>>>> GetList(
        [FromQuery] CustomerFilterDto filter, CancellationToken ct)
    {
        var result = await _customers.GetListAsync(TenantId, filter, ct);
        return Ok(result);
    }

    /// <summary>Search customers by name/email/phone for dropdowns.</summary>
    [HttpGet("search")]
    [Authorize(Policy = "AllRoles")]
    public async Task<ActionResult<ApiResponse<IEnumerable<SelectOptionDto>>>> Search(
        [FromQuery] string term, CancellationToken ct)
    {
        var result = await _customers.SearchAsync(TenantId, term, ct);
        return Ok(result);
    }

    /// <summary>Get customer details by ID.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "AllRoles")]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> Get(Guid id, CancellationToken ct)
    {
        var result = await _customers.GetAsync(TenantId, id, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Get invoice statistics for a customer.</summary>
    [HttpGet("{id:guid}/statistics")]
    [Authorize(Policy = "AllRoles")]
    public async Task<ActionResult<ApiResponse<CustomerStatisticsDto>>> GetStatistics(
        Guid id, CancellationToken ct)
    {
        var result = await _customers.GetStatisticsAsync(TenantId, id, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Create a new customer.</summary>
    [HttpPost]
    [Authorize(Policy = "SalesUp")]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> Create(
        [FromBody] CreateCustomerDto dto, CancellationToken ct)
    {
        var result = await _customers.CreateAsync(TenantId, UserId, dto, ct);
        return result.Success ? CreatedAtAction(nameof(Get), new { id = result.Data!.Id }, result) : BadRequest(result);
    }

    /// <summary>Update customer details.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "SalesUp")]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> Update(
        Guid id, [FromBody] UpdateCustomerDto dto, CancellationToken ct)
    {
        var result = await _customers.UpdateAsync(TenantId, id, dto, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Delete a customer (only if no invoices exist).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "ManagerUp")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id, CancellationToken ct)
    {
        var result = await _customers.DeleteAsync(TenantId, id, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
