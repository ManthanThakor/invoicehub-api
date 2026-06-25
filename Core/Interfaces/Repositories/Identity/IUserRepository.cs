using Core.Entities;
using System.Linq.Expressions;

namespace Core.Interfaces.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task AddRefreshTokenAsync(RefreshToken refresh, CancellationToken ct);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByGoogleIdAsync(string googleId, CancellationToken ct = default);
    Task<bool> IsEmailUniqueAsync(string email, Guid? excludeId = null, CancellationToken ct = default);
    //Task<User?> FirstOrDefaultAsync(Ekxpression<Func<User, bool>> predicate, CancellationToken ct = default);
}