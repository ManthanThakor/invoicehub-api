using Core.Entities;
using Core.Enums;

namespace Core.Interfaces.Repositories;

public interface IAIInsightRepository : IRepository<AIInsight>
{
    Task<IEnumerable<AIInsight>> GetActiveAsync(Guid tenantId, CancellationToken ct = default);
    Task ClearInsightTypeAsync(Guid tenantId, InsightType type, CancellationToken ct = default);
}
