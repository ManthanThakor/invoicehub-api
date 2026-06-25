using Core.Entities;

namespace Core.Interfaces.Repositories;

public interface ITenantRepository : IRepository<Tenant>
{
    Task<bool> IsGSTINUniqueAsync(string gstin, Guid? excludeId = null, CancellationToken ct = default);
}
