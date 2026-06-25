using Core.Entities;
using Core.Enums;
using Core.Interfaces.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ExpenseRepository : Repository<Expense>, IExpenseRepository
{
    public ExpenseRepository(AppDbContext db) : base(db) { }

    public async Task<decimal> GetTotalExpensesAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        return await _set
            .Where(e => e.TenantId == tenantId 
                     && e.ExpenseDate >= from 
                     && e.ExpenseDate <= to)
            .SumAsync(e => e.TotalAmount, ct);
    }

    public async Task<IEnumerable<(ExpenseCategory Category, decimal Total)>> GetByCategory(
        Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var result = await _set
            .Where(e => e.TenantId == tenantId 
                     && e.ExpenseDate >= from 
                     && e.ExpenseDate <= to)
            .GroupBy(e => e.Category)
            .Select(g => new { Category = g.Key, Total = g.Sum(e => e.TotalAmount) })
            .ToListAsync(ct);

        return result.Select(r => (r.Category, r.Total));
    }
}
