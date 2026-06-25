using Application.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Services.Utilities
{
    /// <summary>
    /// Implementation of PDF generation service using QuestPDF.
    /// Handles the layout and rendering of business documents.
    /// </summary>
    public class PdfService : IPdfService
    {
        public byte[] GenerateInvoicePdf(InvoiceDto invoice, TenantDto tenant)
        {
            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(50);
                    page.Header().Element(h => ComposeHeader(h, tenant, "TAX INVOICE", invoice.InvoiceNumber));
                    page.Content().Element(c => ComposeInvoiceContent(c, invoice, tenant));
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                    });
                });
            });

            return document.GeneratePdf();
        }

        public byte[] GeneratePurchaseOrderPdf(PurchaseOrderDto po, TenantDto tenant)
        {
            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(50);
                    page.Header().Element(h => ComposeHeader(h, tenant, "PURCHASE ORDER", po.PONumber));
                    page.Content().Element(c => ComposePurchaseOrderContent(c, po, tenant));
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                    });
                });
            });

            return document.GeneratePdf();
        }

        public byte[] GeneratePaymentReceiptPdf(PaymentDto payment, TenantDto tenant)
        {
            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(50);
                    page.Header().Element(h => ComposeHeader(h, tenant, "PAYMENT RECEIPT", payment.PaymentNumber));
                    page.Content().PaddingVertical(20).Column(col =>
                    {
                        col.Item().Text($"Date: {payment.PaymentDate:dd/MM/yyyy}");
                        col.Item().PaddingTop(10).Text(x =>
                        {
                            x.Span("Received with thanks from ");
                            x.Span(payment.CustomerName ?? "Customer").Bold();
                            x.Span(" the sum of ");
                            x.Span($"{tenant.CurrencyCode} {payment.Amount:N2}").Bold();
                        });
                        col.Item().PaddingTop(10).Text($"Payment Method: {payment.Method}");
                        if (!string.IsNullOrEmpty(payment.ReferenceNumber))
                            col.Item().Text($"Reference No: {payment.ReferenceNumber}");
                        
                        col.Item().PaddingTop(20).Text("Authorized Signature").Italic();
                    });
                });
            });

            return document.GeneratePdf();
        }

        private void ComposeHeader(IContainer container, TenantDto tenant, string title, string number)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text(tenant.BusinessName).FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);
                    col.Item().Text(tenant.Address.Line1);
                    if (!string.IsNullOrEmpty(tenant.Address.Line2))
                        col.Item().Text(tenant.Address.Line2);
                    col.Item().Text($"{tenant.Address.City}, {tenant.Address.State} - {tenant.Address.PinCode}");
                    if (!string.IsNullOrEmpty(tenant.GSTIN))
                        col.Item().Text($"GSTIN: {tenant.GSTIN}").Bold();
                });

                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Text(title).FontSize(24).SemiBold().FontColor(Colors.Grey.Medium);
                    col.Item().Text($"{title.Split(' ').Last()} #: {number}");
                });
            });
        }

        private void ComposeInvoiceContent(IContainer container, InvoiceDto invoice, TenantDto tenant)
        {
            container.PaddingVertical(20).Column(col =>
            {
                // Bill To / Ship To
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Bill To:").SemiBold();
                        c.Item().Text(invoice.CustomerName);
                        if (invoice.BillingAddress != null)
                        {
                            c.Item().Text(invoice.BillingAddress.Line1);
                            if (!string.IsNullOrEmpty(invoice.BillingAddress.Line2))
                                c.Item().Text(invoice.BillingAddress.Line2);
                            c.Item().Text($"{invoice.BillingAddress.City}, {invoice.BillingAddress.State}");
                        }
                        if (!string.IsNullOrEmpty(invoice.CustomerGSTIN))
                            c.Item().Text($"GSTIN: {invoice.CustomerGSTIN}");
                    });

                    row.RelativeItem().AlignRight().Column(c =>
                    {
                        c.Item().Text($"Invoice Date: {invoice.InvoiceDate:dd/MM/yyyy}");
                        if (invoice.DueDate.HasValue)
                            c.Item().Text($"Due Date: {invoice.DueDate.Value:dd/MM/yyyy}");
                    });
                });

                // Items Table
                col.Item().PaddingTop(20).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(25);
                        columns.RelativeColumn(3);
                        columns.ConstantColumn(50);
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("#");
                        header.Cell().Element(CellStyle).Text("Item Description");
                        header.Cell().Element(CellStyle).AlignRight().Text("Qty");
                        header.Cell().Element(CellStyle).AlignRight().Text("Price");
                        header.Cell().Element(CellStyle).AlignRight().Text("GST");
                        header.Cell().Element(CellStyle).AlignRight().Text("Total");

                        static IContainer CellStyle(IContainer container) => container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                    });

                    foreach (var item in invoice.Items)
                    {
                        table.Cell().Element(ItemStyle).Text(item.SortOrder.ToString());
                        table.Cell().Element(ItemStyle).Text(item.ProductName);
                        table.Cell().Element(ItemStyle).AlignRight().Text(item.Quantity.ToString());
                        table.Cell().Element(ItemStyle).AlignRight().Text(item.UnitPrice.ToString("N2"));
                        table.Cell().Element(ItemStyle).AlignRight().Text($"{item.GSTRate}%");
                        table.Cell().Element(ItemStyle).AlignRight().Text(item.TotalAmount.ToString("N2"));

                        static IContainer ItemStyle(IContainer container) => container.PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                    }
                });

                // Totals
                col.Item().AlignRight().PaddingTop(10).Column(t =>
                {
                    t.Item().Text($"Subtotal: {invoice.SubTotal:N2}");
                    if (invoice.DiscountAmount > 0)
                        t.Item().Text($"Discount: -{invoice.DiscountAmount:N2}");
                    t.Item().Text($"Taxable: {invoice.TaxableAmount:N2}");
                    t.Item().Text($"Total Tax: {invoice.TotalTaxAmount:N2}");
                    t.Item().Text($"Grand Total: {tenant.CurrencyCode} {invoice.GrandTotal:N2}").FontSize(14).Bold();
                });
            });
        }

        private void ComposePurchaseOrderContent(IContainer container, PurchaseOrderDto po, TenantDto tenant)
        {
             container.PaddingVertical(20).Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Vendor:").SemiBold();
                        c.Item().Text(po.SupplierName);
                        if (!string.IsNullOrEmpty(po.SupplierGSTIN))
                            c.Item().Text($"GSTIN: {po.SupplierGSTIN}");
                    });

                    row.RelativeItem().AlignRight().Column(c =>
                    {
                        c.Item().Text($"PO Date: {po.PODate:dd/MM/yyyy}");
                        if (po.ExpectedDeliveryDate.HasValue)
                            c.Item().Text($"Exp. Delivery: {po.ExpectedDeliveryDate.Value:dd/MM/yyyy}");
                    });
                });

                col.Item().PaddingTop(20).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(25);
                        columns.RelativeColumn(3);
                        columns.ConstantColumn(50);
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("#");
                        header.Cell().Element(CellStyle).Text("Item Description");
                        header.Cell().Element(CellStyle).AlignRight().Text("Qty");
                        header.Cell().Element(CellStyle).AlignRight().Text("Rate");
                        header.Cell().Element(CellStyle).AlignRight().Text("Total");

                        static IContainer CellStyle(IContainer container) => container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                    });

                    foreach (var item in po.Items)
                    {
                        table.Cell().Element(ItemStyle).Text(item.SortOrder.ToString());
                        table.Cell().Element(ItemStyle).Text(item.ProductName);
                        table.Cell().Element(ItemStyle).AlignRight().Text(item.OrderedQty.ToString());
                        table.Cell().Element(ItemStyle).AlignRight().Text(item.UnitPrice.ToString("N2"));
                        table.Cell().Element(ItemStyle).AlignRight().Text(item.TotalAmount.ToString("N2"));

                        static IContainer ItemStyle(IContainer container) => container.PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                    }
                });

                col.Item().AlignRight().PaddingTop(10).Text($"Grand Total: {tenant.CurrencyCode} {po.GrandTotal:N2}").FontSize(14).Bold();
            });
        }
    }
}
