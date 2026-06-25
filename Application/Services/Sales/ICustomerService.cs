using Application.DTOs;

namespace Application.Services.Sales
{
    public interface ICustomerService
    {
        Task<ApiResponse<PagedResult<CustomerListDto>>> GetListAsync(Guid tenantId, CustomerFilterDto filter, CancellationToken ct = default);
        Task<ApiResponse<CustomerDto>> GetAsync(Guid tenantId, Guid id, CancellationToken ct = default);
        Task<ApiResponse<CustomerDto>> CreateAsync(Guid tenantId, Guid userId, CreateCustomerDto dto, CancellationToken ct = default);
        Task<ApiResponse<CustomerDto>> UpdateAsync(Guid tenantId, Guid id, UpdateCustomerDto dto, CancellationToken ct = default);
        Task<ApiResponse<bool>> DeleteAsync(Guid tenantId, Guid id, CancellationToken ct = default);
        Task<ApiResponse<CustomerStatisticsDto>> GetStatisticsAsync(Guid tenantId, Guid id, CancellationToken ct = default);
        Task<ApiResponse<IEnumerable<SelectOptionDto>>> SearchAsync(Guid tenantId, string term, CancellationToken ct = default);
    }
}