using Application.DTOs;
using Application.Services.System;
using InvoiceHub.Application.DTOs.System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InvoiceHub.API.Controllers.System;

[ApiController]
[Route("api/notifications")]
[Authorize]
[Tags("Notifications")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications) =>
        _notifications = notifications;

    private Guid TenantId => Guid.Parse(User.FindFirstValue("tenantId")!);

    /// <summary>Get notification send history / logs (paginated).</summary>
    [HttpGet("logs")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<PagedResult<NotificationLogDto>>>> GetLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _notifications.GetLogsAsync(TenantId, page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>Manually send invoice PDF email to the customer.</summary>
    [HttpPost("invoices/{invoiceId:guid}/send")]
    [Authorize(Policy = "SalesUp")]
    public async Task<IActionResult> SendInvoiceEmail(Guid invoiceId, CancellationToken ct)
    {
        await _notifications.SendInvoiceEmailAsync(TenantId, invoiceId, ct);
        return Ok(new { Success = true, Message = "Invoice email sent." });
    }

    /// <summary>Manually send payment receipt email to the customer.</summary>
    [HttpPost("payments/{paymentId:guid}/send-receipt")]
    [Authorize(Policy = "AccountantUp")]
    public async Task<IActionResult> SendPaymentReceipt(Guid paymentId, CancellationToken ct)
    {
        await _notifications.SendPaymentReceiptAsync(TenantId, paymentId, ct);
        return Ok(new { Success = true, Message = "Payment receipt sent." });
    }

    /// <summary>
    /// Send overdue payment reminders to all customers with unpaid/overdue invoices.
    /// </summary>
    [HttpPost("send-overdue-reminders")]
    [Authorize(Policy = "ManagerUp")]
    public async Task<IActionResult> SendOverdueReminders(CancellationToken ct)
    {
        await _notifications.SendOverdueRemindersAsync(TenantId, ct);
        return Ok(new { Success = true, Message = "Overdue reminders sent." });
    }

    /// <summary>Send low stock alert email to the tenant admin.</summary>
    [HttpPost("send-low-stock-alert")]
    [Authorize(Policy = "ManagerUp")]
    public async Task<IActionResult> SendLowStockAlert(CancellationToken ct)
    {
        await _notifications.SendLowStockAlertAsync(TenantId, ct);
        return Ok(new { Success = true, Message = "Low stock alert sent." });
    }
}
