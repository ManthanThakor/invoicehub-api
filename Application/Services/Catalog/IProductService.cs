using Application.DTOs;
using Microsoft.AspNetCore.Http;

namespace Application.Services.Catalog
{
    public interface IProductService
    {
        Task<ApiResponse<PagedResult<ProductListDto>>> GetListAsync(Guid tenantId, ProductFilterDto filter, CancellationToken ct = default);
        Task<ApiResponse<ProductDto>> GetAsync(Guid tenantId, Guid id, CancellationToken ct = default);
        Task<ApiResponse<ProductDto>> CreateAsync(Guid tenantId, Guid userId, CreateProductDto dto, CancellationToken ct = default);
        Task<ApiResponse<ProductDto>> UpdateAsync(Guid tenantId, Guid id, UpdateProductDto dto, CancellationToken ct = default);
        Task<ApiResponse<bool>> DeleteAsync(Guid tenantId, Guid id, CancellationToken ct = default);
        Task<ApiResponse<string>> UploadImageAsync(Guid tenantId, Guid id, IFormFile file, CancellationToken ct = default);
        Task<ApiResponse<IEnumerable<ProductListDto>>> GetLowStockAsync(Guid tenantId, CancellationToken ct = default);
        Task<ApiResponse<IEnumerable<SelectOptionDto>>> SearchAsync(Guid tenantId, string term, CancellationToken ct = default);
    }
}