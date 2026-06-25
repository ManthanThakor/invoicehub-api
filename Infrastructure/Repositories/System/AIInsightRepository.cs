using Core.Entities;
using Core.Enums;
using Core.Interfaces.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class AIInsightRepository : Repository<AIInsight>, IAIInsightRepository
{
    public AIInsightRepository(AppDbContext db) : base(db) { }

    public async Task<IEnumerable<AIInsight>> GetActiveAsync(Guid tenantId, CancellationToken ct = default)
    {
        return await _set
            .Where(a => a.TenantId == tenantId
                     && (a.ExpiresAt == null || a.ExpiresAt > DateTime.UtcNow))
            .OrderByDescending(a => a.GeneratedAt)
            .ToListAsync(ct);
    }

    public async Task ClearInsightTypeAsync(Guid tenantId, InsightType type, CancellationToken ct = default)
    {
        var old = await _set
            .Where(a => a.TenantId == tenantId && a.InsightType == type)
            .ToListAsync(ct);
        _set.RemoveRange(old);
    }
}
