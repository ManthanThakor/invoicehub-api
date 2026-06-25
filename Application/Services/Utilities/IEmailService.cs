namespace Application.Services.Utilities
{
    public interface IEmailService
    {
        Task SendEmailVerificationAsync(string toEmail, string firstName, string token);
        Task SendPasswordResetAsync(string toEmail, string firstName, string token);
        Task SendInvoiceAsync(string toEmail, string customerName, string invoiceNumber, byte[] pdfBytes, string tenantName);
        Task SendPaymentReceiptAsync(string toEmail, string customerName, string invoiceNumber, decimal amount, DateTime paymentDate);
        Task SendTeamInviteAsync(string toEmail, string firstName, string tempPassword);
        Task SendOverdueReminderAsync(string toEmail, string customerName, string invoiceNumber, decimal amountDue, DateTime dueDate);
        Task SendLowStockAlertAsync(string toEmail, IEnumerable<(string ProductName, decimal Stock)> items);
        Task SendRawAsync(string toEmail, string subject, string htmlBody);
    }
}