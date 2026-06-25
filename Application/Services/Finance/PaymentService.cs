using Application.DTOs;
using Application.Services.Sales;
using Application.Services.System;
using Core.Entities;
using Core.Enums;
using Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Services.Finance
{

    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _payments;
        private readonly IInvoiceRepository _invoices;
        private readonly IPurchaseOrderRepository _pos;
        private readonly IAuditService _audit;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<PaymentService> _log;

        public PaymentService(
            IPaymentRepository payments, IInvoiceRepository invoices,
            IPurchaseOrderRepository pos, IAuditService audit,
            IUnitOfWork uow, ILogger<PaymentService> log)
        {
            _payments = payments; _invoices = invoices;
            _pos = pos; _audit = audit; _uow = uow; _log = log;
        }

        public async Task<ApiResponse<PagedResult<PaymentListDto>>> GetListAsync(
            Guid tenantId, PaymentFilterDto filter, CancellationToken ct = default)
        {
            var query = _payments.Query(tenantId)
                .Include(p => p.Customer)
                .Include(p => p.Invoice)
                .AsQueryable();

            if (filter.InvoiceId.HasValue) query = query.Where(p => p.InvoiceId == filter.InvoiceId);
            if (filter.CustomerId.HasValue) query = query.Where(p => p.CustomerId == filter.CustomerId);
            if (filter.FromDate.HasValue) query = query.Where(p => p.PaymentDate >= filter.FromDate);
            if (filter.ToDate.HasValue) query = query.Where(p => p.PaymentDate <= filter.ToDate);
            if (filter.Method.HasValue) query = query.Where(p => p.Method == filter.Method);
            if (filter.Status.HasValue) query = query.Where(p => p.Status == filter.Status);

            var total = await query.CountAsync(ct);
            var items = await query
                .OrderByDescending(p => p.PaymentDate)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(p => new PaymentListDto(
                    p.Id, p.PaymentNumber, p.PaymentDate, p.Amount, p.Method, p.Status,
                    p.Customer != null ? p.Customer.Name : null,
                    p.Invoice != null ? p.Invoice.InvoiceNumber : null,
                    p.IsRefund))
                .ToListAsync(ct);

            return ApiResponse<PagedResult<PaymentListDto>>.Ok(
                new PagedResult<PaymentListDto>(items, total, filter.Page, filter.PageSize));
        }

        public async Task<ApiResponse<PaymentDto>> GetAsync(
            Guid tenantId, Guid id, CancellationToken ct = default)
        {
            var payment = await _payments.Query(tenantId)
                .Include(p => p.Customer)
                .Include(p => p.Invoice)
                .FirstOrDefaultAsync(p => p.Id == id, ct);

            if (payment == null) return ApiResponse<PaymentDto>.Fail("Payment not found.");
            return ApiResponse<PaymentDto>.Ok(InvoiceService.MapPayment(payment));
        }

        public async Task<ApiResponse<PaymentDto>> RecordAsync(
     Guid tenantId, Guid userId, RecordPaymentDto dto, CancellationToken ct = default)
        {
            if (dto.InvoiceId == null && dto.PurchaseOrderId == null)
                return ApiResponse<PaymentDto>.Fail("Either InvoiceId or PurchaseOrderId is required.");

            Payment? payment = null;

            try
            {
                await _uow.ExecuteInTransactionAsync(async () =>
                {
                    var paymentNumber = $"PAY-{DateTime.UtcNow:yyyyMMdd-HHmmss}";

                    payment = new Payment
                    {
                        TenantId = tenantId,
                        PaymentNumber = paymentNumber,
                        PaymentDate = dto.PaymentDate,
                        Amount = dto.Amount,
                        Method = dto.Method,
                        Status = PaymentStatus.Completed,
                        InvoiceId = dto.InvoiceId,
                        PurchaseOrderId = dto.PurchaseOrderId,
                        ReferenceNumber = dto.ReferenceNumber,
                        BankName = dto.BankName,
                        Notes = dto.Notes,
                        RecordedBy = userId
                    };

                    if (dto.InvoiceId.HasValue)
                    {
                        var invoice = await _invoices.GetByIdWithTenantAsync(
                            dto.InvoiceId.Value, tenantId, ct);
                        if (invoice == null)
                            throw new KeyNotFoundException("Invoice not found.");

                        if (dto.Amount > invoice.BalanceDue)
                            throw new InvalidOperationException(
                                $"Payment ₹{dto.Amount:N2} exceeds balance due ₹{invoice.BalanceDue:N2}.");

                        payment.CustomerId = invoice.CustomerId;
                        invoice.PaidAmount += dto.Amount;
                        invoice.BalanceDue = invoice.GrandTotal - invoice.PaidAmount;
                        invoice.Status = invoice.BalanceDue <= 0
                            ? InvoiceStatus.Paid
                            : InvoiceStatus.PartiallyPaid;

                        _invoices.Update(invoice);
                    }

                    if (dto.PurchaseOrderId.HasValue)
                    {
                        var po = await _pos.GetByIdWithTenantAsync(
                            dto.PurchaseOrderId.Value, tenantId, ct);
                        if (po == null)
                            throw new KeyNotFoundException("Purchase order not found.");

                        if (dto.Amount > po.BalanceDue)
                            throw new InvalidOperationException(
                                $"Payment ₹{dto.Amount:N2} exceeds balance due ₹{po.BalanceDue:N2}.");

                        po.PaidAmount += dto.Amount;
                        po.BalanceDue = po.GrandTotal - po.PaidAmount;
                        _pos.Update(po);
                    }

                    await _payments.AddAsync(payment, ct);
                    await _uow.SaveChangesAsync(ct);

                }, ct);

                await _audit.LogAsync(tenantId, userId, "Payment", payment!.Id, "Create",
                    newValues: new { payment!.Amount, payment.Method }, ct: ct);

                _log.LogInformation("Payment recorded: {PaymentNumber} ₹{Amount}",
                    payment!.PaymentNumber, dto.Amount);

                return ApiResponse<PaymentDto>.Ok(
                    InvoiceService.MapPayment(payment!), "Payment recorded successfully.");
            }
            catch (KeyNotFoundException ex)
            {
                return ApiResponse<PaymentDto>.Fail(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponse<PaymentDto>.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to record payment for tenant {TenantId}", tenantId);
                return ApiResponse<PaymentDto>.Fail("Failed to record payment.");
            }
        }
        public async Task<ApiResponse<bool>> DeleteAsync(
            Guid tenantId, Guid id, CancellationToken ct = default)
        {
            var payment = await _payments.GetByIdWithTenantAsync(id, tenantId, ct);
            if (payment == null) return ApiResponse<bool>.Fail("Payment not found.");

            // Reverse the invoice payment amounts
            if (payment.InvoiceId.HasValue)
            {
                var invoice = await _invoices.GetByIdWithTenantAsync(payment.InvoiceId.Value, tenantId, ct);
                if (invoice != null)
                {
                    invoice.PaidAmount -= payment.Amount;
                    invoice.BalanceDue = invoice.GrandTotal - invoice.PaidAmount;
                    invoice.Status = invoice.PaidAmount <= 0
                        ? InvoiceStatus.Sent
                        : InvoiceStatus.PartiallyPaid;
                    _invoices.Update(invoice);
                }
            }

            _payments.SoftDelete(payment);
            await _uow.SaveChangesAsync(ct);
            return ApiResponse<bool>.Ok(true, "Payment deleted and invoice balance reversed.");
        }
    }

}
