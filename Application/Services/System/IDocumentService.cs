using Application.DTOs;

namespace Application.Services.System
{
    public interface IDocumentService
    {
        Task<ApiResponse<byte[]>> GetInvoicePdfAsync(Guid tenantId, Guid invoiceId, CancellationToken ct = default);
        Task<ApiResponse<byte[]>> GetPurchaseOrderPdfAsync(Guid tenantId, Guid poId, CancellationToken ct = default);
        Task<ApiResponse<byte[]>> GetPaymentReceiptPdfAsync(Guid tenantId, Guid paymentId, CancellationToken ct = default);
        Task<ApiResponse<byte[]>> ExportInvoicesExcelAsync(Guid tenantId, InvoiceFilterDto filter, CancellationToken ct = default);
        Task<ApiResponse<byte[]>> ExportExpensesExcelAsync(Guid tenantId, ExpenseFilterDto filter, CancellationToken ct = default);
        Task<ApiResponse<byte[]>> ExportCustomersExcelAsync(Guid tenantId, CancellationToken ct = default);
        Task<ApiResponse<byte[]>> ExportProductsExcelAsync(Guid tenantId, CancellationToken ct = default);
        Task<ApiResponse<byte[]>> ExportGSTR1Async(Guid tenantId, int month, int year, CancellationToken ct = default);
    }
}