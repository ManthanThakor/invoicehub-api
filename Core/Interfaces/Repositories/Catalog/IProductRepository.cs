using Core.Entities;

namespace Core.Interfaces.Repositories;

public interface IProductRepository : IRepository<Product>
{
    Task<bool> IsSKUUniqueAsync(Guid tenantId, string sku, Guid? excludeId = null, CancellationToken ct = default);
    Task<IEnumerable<Product>> GetLowStockAsync(Guid tenantId, CancellationToken ct = default);
    Task<IEnumerable<Product>> SearchAsync(Guid tenantId, string term, CancellationToken ct = default);
    Task<IEnumerable<(Product Product, decimal Qty)>> GetTopSellingAsync(Guid tenantId, int count, DateTime from, DateTime to, CancellationToken ct = default);
}
