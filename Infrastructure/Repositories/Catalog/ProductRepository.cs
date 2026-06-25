using Core.Entities;
using Core.Enums;
using Core.Interfaces.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(AppDbContext db) : base(db) { }

    public async Task<bool> IsSKUUniqueAsync(Guid tenantId, string sku, Guid? excludeId = null, CancellationToken ct = default)
    {
        var query = _set.Where(p => p.TenantId == tenantId && p.SKU == sku);
        
        if (excludeId.HasValue) 
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }
        
        return !await query.AnyAsync(ct);
    }

    public async Task<IEnumerable<Product>> GetLowStockAsync(Guid tenantId, CancellationToken ct = default)
    {
        return await _set
            .Where(p => p.TenantId == tenantId 
                     && p.TrackInventory 
                     && p.CurrentStock <= p.MinimumStock 
                     && p.IsActive)
            .Include(p => p.Category)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Product>> SearchAsync(Guid tenantId, string term, CancellationToken ct = default)
    {
        return await _set
            .Where(p => p.TenantId == tenantId && p.IsActive && (
                p.Name.Contains(term) ||
                (p.SKU != null && p.SKU.Contains(term)) ||
                (p.HSNCode != null && p.HSNCode.Contains(term)) ||
                (p.Barcode != null && p.Barcode.Contains(term))))
            .Take(20)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<(Product Product, decimal Qty)>> GetTopSellingAsync(
        Guid tenantId, int count, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var result = await _db.InvoiceItems
            .Where(i => i.Invoice.TenantId == tenantId
                     && i.Invoice.InvoiceDate >= from 
                     && i.Invoice.InvoiceDate <= to
                     && i.Invoice.Status != InvoiceStatus.Cancelled)
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Qty = g.Sum(i => i.Quantity) })
            .OrderByDescending(x => x.Qty)
            .Take(count)
            .ToListAsync(ct);

        var productIds = result.Select(r => r.ProductId).ToList();

        var products = await _set
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(ct);

        return result.Select(r => (
            products.First(p => p.Id == r.ProductId),
            r.Qty
        ));
    }
}
