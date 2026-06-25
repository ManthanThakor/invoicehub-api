using Core.Entities;
using Core.Interfaces.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AppDbContext db) : base(db) { }

    public async Task AddRefreshTokenAsync(RefreshToken token, CancellationToken ct = default)
    {
        await _db.Set<RefreshToken>().AddAsync(token, ct);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        return await _set
            .Include(u => u.Tenant)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted, ct);
    }

    public async Task<User?> GetByGoogleIdAsync(string googleId, CancellationToken ct = default)
    {
        return await _set
            .Include(u => u.Tenant)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.GoogleId == googleId, ct);
    }

    public async Task<bool> IsEmailUniqueAsync(string email, Guid? excludeId = null, CancellationToken ct = default)
    {
        var query = _set.Where(u => u.Email == email);
        
        if (excludeId.HasValue) 
        {
            query = query.Where(u => u.Id != excludeId.Value);
        }
        
        return !await query.AnyAsync(ct);
    }
    public override async Task<User?> FirstOrDefaultAsync(
        Expression<Func<User, bool>> predicate, CancellationToken ct = default)
    {
        return await _set
            .Include(u => u.Tenant)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(predicate, ct);
    }
}
