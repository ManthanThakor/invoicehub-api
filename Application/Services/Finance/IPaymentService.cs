using Application.DTOs;

namespace Application.Services.Finance
{
    public interface IPaymentService
    {
        Task<ApiResponse<PagedResult<PaymentListDto>>> GetListAsync(Guid tenantId, PaymentFilterDto filter, CancellationToken ct = default);
        Task<ApiResponse<PaymentDto>> GetAsync(Guid tenantId, Guid id, CancellationToken ct = default);
        Task<ApiResponse<PaymentDto>> RecordAsync(Guid tenantId, Guid userId, RecordPaymentDto dto, CancellationToken ct = default);
        Task<ApiResponse<bool>> DeleteAsync(Guid tenantId, Guid id, CancellationToken ct = default);
    }
}