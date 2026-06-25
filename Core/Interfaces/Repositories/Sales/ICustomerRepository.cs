using Core.Entities;

namespace Core.Interfaces.Repositories;

public interface ICustomerRepository : IRepository<Customer>
{
    Task<bool> IsGSTINUniqueAsync(Guid tenantId, string gstin, Guid? excludeId = null, CancellationToken ct = default);
    Task<IEnumerable<Customer>> SearchAsync(Guid tenantId, string term, CancellationToken ct = default);
    Task<IEnumerable<(Customer Customer, decimal Revenue)>> GetTopCustomersAsync(Guid tenantId, int count, DateTime from, DateTime to, CancellationToken ct = default);
}
