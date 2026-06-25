using Core.Entities;

namespace Core.Interfaces.Repositories;

public interface IAuditLogRepository : IRepository<AuditLog>
{
    Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityType, Guid entityId, CancellationToken ct = default);
}
