using Application.DTOs;
using Application.Services.Finance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InvoiceHub.API.Controllers.Finance;

[ApiController]
[Route("api/expenses")]
[Authorize]
[Tags("Expenses")]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenses;

    public ExpensesController(IExpenseService expenses) => _expenses = expenses;

    private Guid TenantId => Guid.Parse(User.FindFirstValue("tenantId")!);
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>List expenses with filtering by category, date range, and search.</summary>
    [HttpGet]
    [Authorize(Policy = "AccountantUp")]
    public async Task<ActionResult<ApiResponse<PagedResult<ExpenseListDto>>>> GetList(
        [FromQuery] ExpenseFilterDto filter, CancellationToken ct)
    {
        var result = await _expenses.GetListAsync(TenantId, filter, ct);
        return Ok(result);
    }

    /// <summary>Get expense details by ID.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "AccountantUp")]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> Get(Guid id, CancellationToken ct)
    {
        var result = await _expenses.GetAsync(TenantId, id, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Record a new business expense.</summary>
    [HttpPost]
    [Authorize(Policy = "AccountantUp")]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> Create(
        [FromBody] CreateExpenseDto dto, CancellationToken ct)
    {
        var result = await _expenses.CreateAsync(TenantId, UserId, dto, ct);
        return result.Success ? CreatedAtAction(nameof(Get), new { id = result.Data!.Id }, result) : BadRequest(result);
    }

    /// <summary>Update an expense entry.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AccountantUp")]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> Update(
        Guid id, [FromBody] UpdateExpenseDto dto, CancellationToken ct)
    {
        var result = await _expenses.UpdateAsync(TenantId, id, dto, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Delete an expense (also removes the attached receipt file).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "ManagerUp")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id, CancellationToken ct)
    {
        var result = await _expenses.DeleteAsync(TenantId, id, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Upload receipt image/PDF for an expense.</summary>
    [HttpPost("{id:guid}/receipt")]
    [Authorize(Policy = "AccountantUp")]
    public async Task<ActionResult<ApiResponse<string>>> UploadReceipt(
        Guid id, IFormFile file, CancellationToken ct)
    {
        var result = await _expenses.UploadReceiptAsync(TenantId, id, file, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
