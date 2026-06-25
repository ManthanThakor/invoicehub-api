using Core.Entities;
using Core.Interfaces.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(AppDbContext db) : base(db) { }

    public async Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityType, Guid entityId, CancellationToken ct = default)
    {
        return await _set
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .Include(a => a.User)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
    }
}
