using Core.Entities;
using Core.Interfaces.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class TenantRepository : Repository<Tenant>, ITenantRepository
{
    public TenantRepository(AppDbContext db) : base(db) { }

    public async Task<bool> IsGSTINUniqueAsync(string gstin, Guid? excludeId = null, CancellationToken ct = default)
    {
        var query = _set.Where(t => t.GSTIN == gstin);
        
        if (excludeId.HasValue) 
        {
            query = query.Where(t => t.Id != excludeId.Value);
        }
        
        return !await query.AnyAsync(ct);
    }
}
