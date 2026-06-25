using System.Linq.Expressions;
using Core.Common;

namespace Core.Interfaces.Repositories;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<T?> GetByIdWithTenantAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<IEnumerable<T>> GetAllAsync(Guid tenantId, CancellationToken ct = default);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    IQueryable<T> Query(Guid tenantId);  // for complex LINQ with eager loading

    Task AddAsync(T entity, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);
    void Update(T entity);
    void UpdateRange(IEnumerable<T> entities);
    void SoftDelete(T entity);
    void HardDelete(T entity);
}
