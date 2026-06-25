using Core.Entities;

namespace Core.Interfaces.Repositories;

public interface ISupplierRepository : IRepository<Supplier>
{
    Task<IEnumerable<Supplier>> SearchAsync(Guid tenantId, string term, CancellationToken ct = default);
}
