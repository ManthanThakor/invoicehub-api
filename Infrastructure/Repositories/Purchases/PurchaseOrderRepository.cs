using Core.Entities;
using Core.Enums;
using Core.Interfaces.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class PurchaseOrderRepository : Repository<PurchaseOrder>, IPurchaseOrderRepository
{
    public PurchaseOrderRepository(AppDbContext db) : base(db) { }

    public async Task<string> GeneratePONumberAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.FindAsync(new object[] { tenantId }, ct);
        
        if (tenant == null)
        {
            throw new InvalidOperationException("Tenant not found");
        }

        var now = DateTime.UtcNow;
        var fyYear = now.Month >= 4 ? now.Year : now.Year - 1;
        var number = $"{tenant.PurchasePrefix}-{(fyYear % 100).ToString("D2")}{((fyYear + 1) % 100).ToString("D2")}-{tenant.PurchaseCounter.ToString("D4")}";

        tenant.PurchaseCounter++;
        _db.Tenants.Update(tenant);

        return number;
    }

    public async Task<PurchaseOrder?> GetWithItemsAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        return await _set
            .Where(p => p.Id == id && p.TenantId == tenantId)
            .Include(p => p.Items)
                .ThenInclude(x => x.Product)
            .Include(p => p.Supplier)
            .Include(p => p.Payments)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<decimal> GetTotalPurchasesAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        return await _set
            .Where(p => p.TenantId == tenantId
                     && p.PODate >= from 
                     && p.PODate <= to
                     && p.Status != PurchaseOrderStatus.Cancelled
                     && p.Status != PurchaseOrderStatus.Draft)
            .SumAsync(p => p.GrandTotal, ct);
    }
}
