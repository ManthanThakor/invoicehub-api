using Application.DTOs;
using Application.Services.Sales;
using Application.Services.Tenancy;
using Application.Services.Utilities;
using Core.Entities;
using Core.Enums;
using Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using SixLabors.ImageSharp;
using System.Drawing;                          // System.Drawing.Common NuGet package
using Color = System.Drawing.Color;            // explicit alias — avoids ImageSharp ambiguity

#pragma warning disable CS0618 // ExcelPackage.LicenseContext is obsolete

namespace Application.Services.System
{


    public class DocumentService : IDocumentService
    {
        private readonly IInvoiceRepository _invoices;
        private readonly IPurchaseOrderRepository _pos;
        private readonly IPaymentRepository _payments;
        private readonly IExpenseRepository _expenses;
        private readonly ICustomerRepository _customers;
        private readonly IProductRepository _products;
        private readonly ITenantRepository _tenants;
        private readonly IPdfService _pdf;
        private readonly ILogger<DocumentService> _log;

        public DocumentService(
            IInvoiceRepository invoices, IPurchaseOrderRepository pos,
            IPaymentRepository payments, IExpenseRepository expenses,
            ICustomerRepository customers, IProductRepository products,
            ITenantRepository tenants, IPdfService pdf,
            ILogger<DocumentService> log)
        {
            _invoices = invoices; _pos = pos;
            _payments = payments; _expenses = expenses;
            _customers = customers; _products = products;
            _tenants = tenants; _pdf = pdf;
            _log = log;
        }

        // ── Invoice PDF ───────────────────────────────────────────────────
        public async Task<ApiResponse<byte[]>> GetInvoicePdfAsync(
            Guid tenantId, Guid invoiceId, CancellationToken ct = default)
        {
            var invoice = await _invoices.GetWithItemsAsync(invoiceId, tenantId, ct);
            if (invoice == null) return ApiResponse<byte[]>.Fail("Invoice not found.");

            var tenant = await _tenants.GetByIdAsync(tenantId, ct);
            if (tenant == null) return ApiResponse<byte[]>.Fail("Tenant not found.");

            var bytes = _pdf.GenerateInvoicePdf(
                InvoiceService.MapInvoice(invoice),
                TenantService.MapTenant(tenant));

            _log.LogInformation("Invoice PDF generated: {InvoiceNumber}", invoice.InvoiceNumber);
            return ApiResponse<byte[]>.Ok(bytes);
        }

        // ── Purchase Order PDF ────────────────────────────────────────────
        public async Task<ApiResponse<byte[]>> GetPurchaseOrderPdfAsync(
            Guid tenantId, Guid poId, CancellationToken ct = default)
        {
            var po = await _pos.GetWithItemsAsync(poId, tenantId, ct);
            if (po == null) return ApiResponse<byte[]>.Fail("Purchase order not found.");

            var tenant = await _tenants.GetByIdAsync(tenantId, ct);
            if (tenant == null) return ApiResponse<byte[]>.Fail("Tenant not found.");

            var bytes = _pdf.GeneratePurchaseOrderPdf(MapPO(po), TenantService.MapTenant(tenant));
            return ApiResponse<byte[]>.Ok(bytes);
        }

        // ── Payment Receipt PDF ───────────────────────────────────────────
        public async Task<ApiResponse<byte[]>> GetPaymentReceiptPdfAsync(
            Guid tenantId, Guid paymentId, CancellationToken ct = default)
        {
            var payment = await _payments.Query(tenantId)
                .Include(p => p.Customer)
                .Include(p => p.Invoice)
                .FirstOrDefaultAsync(p => p.Id == paymentId, ct);

            if (payment == null) return ApiResponse<byte[]>.Fail("Payment not found.");

            var tenant = await _tenants.GetByIdAsync(tenantId, ct);
            if (tenant == null) return ApiResponse<byte[]>.Fail("Tenant not found.");

            var bytes = _pdf.GeneratePaymentReceiptPdf(
                InvoiceService.MapPayment(payment),
                TenantService.MapTenant(tenant));

            return ApiResponse<byte[]>.Ok(bytes);
        }

        // ── Export Invoices → Excel ───────────────────────────────────────
        public async Task<ApiResponse<byte[]>> ExportInvoicesExcelAsync(
            Guid tenantId, InvoiceFilterDto filter, CancellationToken ct = default)
        {
            var from = filter.FromDate ?? DateTime.UtcNow.AddMonths(-3);
            var to = filter.ToDate ?? DateTime.UtcNow;
            var invoices = (await _invoices.GetByDateRangeAsync(tenantId, from, to, ct)).ToList();

            if (filter.Status.HasValue)
                invoices = invoices.Where(i => i.Status == filter.Status).ToList();

            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            using var pkg = new ExcelPackage();
            var sheet = pkg.Workbook.Worksheets.Add("Invoices");

            ExcelHelper.WriteHeader(sheet, new[]
            {
            "Invoice #","Date","Due Date","Customer","GSTIN",
            "Sub Total","Discount","Taxable","IGST","CGST","SGST",
            "Cess","Total Tax","Grand Total","Paid","Balance","Status"
        });

            var row = 2;
            foreach (var inv in invoices)
            {
                sheet.Cells[row, 1].Value = inv.InvoiceNumber;
                sheet.Cells[row, 2].Value = inv.InvoiceDate.ToString("dd/MM/yyyy");
                sheet.Cells[row, 3].Value = inv.DueDate?.ToString("dd/MM/yyyy") ?? "";
                sheet.Cells[row, 4].Value = inv.Customer?.Name ?? "";
                sheet.Cells[row, 5].Value = inv.Customer?.GSTIN ?? "";
                sheet.Cells[row, 6].Value = inv.SubTotal;
                sheet.Cells[row, 7].Value = inv.DiscountAmount;
                sheet.Cells[row, 8].Value = inv.TaxableAmount;
                sheet.Cells[row, 9].Value = inv.IGSTAmount;
                sheet.Cells[row, 10].Value = inv.CGSTAmount;
                sheet.Cells[row, 11].Value = inv.SGSTAmount;
                sheet.Cells[row, 12].Value = inv.CessAmount;
                sheet.Cells[row, 13].Value = inv.TotalTaxAmount;
                sheet.Cells[row, 14].Value = inv.GrandTotal;
                sheet.Cells[row, 15].Value = inv.PaidAmount;
                sheet.Cells[row, 16].Value = inv.BalanceDue;
                sheet.Cells[row, 17].Value = inv.Status.ToString();

                // Status colour
                sheet.Cells[row, 17].Style.Font.Color.SetColor(inv.Status switch
                {
                    InvoiceStatus.Paid => Color.Green,
                    InvoiceStatus.Overdue => Color.Red,
                    InvoiceStatus.Cancelled => Color.Gray,
                    _ => Color.Black
                });

                // Currency format on amount columns 6–16
                for (var col = 6; col <= 16; col++)
                    ExcelHelper.SetCurrency(sheet.Cells[row, col]);

                row++;
            }

            // Summary row
            sheet.Cells[row, 1].Value = "TOTAL";
            sheet.Cells[row, 1].Style.Font.Bold = true;
            sheet.Cells[row, 14].Value = invoices.Sum(i => i.GrandTotal);
            sheet.Cells[row, 15].Value = invoices.Sum(i => i.PaidAmount);
            sheet.Cells[row, 16].Value = invoices.Sum(i => i.BalanceDue);
            for (var col = 14; col <= 16; col++)
            {
                sheet.Cells[row, col].Style.Font.Bold = true;
                ExcelHelper.SetCurrency(sheet.Cells[row, col]);
            }

            ExcelHelper.AutoFit(sheet);

            _log.LogInformation("Exported {Count} invoices to Excel for tenant {TenantId}",
                invoices.Count, tenantId);

            return ApiResponse<byte[]>.Ok(pkg.GetAsByteArray());
        }

        // ── Export Expenses → Excel ───────────────────────────────────────
        public async Task<ApiResponse<byte[]>> ExportExpensesExcelAsync(
            Guid tenantId, ExpenseFilterDto filter, CancellationToken ct = default)
        {
            var from = filter.FromDate ?? DateTime.UtcNow.AddMonths(-3);
            var to = filter.ToDate ?? DateTime.UtcNow;
            var categories = (await _expenses.GetByCategory(tenantId, from, to, ct)).ToList();
            var allExpenses = (await _expenses.FindAsync(
                e => e.TenantId == tenantId && e.ExpenseDate >= from && e.ExpenseDate <= to, ct))
                .OrderByDescending(e => e.ExpenseDate)
                .ToList();

            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            using var pkg = new ExcelPackage();

            // Sheet 1 — Detail
            var detail = pkg.Workbook.Worksheets.Add("Expenses");
            ExcelHelper.WriteHeader(detail, new[]
            {
            "Title","Category","Date","Amount","GST",
            "Total","Payment Method","Vendor","Reference","Notes"
        });

            var row = 2;
            foreach (var e in allExpenses)
            {
                detail.Cells[row, 1].Value = e.Title;
                detail.Cells[row, 2].Value = e.Category.ToString();
                detail.Cells[row, 3].Value = e.ExpenseDate.ToString("dd/MM/yyyy");
                detail.Cells[row, 4].Value = e.Amount;
                detail.Cells[row, 5].Value = e.GSTAmount ?? 0;
                detail.Cells[row, 6].Value = e.TotalAmount;
                detail.Cells[row, 7].Value = e.PaymentMethod.ToString();
                detail.Cells[row, 8].Value = e.VendorName ?? "";
                detail.Cells[row, 9].Value = e.ReferenceNumber ?? "";
                detail.Cells[row, 10].Value = e.Notes ?? "";

                ExcelHelper.SetCurrency(detail.Cells[row, 4]);
                ExcelHelper.SetCurrency(detail.Cells[row, 5]);
                ExcelHelper.SetCurrency(detail.Cells[row, 6]);
                row++;
            }
            ExcelHelper.AutoFit(detail);

            // Sheet 2 — Category summary
            var summary = pkg.Workbook.Worksheets.Add("By Category");
            ExcelHelper.WriteHeader(summary, new[] { "Category", "Total Amount" });

            var catRow = 2;
            foreach (var cat in categories.OrderByDescending(c => c.Total))
            {
                summary.Cells[catRow, 1].Value = cat.Category.ToString();
                summary.Cells[catRow, 2].Value = cat.Total;
                ExcelHelper.SetCurrency(summary.Cells[catRow, 2]);
                catRow++;
            }
            summary.Cells[catRow, 1].Value = "GRAND TOTAL";
            summary.Cells[catRow, 1].Style.Font.Bold = true;
            summary.Cells[catRow, 2].Value = categories.Sum(c => c.Total);
            summary.Cells[catRow, 2].Style.Font.Bold = true;
            ExcelHelper.SetCurrency(summary.Cells[catRow, 2]);
            ExcelHelper.AutoFit(summary);

            return ApiResponse<byte[]>.Ok(pkg.GetAsByteArray());
        }

        // ── Export Customers → Excel ──────────────────────────────────────
        public async Task<ApiResponse<byte[]>> ExportCustomersExcelAsync(
            Guid tenantId, CancellationToken ct = default)
        {
            var customers = (await _customers.GetAllAsync(tenantId, ct))
                .OrderBy(c => c.Name).ToList();

            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            using var pkg = new ExcelPackage();
            var sheet = pkg.Workbook.Worksheets.Add("Customers");

            ExcelHelper.WriteHeader(sheet, new[]
            {
            "Name","Type","Status","Email","Phone",
            "GSTIN","PAN","City","State","Pin Code",
            "Credit Limit","Payment Terms (Days)","Tags"
        });

            var row = 2;
            foreach (var c in customers)
            {
                sheet.Cells[row, 1].Value = c.Name;
                sheet.Cells[row, 2].Value = c.CustomerType.ToString();
                sheet.Cells[row, 3].Value = c.Status.ToString();
                sheet.Cells[row, 4].Value = c.Email ?? "";
                sheet.Cells[row, 5].Value = c.Phone ?? "";
                sheet.Cells[row, 6].Value = c.GSTIN ?? "";
                sheet.Cells[row, 7].Value = c.PAN ?? "";
                sheet.Cells[row, 8].Value = c.BillingCity ?? "";
                sheet.Cells[row, 9].Value = c.BillingState ?? "";
                sheet.Cells[row, 10].Value = c.BillingPinCode ?? "";
                sheet.Cells[row, 11].Value = c.CreditLimit;
                ExcelHelper.SetCurrency(sheet.Cells[row, 11]);
                sheet.Cells[row, 12].Value = c.PaymentTermDays ?? 0;
                sheet.Cells[row, 13].Value = c.Tags ?? "";
                row++;
            }
            ExcelHelper.AutoFit(sheet);
            return ApiResponse<byte[]>.Ok(pkg.GetAsByteArray());
        }

        // ── Export Products → Excel ───────────────────────────────────────
        public async Task<ApiResponse<byte[]>> ExportProductsExcelAsync(
            Guid tenantId, CancellationToken ct = default)
        {
            var products = (await _products.GetAllAsync(tenantId, ct))
                .OrderBy(p => p.Name).ToList();

            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            using var pkg = new ExcelPackage();
            var sheet = pkg.Workbook.Worksheets.Add("Products");

            ExcelHelper.WriteHeader(sheet, new[]
            {
            "Name","SKU","HSN/SAC","Type","Unit",
            "Purchase Price","Sale Price","MRP","GST %",
            "Current Stock","Min Stock","Reorder Qty","Stock Value","Status"
        });

            var row = 2;
            foreach (var p in products)
            {
                sheet.Cells[row, 1].Value = p.Name;
                sheet.Cells[row, 2].Value = p.SKU ?? "";
                sheet.Cells[row, 3].Value = p.HSNCode ?? "";
                sheet.Cells[row, 4].Value = p.ProductType.ToString();
                sheet.Cells[row, 5].Value = p.Unit.ToString();
                sheet.Cells[row, 6].Value = p.PurchasePrice;
                sheet.Cells[row, 7].Value = p.SalePrice;
                sheet.Cells[row, 8].Value = p.MRP;
                sheet.Cells[row, 9].Value = p.GSTRate;
                sheet.Cells[row, 10].Value = p.CurrentStock;
                sheet.Cells[row, 11].Value = p.MinimumStock;
                sheet.Cells[row, 12].Value = p.ReorderQty;
                sheet.Cells[row, 13].Value = p.CurrentStock * p.PurchasePrice;
                sheet.Cells[row, 14].Value = p.IsActive ? "Active" : "Inactive";

                ExcelHelper.SetCurrency(sheet.Cells[row, 6]);
                ExcelHelper.SetCurrency(sheet.Cells[row, 7]);
                ExcelHelper.SetCurrency(sheet.Cells[row, 8]);
                ExcelHelper.SetCurrency(sheet.Cells[row, 13]);
                sheet.Cells[row, 9].Style.Numberformat.Format = "0.00\"\\%\"";

                // Highlight low-stock rows light-red
                if (p.TrackInventory && p.CurrentStock <= p.MinimumStock)
                {
                    var range = sheet.Cells[row, 1, row, 14];
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 254, 226, 226));
                }
                row++;
            }
            ExcelHelper.AutoFit(sheet);
            return ApiResponse<byte[]>.Ok(pkg.GetAsByteArray());
        }

        // ── Export GSTR-1 → Excel ─────────────────────────────────────────
        public async Task<ApiResponse<byte[]>> ExportGSTR1Async(
            Guid tenantId, int month, int year, CancellationToken ct = default)
        {
            var from = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var to = from.AddMonths(1).AddTicks(-1);
            var invoices = (await _invoices.GetByDateRangeAsync(tenantId, from, to, ct))
                .Where(i => i.Status != InvoiceStatus.Cancelled
                         && i.Status != InvoiceStatus.Draft)
                .ToList();

            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            using var pkg = new ExcelPackage();

            // B2B sheet
            var b2b = pkg.Workbook.Worksheets.Add("B2B Invoices");
            ExcelHelper.WriteHeader(b2b, new[]
            {
            "GSTIN of Receiver","Receiver Name","Invoice No.","Invoice Date",
            "Invoice Value","Place of Supply","Inter-State?",
            "Taxable Value","IGST","CGST","SGST","Cess"
        });

            // B2C sheet
            var b2c = pkg.Workbook.Worksheets.Add("B2C Invoices");
            ExcelHelper.WriteHeader(b2c, new[]
            {
            "Customer Name","Invoice No.","Invoice Date",
            "Invoice Value","Place of Supply",
            "Taxable Value","IGST","CGST","SGST","Cess"
        });

            var b2bRow = 2;
            var b2cRow = 2;

            foreach (var inv in invoices)
            {
                if (!string.IsNullOrEmpty(inv.Customer?.GSTIN))
                {
                    b2b.Cells[b2bRow, 1].Value = inv.Customer.GSTIN;
                    b2b.Cells[b2bRow, 2].Value = inv.Customer.Name;
                    b2b.Cells[b2bRow, 3].Value = inv.InvoiceNumber;
                    b2b.Cells[b2bRow, 4].Value = inv.InvoiceDate.ToString("dd/MM/yyyy");
                    b2b.Cells[b2bRow, 5].Value = inv.GrandTotal;
                    b2b.Cells[b2bRow, 6].Value = inv.PlaceOfSupply ?? "";
                    b2b.Cells[b2bRow, 7].Value = inv.IsInterState ? "Y" : "N";
                    b2b.Cells[b2bRow, 8].Value = inv.TaxableAmount;
                    b2b.Cells[b2bRow, 9].Value = inv.IGSTAmount;
                    b2b.Cells[b2bRow, 10].Value = inv.CGSTAmount;
                    b2b.Cells[b2bRow, 11].Value = inv.SGSTAmount;
                    b2b.Cells[b2bRow, 12].Value = inv.CessAmount;
                    for (var col = 5; col <= 12; col++)
                        b2b.Cells[b2bRow, col].Style.Numberformat.Format = "0.00";
                    b2bRow++;
                }
                else
                {
                    b2c.Cells[b2cRow, 1].Value = inv.Customer?.Name ?? "";
                    b2c.Cells[b2cRow, 2].Value = inv.InvoiceNumber;
                    b2c.Cells[b2cRow, 3].Value = inv.InvoiceDate.ToString("dd/MM/yyyy");
                    b2c.Cells[b2cRow, 4].Value = inv.GrandTotal;
                    b2c.Cells[b2cRow, 5].Value = inv.PlaceOfSupply ?? "";
                    b2c.Cells[b2cRow, 6].Value = inv.TaxableAmount;
                    b2c.Cells[b2cRow, 7].Value = inv.IGSTAmount;
                    b2c.Cells[b2cRow, 8].Value = inv.CGSTAmount;
                    b2c.Cells[b2cRow, 9].Value = inv.SGSTAmount;
                    b2c.Cells[b2cRow, 10].Value = inv.CessAmount;
                    for (var col = 4; col <= 10; col++)
                        b2c.Cells[b2cRow, col].Style.Numberformat.Format = "0.00";
                    b2cRow++;
                }
            }

            ExcelHelper.AutoFit(b2b);
            ExcelHelper.AutoFit(b2c);

            _log.LogInformation("GSTR-1 exported for {Month}/{Year}, tenant {TenantId}",
                month, year, tenantId);

            return ApiResponse<byte[]>.Ok(pkg.GetAsByteArray());
        }

        // ── Local PO mapper (avoids circular dependency) ──────────────────
        private static PurchaseOrderDto MapPO(PurchaseOrder p) => new(
            p.Id, p.PONumber, p.PODate, p.ExpectedDeliveryDate, p.ReceivedDate,
            p.Status, p.SupplierId, p.Supplier?.Name ?? "",
            p.Supplier?.GSTIN, p.SupplierInvoiceNumber, p.SupplierInvoiceDate,
            p.Items.OrderBy(i => i.SortOrder).Select(i => new PurchaseOrderItemDto(
                i.Id, i.SortOrder, i.ProductId, i.Product?.Name ?? "",
                i.HSNCode, i.Description, i.OrderedQty, i.ReceivedQty, i.Unit,
                i.UnitPrice, i.DiscountPercent, i.DiscountAmount, i.TaxableAmount,
                i.GSTRate, i.IGSTAmount, i.CGSTAmount, i.SGSTAmount, i.CessAmount, i.TotalAmount)),
            p.SubTotal, p.DiscountAmount, p.TaxableAmount,
            p.IGSTAmount, p.CGSTAmount, p.SGSTAmount, p.CessAmount, p.TotalTaxAmount,
            p.RoundOff, p.GrandTotal, p.PaidAmount, p.BalanceDue,
            p.IsInterState, p.Notes,
            p.Payments?.Select(InvoiceService.MapPayment) ?? [],
            p.CreatedAt);
    }

}
