using Application.DTOs;
using Core.Entities;
using Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Application.Services.System
{

    public class AuditService : IAuditService
    {
        private readonly IAuditLogRepository _auditLogs;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<AuditService> _log;

        public AuditService(
            IAuditLogRepository auditLogs,
            IUnitOfWork uow,
            ILogger<AuditService> log)
        {
            _auditLogs = auditLogs; _uow = uow; _log = log;
        }

        public async Task LogAsync(
            Guid tenantId, Guid userId, string entityType, Guid entityId,
            string action, object? oldValues = null, object? newValues = null,
            string? ipAddress = null, string? userAgent = null,
            CancellationToken ct = default)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    TenantId = tenantId,
                    UserId = userId,
                    EntityType = entityType,
                    EntityId = entityId,
                    Action = action,
                    OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                    NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
                    IpAddress = ipAddress,
                    UserAgent = userAgent
                };

                await _auditLogs.AddAsync(auditLog, ct);
                await _uow.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                // Audit failures must never break the main operation
                _log.LogError(ex, "Audit log failed for {Action} on {EntityType}:{EntityId}",
                    action, entityType, entityId);
            }
        }

        public async Task<ApiResponse<PagedResult<AuditLogDto>>> GetLogsAsync(
            Guid tenantId, string? entityType, Guid? entityId,
            int page, int pageSize, CancellationToken ct = default)
        {
            IEnumerable<AuditLog> logs;

            if (!string.IsNullOrEmpty(entityType) && entityId.HasValue)
                logs = await _auditLogs.GetByEntityAsync(entityType, entityId.Value, ct);
            else
                logs = await _auditLogs.GetAllAsync(tenantId, ct);

            var filtered = logs
                .Where(l => entityType == null || l.EntityType == entityType)
                .OrderByDescending(l => l.CreatedAt)
                .ToList();

            var total = filtered.Count;
            var paged = filtered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new AuditLogDto(
                    l.Id, l.EntityType, l.EntityId, l.Action,
                    l.OldValues, l.NewValues, l.ChangedProperties,
                    l.IpAddress, l.UserId,
                    l.User != null ? $"{l.User.FirstName} {l.User.LastName}" : "System",
                    l.CreatedAt));

            return ApiResponse<PagedResult<AuditLogDto>>.Ok(
                new PagedResult<AuditLogDto>(paged, total, page, pageSize));
        }
    }

}
