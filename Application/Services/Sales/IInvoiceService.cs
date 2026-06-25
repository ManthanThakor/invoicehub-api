using Application.DTOs;

namespace Application.Services.Sales
{
    public interface IInvoiceService
    {
        Task<ApiResponse<PagedResult<InvoiceListDto>>> GetListAsync(Guid tenantId, InvoiceFilterDto filter, CancellationToken ct = default);
        Task<ApiResponse<InvoiceDto>> GetAsync(Guid tenantId, Guid id, CancellationToken ct = default);
        Task<ApiResponse<InvoiceDto>> CreateAsync(Guid tenantId, Guid userId, CreateInvoiceDto dto, CancellationToken ct = default);
        Task<ApiResponse<InvoiceDto>> UpdateAsync(Guid tenantId, Guid id, UpdateInvoiceDto dto, CancellationToken ct = default);
        Task<ApiResponse<bool>> SendAsync(Guid tenantId, Guid id, CancellationToken ct = default);
        Task<ApiResponse<bool>> CancelAsync(Guid tenantId, Guid id, CancellationToken ct = default);
        Task<ApiResponse<bool>> MarkOverdueAsync(Guid tenantId, CancellationToken ct = default);
        Task<ApiResponse<byte[]>> GeneratePdfAsync(Guid tenantId, Guid id, CancellationToken ct = default);
        Task<ApiResponse<GSTSummaryDto>> GetGSTSummaryAsync(Guid tenantId, int month, int year, CancellationToken ct = default);
    }
}