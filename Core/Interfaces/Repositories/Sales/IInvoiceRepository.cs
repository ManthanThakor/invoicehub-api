using Core.Entities;

namespace Core.Interfaces.Repositories;

public interface IInvoiceRepository : IRepository<Invoice>
{
    Task<string> GenerateInvoiceNumberAsync(Guid tenantId, CancellationToken ct = default);
    Task<IEnumerable<Invoice>> GetOverdueAsync(Guid tenantId, CancellationToken ct = default);
    Task<decimal> GetTotalRevenueAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<IEnumerable<Invoice>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<Invoice?> GetWithItemsAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<IEnumerable<Invoice>> GetByDateRangeAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default);
}
