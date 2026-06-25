
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

using static System.Net.Mime.MediaTypeNames;

namespace Application.Services.Utilities
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _log;

        private string SmtpHost => _config["Email:Host"] ?? "smtp.gmail.com";
        private int SmtpPort => int.Parse(_config["Email:Port"] ?? "587");
        private bool UseSsl => bool.Parse(_config["Email:UseSsl"] ?? "false");
        private string Username => _config["Email:Username"] ?? "";
        private string Password => _config["Email:Password"] ?? "";
        private string FromName => _config["Email:FromName"] ?? "InvoiceHub";
        private string FromAddress => _config["Email:FromAddress"] ?? Username;
        private string AppUrl => _config["App:BaseUrl"] ?? "https://app.invoicehub.in";

        public EmailService(IConfiguration config, ILogger<EmailService> log)
        {
            _config = config; _log = log;
        }

        public Task SendEmailVerificationAsync(string toEmail, string firstName, string token)
        {
            var link = $"{AppUrl}/auth/verify-email?token={token}";
            var body = BuildBody("Email Verification",
                "<h2>Welcome to InvoiceHub, " + firstName + "!</h2>" +
                "<p>Please verify your email address to activate your account.</p>" +
                ActionButton(link, "Verify Email Address", "#4F46E5") +
                "<p style=\"color:#6b7280;font-size:13px\">This link expires in 24 hours.</p>");
            return SendRawAsync(toEmail, "Verify your InvoiceHub email", body);
        }

        public Task SendPasswordResetAsync(string toEmail, string firstName, string token)
        {
            var link = $"{AppUrl}/auth/reset-password?token={token}";
            var body = BuildBody("Password Reset",
                "<h2>Password Reset Request</h2>" +
                "<p>Hi <strong>" + firstName + "</strong>, we received a request to reset your InvoiceHub password.</p>" +
                ActionButton(link, "Reset Password", "#DC2626") +
                "<p style=\"color:#6b7280;font-size:13px\">This link expires in 2 hours. " +
                "If you did not request this, please ignore this email.</p>");
            return SendRawAsync(toEmail, "Reset your InvoiceHub password", body);
        }

        public async Task SendInvoiceAsync(
            string toEmail, string customerName,
            string invoiceNumber, byte[] pdfBytes, string tenantName)
        {
            var html = BuildBody("Invoice " + invoiceNumber,
                "<h2>Invoice " + invoiceNumber + "</h2>" +
                "<p>Dear <strong>" + customerName + "</strong>,</p>" +
                "<p>Please find your invoice <strong>" + invoiceNumber + "</strong> " +
                "from <strong>" + tenantName + "</strong> attached to this email.</p>" +
                "<p>Thank you for your business!</p>");

            var message = BuildMessage(toEmail, customerName,
                "Invoice " + invoiceNumber + " from " + tenantName, html);

            // Attach PDF — explicitly qualify MimeKit.Multipart to avoid ambiguity
            var multipart = new MimeKit.Multipart("mixed");
            multipart.Add(message.Body!);
            multipart.Add(new MimePart("application", "pdf")
            {
                Content = new MimeContent(new MemoryStream(pdfBytes)),
                ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                ContentTransferEncoding = ContentEncoding.Base64,
                FileName = invoiceNumber + ".pdf"
            });
            message.Body = multipart;

            await SendAsync(message);
        }

        public Task SendPaymentReceiptAsync(
            string toEmail, string customerName,
            string invoiceNumber, decimal amount, DateTime paymentDate)
        {
            var body = BuildBody("Payment Received",
                "<h2>Payment Received &#x2713;</h2>" +
                "<p>Dear <strong>" + customerName + "</strong>,</p>" +
                "<p>We have received your payment of <strong>&#x20b9;" + amount.ToString("N2") +
                "</strong> for invoice <strong>" + invoiceNumber + "</strong>" +
                " on <strong>" + paymentDate.ToString("dd MMM yyyy") + "</strong>.</p>" +
                "<p>Thank you for the prompt payment!</p>");
            return SendRawAsync(toEmail, "Payment Receipt \u2013 " + invoiceNumber, body);
        }

        public Task SendTeamInviteAsync(string toEmail, string firstName, string tempPassword)
        {
            var body = BuildBody("Team Invitation",
                "<h2>You&#39;ve been invited to InvoiceHub &#127881;</h2>" +
                "<p>Hi <strong>" + firstName + "</strong>,</p>" +
                "<p>Your account has been created. Log in with the credentials below " +
                "and change your password on first login.</p>" +
                "<table style=\"border-collapse:collapse;margin:16px 0\">" +
                "<tr><td style=\"padding:6px 12px;background:#f3f4f6;font-weight:bold\">Email</td>" +
                "<td style=\"padding:6px 12px;border:1px solid #e5e7eb\">" + toEmail + "</td></tr>" +
                "<tr><td style=\"padding:6px 12px;background:#f3f4f6;font-weight:bold\">Password</td>" +
                "<td style=\"padding:6px 12px;border:1px solid #e5e7eb;font-family:monospace\">" + tempPassword + "</td></tr>" +
                "</table>" +
                ActionButton(AppUrl + "/auth/login", "Login to InvoiceHub", "#4F46E5"));
            return SendRawAsync(toEmail, "You're invited to InvoiceHub", body);
        }

        public Task SendOverdueReminderAsync(
            string toEmail, string customerName,
            string invoiceNumber, decimal amountDue, DateTime dueDate)
        {
            var overdueDays = (DateTime.Today - dueDate.Date).Days;
            var body = BuildBody("Payment Overdue",
                "<div style=\"background:#fee2e2;border-left:4px solid #dc2626;padding:12px;margin-bottom:16px\">" +
                "<strong style=\"color:#dc2626\">&#9888; Payment Overdue</strong></div>" +
                "<p>Dear <strong>" + customerName + "</strong>,</p>" +
                "<p>Invoice <strong>" + invoiceNumber + "</strong> of amount " +
                "<strong>&#x20b9;" + amountDue.ToString("N2") + "</strong> was due on " +
                "<strong>" + dueDate.ToString("dd MMM yyyy") + "</strong> (" +
                overdueDays + " day" + (overdueDays != 1 ? "s" : "") + " ago).</p>" +
                "<p>Please make the payment at your earliest convenience.</p>");
            return SendRawAsync(toEmail, "Payment Overdue \u2013 " + invoiceNumber, body);
        }

        public Task SendLowStockAlertAsync(
            string toEmail, IEnumerable<(string ProductName, decimal Stock)> items)
        {
            var rows = string.Join("", items.Select(i =>
                "<tr>" +
                "<td style=\"padding:8px 12px;border:1px solid #e5e7eb\">" + i.ProductName + "</td>" +
                "<td style=\"padding:8px 12px;border:1px solid #e5e7eb;color:#dc2626;font-weight:bold\">" + i.Stock + "</td>" +
                "</tr>"));

            var body = BuildBody("Low Stock Alert",
                "<h2>Low Stock Alert</h2>" +
                "<p>The following products are at or below their minimum stock level:</p>" +
                "<table style=\"border-collapse:collapse;width:100%;margin:16px 0\">" +
                "<thead><tr style=\"background:#f3f4f6\">" +
                "<th style=\"padding:8px 12px;border:1px solid #e5e7eb;text-align:left\">Product</th>" +
                "<th style=\"padding:8px 12px;border:1px solid #e5e7eb;text-align:left\">Current Stock</th>" +
                "</tr></thead><tbody>" + rows + "</tbody></table>" +
                "<p>Please raise purchase orders to avoid stockouts.</p>");

            return SendRawAsync(toEmail, "&#9888; Low Stock Alert \u2013 Action Required", body);
        }

        public async Task SendRawAsync(string toEmail, string subject, string htmlBody)
            => await SendAsync(BuildMessage(toEmail, toEmail, subject, htmlBody));

        // ── Private helpers ───────────────────────────────────────────────
        private MimeMessage BuildMessage(
            string toEmail, string toName, string subject, string htmlBody)
        {
            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress(FromName, FromAddress));
            msg.To.Add(new MailboxAddress(toName, toEmail));
            msg.Subject = subject;
            msg.Body = new TextPart("html") { Text = htmlBody };
            return msg;
        }

        private async Task SendAsync(MimeMessage message)
        {
            try
            {
                using var client = new SmtpClient();
                var secure = UseSsl
                    ? SecureSocketOptions.SslOnConnect
                    : SecureSocketOptions.StartTls;

                await client.ConnectAsync(SmtpHost, SmtpPort, secure);
                await client.AuthenticateAsync(Username, Password);
                await client.SendAsync(message);
                await client.DisconnectAsync(quit: true);

                _log.LogInformation("Email sent to {To}: {Subject}", message.To, message.Subject);
            }
            catch (Exception ex)
            {
                // Never throw — email failure must NOT break the main flow
                _log.LogError(ex, "Email failed to {To}: {Subject}", message.To, message.Subject);
            }
        }

        // Build full HTML email body — plain string concatenation avoids raw-string CSS ambiguity
        private static string BuildBody(string title, string content)
        {
            return
                "<!DOCTYPE html><html><head><meta charset=\"utf-8\"/>" +
                "<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"/>" +
                "<style>" +
                "body{font-family:'Segoe UI',Arial,sans-serif;background:#f9fafb;margin:0;padding:0}" +
                ".wrap{max-width:600px;margin:32px auto;background:#fff;border-radius:10px;" +
                "box-shadow:0 2px 8px rgba(0,0,0,.08);overflow:hidden}" +
                ".hdr{background:#4F46E5;color:#fff;padding:24px 32px}" +
                ".hdr h1{margin:0;font-size:18px;letter-spacing:.5px}" +
                ".bdy{padding:32px;color:#111827;line-height:1.7}" +
                ".ftr{background:#f3f4f6;padding:16px 32px;font-size:12px;color:#6b7280;" +
                "border-top:1px solid #e5e7eb}" +
                "h2{color:#111827;margin-top:0}" +
                "</style></head><body>" +
                "<div class=\"wrap\">" +
                "<div class=\"hdr\"><h1>InvoiceHub &mdash; " + title + "</h1></div>" +
                "<div class=\"bdy\">" + content + "</div>" +
                "<div class=\"ftr\">" +
                "&copy; " + DateTime.Now.Year + " InvoiceHub. All rights reserved.<br/>" +
                "This is an automated email. Please do not reply directly." +
                "</div></div></body></html>";
        }

        private static string ActionButton(string url, string label, string bgColor) =>
            "<a href=\"" + url + "\" " +
            "style=\"display:inline-block;background:" + bgColor + ";color:#ffffff;" +
            "padding:12px 28px;border-radius:6px;text-decoration:none;" +
            "font-weight:bold;margin:16px 0\">" +
            label + "</a>";
    }
}
