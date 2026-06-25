using Application.DTOs;
using InvoiceHub.Application.DTOs.System;

namespace Application.Services.System
{
    public interface INotificationService
    {
        Task SendInvoiceEmailAsync(Guid tenantId, Guid invoiceId, CancellationToken ct = default);
        Task SendPaymentReceiptAsync(Guid tenantId, Guid paymentId, CancellationToken ct = default);
        Task SendOverdueRemindersAsync(Guid tenantId, CancellationToken ct = default);
        Task SendLowStockAlertAsync(Guid tenantId, CancellationToken ct = default);
        Task SendTeamInviteAsync(string email, string firstName, string tempPassword, CancellationToken ct = default);
        Task<ApiResponse<PagedResult<NotificationLogDto>>> GetLogsAsync(Guid tenantId, int page, int pageSize, CancellationToken ct = default);
    }
}