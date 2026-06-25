using Core.Entities;
using Core.Interfaces.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class SupplierRepository : Repository<Supplier>, ISupplierRepository
{
    public SupplierRepository(AppDbContext db) : base(db) { }

    public async Task<IEnumerable<Supplier>> SearchAsync(Guid tenantId, string term, CancellationToken ct = default)
    {
        return await _set
            .Where(s => s.TenantId == tenantId && (
                s.Name.Contains(term) ||
                (s.Email != null && s.Email.Contains(term)) ||
                (s.GSTIN != null && s.GSTIN.Contains(term))))
            .Take(20)
            .ToListAsync(ct);
    }
}
