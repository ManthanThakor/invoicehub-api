using Application.DTOs;
using Application.Services.Sales;
using Application.Services.Tenancy;
using Application.Services.Utilities;
using Core.Entities;
using Core.Enums;
using Core.Interfaces.Repositories;
using InvoiceHub.Application.DTOs.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Services.System
{

    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notifications;
        private readonly IInvoiceRepository _invoices;
        private readonly IPaymentRepository _payments;
        private readonly IProductRepository _products;
        private readonly ITenantRepository _tenants;
        private readonly IUserRepository _users;
        private readonly IPdfService _pdf;
        private readonly IEmailService _email;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<NotificationService> _log;

        public NotificationService(
            INotificationRepository notifications,
            IInvoiceRepository invoices,
            IPaymentRepository payments,
            IProductRepository products,
            ITenantRepository tenants,
            IUserRepository users,
            IPdfService pdf,
            IEmailService email,
            IUnitOfWork uow,
            ILogger<NotificationService> log)
        {
            _notifications = notifications; _invoices = invoices;
            _payments = payments; _products = products;
            _tenants = tenants; _users = users;
            _pdf = pdf; _email = email; _uow = uow; _log = log;
        }

        // ── Send Invoice Email ────────────────────────────────────────────
        public async Task SendInvoiceEmailAsync(
            Guid tenantId, Guid invoiceId, CancellationToken ct = default)
        {
            var invoice = await _invoices.GetWithItemsAsync(invoiceId, tenantId, ct);
            if (invoice == null) return;

            var tenant = await _tenants.GetByIdAsync(tenantId, ct);
            if (tenant == null || string.IsNullOrEmpty(invoice.Customer.Email)) return;

            var pdfBytes = _pdf.GenerateInvoicePdf(
                InvoiceService.MapInvoice(invoice), TenantService.MapTenant(tenant));

            await _email.SendInvoiceAsync(
                invoice.Customer.Email, invoice.Customer.Name,
                invoice.InvoiceNumber, pdfBytes, tenant.BusinessName);

            await LogNotification(tenantId, NotificationType.Email,
                $"Invoice {invoice.InvoiceNumber}", invoice.Customer.Email,
                "Invoice", invoice.Id, ct);
        }

        // ── Send Payment Receipt ──────────────────────────────────────────
        public async Task SendPaymentReceiptAsync(
            Guid tenantId, Guid paymentId, CancellationToken ct = default)
        {
            var payment = await _payments.Query(tenantId)
                .Include(p => p.Customer)
                .Include(p => p.Invoice)
                .FirstOrDefaultAsync(p => p.Id == paymentId, ct);

            if (payment?.Customer?.Email == null) return;

            await _email.SendPaymentReceiptAsync(
                payment.Customer.Email, payment.Customer.Name,
                payment.Invoice?.InvoiceNumber ?? "-",
                payment.Amount, payment.PaymentDate);

            await LogNotification(tenantId, NotificationType.Email,
                $"Payment Receipt {payment.PaymentNumber}", payment.Customer.Email,
                "Payment", paymentId, ct);
        }

        // ── Send Overdue Reminders (batch) ────────────────────────────────
        public async Task SendOverdueRemindersAsync(
            Guid tenantId, CancellationToken ct = default)
        {
            var overdueInvoices = await _invoices.GetOverdueAsync(tenantId, ct);
            var count = 0;

            foreach (var inv in overdueInvoices)
            {
                if (string.IsNullOrEmpty(inv.Customer?.Email)) continue;

                await _email.SendOverdueReminderAsync(
                    inv.Customer.Email, inv.Customer.Name,
                    inv.InvoiceNumber, inv.BalanceDue, inv.DueDate ?? DateTime.UtcNow);

                await LogNotification(tenantId, NotificationType.Email,
                    $"Overdue Reminder – {inv.InvoiceNumber}", inv.Customer.Email,
                    "Invoice", inv.Id, ct);
                count++;
            }

            _log.LogInformation("Sent {Count} overdue reminders for tenant {TenantId}", count, tenantId);
        }

        // ── Send Low Stock Alert ──────────────────────────────────────────
        public async Task SendLowStockAlertAsync(
            Guid tenantId, CancellationToken ct = default)
        {
            var lowStock = (await _products.GetLowStockAsync(tenantId, ct)).ToList();
            if (!lowStock.Any()) return;

            // Find admin email for this tenant
            var admin = await _users.FirstOrDefaultAsync(
                u => u.TenantId == tenantId && u.Role == UserRole.Admin, ct);
            if (admin?.Email == null) return;

            await _email.SendLowStockAlertAsync(admin.Email,
                lowStock.Select(p => (p.Name, p.CurrentStock)));

            await LogNotification(tenantId, NotificationType.Email,
                "Low Stock Alert", admin.Email, null, null, ct);
        }

        // ── Send Team Invite ──────────────────────────────────────────────
        public async Task SendTeamInviteAsync(
            string email, string firstName, string tempPassword, CancellationToken ct = default)
        {
            await _email.SendTeamInviteAsync(email, firstName, tempPassword);
            _log.LogInformation("Team invite sent to {Email}", email);
        }

        // ── Get Notification Logs ─────────────────────────────────────────
        public async Task<ApiResponse<PagedResult<NotificationLogDto>>> GetLogsAsync(
            Guid tenantId, int page, int pageSize, CancellationToken ct = default)
        {
            var query = _notifications.Query(tenantId)
                .OrderByDescending(n => n.CreatedAt);

            var total = await query.CountAsync(ct);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NotificationLogDto(
                    n.Id, n.Type, n.Status, n.Subject, n.Recipient,
                    n.RetryCount, n.SentAt, n.ErrorMessage, n.CreatedAt))
                .ToListAsync(ct);

            return ApiResponse<PagedResult<NotificationLogDto>>.Ok(
                new PagedResult<NotificationLogDto>(items, total, page, pageSize));
        }

        // ── Private: log to notification table ───────────────────────────
        private async Task LogNotification(
            Guid tenantId, NotificationType type, string subject,
            string? recipient, string? refType, Guid? refId, CancellationToken ct)
        {
            var notification = new Notification
            {
                TenantId = tenantId,
                Type = type,
                Status = NotificationStatus.Sent,
                Subject = subject,
                Body = subject,
                Recipient = recipient,
                ReferenceType = refType,
                ReferenceId = refId,
                SentAt = DateTime.UtcNow
            };
            await _notifications.AddAsync(notification, ct);
            await _uow.SaveChangesAsync(ct);
        }
    }
}
