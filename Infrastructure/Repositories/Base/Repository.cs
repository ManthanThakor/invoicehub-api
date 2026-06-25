using System.Linq.Expressions;
using Core.Common;
using Core.Interfaces.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext _db;
    protected readonly DbSet<T> _set;

    public Repository(AppDbContext db)
    {
        _db = db;
        _set = db.Set<T>();
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _set.FindAsync(new object[] { id }, ct);
    }

    public async Task<T?> GetByIdWithTenantAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        return await _set.FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId, ct);
    }

    public async Task<IEnumerable<T>> GetAllAsync(Guid tenantId, CancellationToken ct = default)
    {
        return await _set.Where(e => e.TenantId == tenantId).ToListAsync(ct);
    }

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        return await _set.Where(predicate).ToListAsync(ct);
    }

    public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        return await _set.FirstOrDefaultAsync(predicate, ct);
    }
    public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        return await _set.AnyAsync(predicate, ct);
    }

    public async Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        return await _set.CountAsync(predicate, ct);
    }

    public IQueryable<T> Query(Guid tenantId)
    {
        return _set.Where(e => e.TenantId == tenantId).AsQueryable();
    }

    public async Task AddAsync(T entity, CancellationToken ct = default)
    {
        await _set.AddAsync(entity, ct);
    }

    public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
    {
        await _set.AddRangeAsync(entities, ct);
    }

    public void Update(T entity)
    {
        _set.Update(entity);
    }

    public void UpdateRange(IEnumerable<T> entities)
    {
        _set.UpdateRange(entities);
    }

    public void SoftDelete(T entity)
    {
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        _set.Update(entity);
    }

    public void HardDelete(T entity)
    {
        _set.Remove(entity);
    }
}
