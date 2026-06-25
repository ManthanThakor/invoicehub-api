using Core.Entities;
using Core.Interfaces.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class InventoryRepository : Repository<InventoryMovement>, IInventoryRepository
{
    public InventoryRepository(AppDbContext db) : base(db) { }

    public async Task<IEnumerable<InventoryMovement>> GetByProductAsync(Guid productId, CancellationToken ct = default)
    {
        return await _set
            .Where(m => m.ProductId == productId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task UpdateStockAsync(Guid productId, decimal quantityChange, CancellationToken ct = default)
    {
        var product = await _db.Products.FindAsync(new object[] { productId }, ct);
        
        if (product == null)
        {
            throw new InvalidOperationException($"Product {productId} not found");
        }
            
        product.CurrentStock += quantityChange;
        _db.Products.Update(product);
    }
}
