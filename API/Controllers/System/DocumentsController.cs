using Application.DTOs;
using Application.Services.System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InvoiceHub.API.Controllers.System;

[ApiController]
[Route("api/documents")]
[Authorize]
[Tags("Documents & Exports")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documents;

    public DocumentsController(IDocumentService documents) => _documents = documents;

    private Guid TenantId => Guid.Parse(User.FindFirstValue("tenantId")!);

    // -- PDF Downloads -----------------------------------------------------

    /// <summary>Download invoice PDF.</summary>
    [HttpGet("invoices/{invoiceId:guid}/pdf")]
    [Authorize(Policy = "AllRoles")]
    public async Task<IActionResult> GetInvoicePdf(Guid invoiceId, CancellationToken ct)
    {
        var result = await _documents.GetInvoicePdfAsync(TenantId, invoiceId, ct);
        if (!result.Success) return NotFound(result);
        return File(result.Data!, "application/pdf", $"Invoice-{invoiceId}.pdf");
    }

    /// <summary>Download purchase order PDF.</summary>
    [HttpGet("purchase-orders/{poId:guid}/pdf")]
    [Authorize(Policy = "AccountantUp")]
    public async Task<IActionResult> GetPurchaseOrderPdf(Guid poId, CancellationToken ct)
    {
        var result = await _documents.GetPurchaseOrderPdfAsync(TenantId, poId, ct);
        if (!result.Success) return NotFound(result);
        return File(result.Data!, "application/pdf", $"PO-{poId}.pdf");
    }

    /// <summary>Download payment receipt PDF.</summary>
    [HttpGet("payments/{paymentId:guid}/pdf")]
    [Authorize(Policy = "AccountantUp")]
    public async Task<IActionResult> GetPaymentReceiptPdf(Guid paymentId, CancellationToken ct)
    {
        var result = await _documents.GetPaymentReceiptPdfAsync(TenantId, paymentId, ct);
        if (!result.Success) return NotFound(result);
        return File(result.Data!, "application/pdf", $"Receipt-{paymentId}.pdf");
    }

    // -- Excel Exports -----------------------------------------------------

    /// <summary>Export invoices to Excel (.xlsx) with GST breakdown columns.</summary>
    [HttpGet("invoices/export")]
    [Authorize(Policy = "AccountantUp")]
    public async Task<IActionResult> ExportInvoices(
        [FromQuery] InvoiceFilterDto filter, CancellationToken ct)
    {
        var result = await _documents.ExportInvoicesExcelAsync(TenantId, filter, ct);
        if (!result.Success) return BadRequest(result);
        return File(result.Data!, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Invoices-{DateTime.Today:yyyyMMdd}.xlsx");
    }

    /// <summary>Export expenses to Excel with category summary sheet.</summary>
    [HttpGet("expenses/export")]
    [Authorize(Policy = "AccountantUp")]
    public async Task<IActionResult> ExportExpenses(
        [FromQuery] ExpenseFilterDto filter, CancellationToken ct)
    {
        var result = await _documents.ExportExpensesExcelAsync(TenantId, filter, ct);
        if (!result.Success) return BadRequest(result);
        return File(result.Data!, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Expenses-{DateTime.Today:yyyyMMdd}.xlsx");
    }

    /// <summary>Export all customers to Excel.</summary>
    [HttpGet("customers/export")]
    [Authorize(Policy = "ManagerUp")]
    public async Task<IActionResult> ExportCustomers(CancellationToken ct)
    {
        var result = await _documents.ExportCustomersExcelAsync(TenantId, ct);
        if (!result.Success) return BadRequest(result);
        return File(result.Data!, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Customers-{DateTime.Today:yyyyMMdd}.xlsx");
    }

    /// <summary>Export all products with stock values to Excel. Low-stock rows highlighted in red.</summary>
    [HttpGet("products/export")]
    [Authorize(Policy = "ManagerUp")]
    public async Task<IActionResult> ExportProducts(CancellationToken ct)
    {
        var result = await _documents.ExportProductsExcelAsync(TenantId, ct);
        if (!result.Success) return BadRequest(result);
        return File(result.Data!, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Products-{DateTime.Today:yyyyMMdd}.xlsx");
    }

    /// <summary>Export GSTR-1 (B2B + B2C sheets) for a specific month/year.</summary>
    [HttpGet("gstr1/export")]
    [Authorize(Policy = "AccountantUp")]
    public async Task<IActionResult> ExportGSTR1(
        [FromQuery] int month, [FromQuery] int year, CancellationToken ct)
    {
        var result = await _documents.ExportGSTR1Async(TenantId, month, year, ct);
        if (!result.Success) return BadRequest(result);
        return File(result.Data!, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"GSTR1-{year}-{month:D2}.xlsx");
    }
}
