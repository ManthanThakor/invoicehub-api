using Core.Enums;

namespace Application.DTOs;

public record PurchaseOrderListDto(
    Guid Id,
    string PONumber,
    DateTime PODate,
    string SupplierName,
    decimal GrandTotal,
    decimal PaidAmount,
    decimal BalanceDue,
    PurchaseOrderStatus Status,
    DateTime? ExpectedDeliveryDate
);

public record PurchaseOrderDto(
    Guid Id,
    string PONumber,
    DateTime PODate,
    DateTime? ExpectedDeliveryDate,
    DateTime? ReceivedDate,
    PurchaseOrderStatus Status,
    Guid SupplierId,
    string SupplierName,
    string? SupplierGSTIN,
    string? SupplierInvoiceNumber,
    DateTime? SupplierInvoiceDate,
    IEnumerable<PurchaseOrderItemDto> Items,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxableAmount,
    decimal IGSTAmount,
    decimal CGSTAmount,
    decimal SGSTAmount,
    decimal CessAmount,
    decimal TotalTaxAmount,
    decimal RoundOff,
    decimal GrandTotal,
    decimal PaidAmount,
    decimal BalanceDue,
    bool IsInterState,
    string? Notes,
    IEnumerable<PaymentDto> Payments,
    DateTime CreatedAt
);

public record PurchaseOrderItemDto(
    Guid? Id,
    int SortOrder,
    Guid ProductId,
    string ProductName,
    string? HSNCode,
    string? Description,
    decimal OrderedQty,
    decimal ReceivedQty,
    UnitOfMeasure Unit,
    decimal UnitPrice,
    decimal DiscountPercent,
    decimal DiscountAmount,
    decimal TaxableAmount,
    decimal GSTRate,
    decimal IGSTAmount,
    decimal CGSTAmount,
    decimal SGSTAmount,
    decimal CessAmount,
    decimal TotalAmount
);

public record CreatePurchaseOrderDto(
    DateTime PODate,
    Guid SupplierId,
    bool IsInterState,
    IEnumerable<CreatePurchaseOrderItemDto> Items,
    bool SaveAsDraft = false,
    DateTime? ExpectedDeliveryDate = null,
    string? SupplierInvoiceNumber = null,
    DateTime? SupplierInvoiceDate = null,
    string? Notes = null
);

public record CreatePurchaseOrderItemDto(
    Guid ProductId,
    decimal OrderedQty,
    decimal UnitPrice,
    decimal DiscountPercent,
    string? Description = null
);

public record UpdatePurchaseOrderDto(
    DateTime PODate,
    bool IsInterState,
    IEnumerable<CreatePurchaseOrderItemDto> Items,
    DateTime? ExpectedDeliveryDate = null,
    string? SupplierInvoiceNumber = null,
    DateTime? SupplierInvoiceDate = null,
    string? Notes = null
);

public record PurchaseOrderFilterDto(
    int Page = 1,
    int PageSize = 20,
    string SortBy = "PODate",
    bool SortDesc = true,
    string? Search = null,
    PurchaseOrderStatus? Status = null,
    Guid? SupplierId = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null
);
