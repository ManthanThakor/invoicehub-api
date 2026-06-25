using Application.DTOs;

namespace Application.Services.Catalog;

public interface IProductCategoryService
{
    Task<ApiResponse<IEnumerable<ProductCategoryDto>>> GetAllAsync(Guid tenantId, CancellationToken ct = default);
    Task<ApiResponse<ProductCategoryDto>> GetAsync(Guid tenantId, Guid id, CancellationToken ct = default);
    Task<ApiResponse<ProductCategoryDto>> CreateAsync(Guid tenantId, Guid userId, CreateProductCategoryDto dto, CancellationToken ct = default);
    Task<ApiResponse<ProductCategoryDto>> UpdateAsync(Guid tenantId, Guid id, UpdateProductCategoryDto dto, CancellationToken ct = default);
    Task<ApiResponse<bool>> DeleteAsync(Guid tenantId, Guid id, CancellationToken ct = default);
    Task<ApiResponse<IEnumerable<SelectOptionDto>>> SearchAsync(Guid tenantId, string term, CancellationToken ct = default);
}
