using Core.Entities;
using Core.Enums;
using Core.Interfaces.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class InvoiceRepository : Repository<Invoice>, IInvoiceRepository
{
    public InvoiceRepository(AppDbContext db) : base(db) { }

    public async Task<string> GenerateInvoiceNumberAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.FindAsync(new object[] { tenantId }, ct);
        
        if (tenant == null)
        {
            throw new InvalidOperationException("Tenant not found");
        }

        var now = DateTime.UtcNow;
        var fyYear = now.Month >= 4 ? now.Year : now.Year - 1;
        var number = $"{tenant.InvoicePrefix}-{(fyYear % 100).ToString("D2")}{((fyYear + 1) % 100).ToString("D2")}-{tenant.InvoiceCounter.ToString("D4")}";

        tenant.InvoiceCounter++;
        _db.Tenants.Update(tenant);

        return number;
    }

    public async Task<IEnumerable<Invoice>> GetOverdueAsync(Guid tenantId, CancellationToken ct = default)
    {
        return await _set
            .Where(i => i.TenantId == tenantId
                     && i.DueDate < DateTime.UtcNow
                     && i.Status != InvoiceStatus.Paid
                     && i.Status != InvoiceStatus.Cancelled)
            .Include(i => i.Customer)
            .OrderBy(i => i.DueDate)
            .ToListAsync(ct);
    }

    public async Task<decimal> GetTotalRevenueAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        return await _set
            .Where(i => i.TenantId == tenantId
                     && i.InvoiceDate >= from 
                     && i.InvoiceDate <= to
                     && i.Status != InvoiceStatus.Cancelled
                     && i.Status != InvoiceStatus.Draft)
            .SumAsync(i => i.GrandTotal, ct);
    }

    public async Task<IEnumerable<Invoice>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default)
    {
        return await _set
            .Where(i => i.CustomerId == customerId)
            .Include(i => i.Items)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync(ct);
    }

    public async Task<Invoice?> GetWithItemsAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        return await _set
            .Where(i => i.Id == id && i.TenantId == tenantId)
            .Include(i => i.Items)
                .ThenInclude(x => x.Product)
            .Include(i => i.Customer)
            .Include(i => i.Payments)
            .Include(i => i.CreditNotes)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IEnumerable<Invoice>> GetByDateRangeAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        return await _set
            .Where(i => i.TenantId == tenantId 
                     && i.InvoiceDate >= from 
                     && i.InvoiceDate <= to)
            .Include(i => i.Items)
            .Include(i => i.Customer)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync(ct);
    }
}
