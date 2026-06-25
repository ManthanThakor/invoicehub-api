using Core.Entities;
using Core.Enums;

namespace Core.Interfaces.Repositories;

public interface IExpenseRepository : IRepository<Expense>
{
    Task<decimal> GetTotalExpensesAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<IEnumerable<(ExpenseCategory Category, decimal Total)>> GetByCategory(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default);
}
