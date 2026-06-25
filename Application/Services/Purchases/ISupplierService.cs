using Application.DTOs;

namespace Application.Services.Purchases
{
    public interface ISupplierService
    {
        Task<ApiResponse<PagedResult<SupplierListDto>>> GetListAsync(Guid tenantId, SupplierFilterDto filter, CancellationToken ct = default);
        Task<ApiResponse<SupplierDto>> GetAsync(Guid tenantId, Guid id, CancellationToken ct = default);
        Task<ApiResponse<SupplierDto>> CreateAsync(Guid tenantId, Guid userId, CreateSupplierDto dto, CancellationToken ct = default);
        Task<ApiResponse<SupplierDto>> UpdateAsync(Guid tenantId, Guid id, UpdateSupplierDto dto, CancellationToken ct = default);
        Task<ApiResponse<bool>> DeleteAsync(Guid tenantId, Guid id, CancellationToken ct = default);
        Task<ApiResponse<IEnumerable<SelectOptionDto>>> SearchAsync(Guid tenantId, string term, CancellationToken ct = default);
    }
}