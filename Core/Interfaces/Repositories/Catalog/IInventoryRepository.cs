using Core.Entities;

namespace Core.Interfaces.Repositories;

public interface IInventoryRepository : IRepository<InventoryMovement>
{
    Task<IEnumerable<InventoryMovement>> GetByProductAsync(Guid productId, CancellationToken ct = default);
    Task UpdateStockAsync(Guid productId, decimal quantityChange, CancellationToken ct = default);
}
