using Application.DTOs;

namespace Application.Services.Purchases
{
    public interface IPurchaseService
    {
        Task<ApiResponse<PagedResult<PurchaseOrderListDto>>> GetListAsync(Guid tenantId, PurchaseOrderFilterDto filter, CancellationToken ct = default);
        Task<ApiResponse<PurchaseOrderDto>> GetAsync(Guid tenantId, Guid id, CancellationToken ct = default);
        Task<ApiResponse<PurchaseOrderDto>> CreateAsync(Guid tenantId, Guid userId, CreatePurchaseOrderDto dto, CancellationToken ct = default);
        Task<ApiResponse<PurchaseOrderDto>> UpdateAsync(Guid tenantId, Guid id, UpdatePurchaseOrderDto dto, CancellationToken ct = default);
        Task<ApiResponse<bool>> MarkReceivedAsync(Guid tenantId, Guid id, CancellationToken ct = default);
        Task<ApiResponse<bool>> CancelAsync(Guid tenantId, Guid id, CancellationToken ct = default);
        Task<ApiResponse<byte[]>> GeneratePdfAsync(Guid tenantId, Guid id, CancellationToken ct = default);
    }

}