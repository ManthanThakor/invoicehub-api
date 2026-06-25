using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services.Utilities
{
    public interface IPdfService
    {
        byte[] GenerateInvoicePdf(InvoiceDto invoice, TenantDto tenant);
        byte[] GeneratePurchaseOrderPdf(PurchaseOrderDto po, TenantDto tenant);
        byte[] GeneratePaymentReceiptPdf(PaymentDto payment, TenantDto tenant);
    }
}
