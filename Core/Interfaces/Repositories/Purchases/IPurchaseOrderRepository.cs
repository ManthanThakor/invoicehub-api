using Core.Entities;

namespace Core.Interfaces.Repositories;

public interface IPurchaseOrderRepository : IRepository<PurchaseOrder>
{
    Task<string> GeneratePONumberAsync(Guid tenantId, CancellationToken ct = default);
    Task<PurchaseOrder?> GetWithItemsAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<decimal> GetTotalPurchasesAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default);
}
