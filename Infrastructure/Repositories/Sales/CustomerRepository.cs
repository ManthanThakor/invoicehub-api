using Core.Entities;
using Core.Enums;
using Core.Interfaces.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    public CustomerRepository(AppDbContext db) : base(db) { }

    public async Task<bool> IsGSTINUniqueAsync(Guid tenantId, string gstin, Guid? excludeId = null, CancellationToken ct = default)
    {
        var query = _set.Where(c => c.TenantId == tenantId && c.GSTIN == gstin);
        
        if (excludeId.HasValue) 
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }
        
        return !await query.AnyAsync(ct);
    }

    public async Task<IEnumerable<Customer>> SearchAsync(Guid tenantId, string term, CancellationToken ct = default)
    {
        return await _set
            .Where(c => c.TenantId == tenantId && (
                c.Name.Contains(term) ||
                (c.Email != null && c.Email.Contains(term)) ||
                (c.Phone != null && c.Phone.Contains(term)) ||
                (c.GSTIN != null && c.GSTIN.Contains(term))))
            .Take(20)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<(Customer Customer, decimal Revenue)>> GetTopCustomersAsync(
        Guid tenantId, int count, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var result = await _db.Invoices
            .Where(i => i.TenantId == tenantId 
                     && i.InvoiceDate >= from 
                     && i.InvoiceDate <= to
                     && i.Status != InvoiceStatus.Cancelled
                     && i.Status != InvoiceStatus.Draft)
            .GroupBy(i => i.CustomerId)
            .Select(g => new { CustomerId = g.Key, Revenue = g.Sum(i => i.GrandTotal) })
            .OrderByDescending(x => x.Revenue)
            .Take(count)
            .ToListAsync(ct);

        var customerIds = result.Select(r => r.CustomerId).ToList();

        var customers = await _set
            .Where(c => customerIds.Contains(c.Id))
            .ToListAsync(ct);

        return result.Select(r => (
            customers.First(c => c.Id == r.CustomerId),
            r.Revenue
        ));
    }
}
