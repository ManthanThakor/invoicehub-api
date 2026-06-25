using Application.DTOs;
using Microsoft.AspNetCore.Http;

namespace Application.Services.Finance
{
    public interface IExpenseService
    {
        Task<ApiResponse<PagedResult<ExpenseListDto>>> GetListAsync(Guid tenantId, ExpenseFilterDto filter, CancellationToken ct = default);
        Task<ApiResponse<ExpenseDto>> GetAsync(Guid tenantId, Guid id, CancellationToken ct = default);
        Task<ApiResponse<ExpenseDto>> CreateAsync(Guid tenantId, Guid userId, CreateExpenseDto dto, CancellationToken ct = default);
        Task<ApiResponse<ExpenseDto>> UpdateAsync(Guid tenantId, Guid id, UpdateExpenseDto dto, CancellationToken ct = default);
        Task<ApiResponse<bool>> DeleteAsync(Guid tenantId, Guid id, CancellationToken ct = default);
        Task<ApiResponse<string>> UploadReceiptAsync(Guid tenantId, Guid id, IFormFile file, CancellationToken ct = default);
    }
}