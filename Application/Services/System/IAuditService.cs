using Application.DTOs;

namespace Application.Services.System
{
    public interface IAuditService
    {
        Task LogAsync(Guid tenantId, Guid userId, string entityType, Guid entityId,
            string action, object? oldValues = null, object? newValues = null,
            string? ipAddress = null, string? userAgent = null, CancellationToken ct = default);
        Task<ApiResponse<PagedResult<AuditLogDto>>> GetLogsAsync(Guid tenantId, string? entityType,
            Guid? entityId, int page, int pageSize, CancellationToken ct = default);
    }
}