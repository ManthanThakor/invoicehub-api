using Application.DTOs;
using InvoiceHub.Application.DTOs.Products;

namespace Application.Services.Catalog
{
    public interface IInventoryService
    {
        Task<ApiResponse<PagedResult<InventoryMovementDto>>> GetMovementsAsync(Guid tenantId, Guid? productId, int page, int pageSize, CancellationToken ct = default);
        Task<ApiResponse<bool>> AdjustStockAsync(Guid tenantId, Guid productId, Guid userId, StockAdjustmentDto dto, CancellationToken ct = default);
        Task<ApiResponse<IEnumerable<ProductListDto>>> GetLowStockProductsAsync(Guid tenantId, CancellationToken ct = default);
        Task<ApiResponse<StockValuationDto>> GetStockValuationAsync(Guid tenantId, CancellationToken ct = default);
    }
}