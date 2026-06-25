using Core.Entities;
using Core.Enums;
using Core.Interfaces.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class PaymentRepository : Repository<Payment>, IPaymentRepository
{
    public PaymentRepository(AppDbContext db) : base(db) { }

    public async Task<IEnumerable<Payment>> GetByInvoiceAsync(Guid invoiceId, CancellationToken ct = default)
    {
        return await _set
            .Where(p => p.InvoiceId == invoiceId)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync(ct);
    }

    public async Task<decimal> GetCollectedAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        return await _set
            .Where(p => p.TenantId == tenantId
                     && p.PaymentDate >= from 
                     && p.PaymentDate <= to
                     && p.Status == PaymentStatus.Completed
                     && !p.IsRefund
                     && p.InvoiceId != null)
            .SumAsync(p => p.Amount, ct);
    }
}
