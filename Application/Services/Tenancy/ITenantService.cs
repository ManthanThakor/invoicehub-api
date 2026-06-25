using Application.DTOs;
using Microsoft.AspNetCore.Http;

namespace Application.Services.Tenancy
{
    public interface ITenantService
    {
    Task<ApiResponse<TenantDto>> GetAsync(Guid tenantId, CancellationToken ct = default);
    Task<ApiResponse<TenantDto>> UpdateAsync(Guid tenantId, UpdateTenantDto dto, CancellationToken ct = default);
    Task<ApiResponse<string>> UploadLogoAsync(Guid tenantId, IFormFile file, CancellationToken ct = default);
    Task<ApiResponse<bool>> DeleteLogoAsync(Guid tenantId, CancellationToken ct = default);
    Task<ApiResponse<DashboardSummaryDto>> GetDashboardSummaryAsync(Guid tenantId, CancellationToken ct = default);
    }
}