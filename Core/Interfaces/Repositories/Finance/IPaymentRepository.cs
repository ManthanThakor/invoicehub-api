using Core.Entities;

namespace Core.Interfaces.Repositories;

public interface IPaymentRepository : IRepository<Payment>
{
    Task<IEnumerable<Payment>> GetByInvoiceAsync(Guid invoiceId, CancellationToken ct = default);
    Task<decimal> GetCollectedAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default);
}
