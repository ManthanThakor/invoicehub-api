using Core.Enums;

namespace Application.DTOs;

public record InvoiceListDto(
    Guid Id,
    string InvoiceNumber,
    DateTime InvoiceDate,
    DateTime? DueDate,
    Guid CustomerId,
    string CustomerName,
    decimal GrandTotal,
    decimal PaidAmount,
    decimal BalanceDue,
    InvoiceStatus Status,
    bool IsOverdue
);

public record InvoiceDto(
    Guid Id,
    string InvoiceNumber,
    DateTime InvoiceDate,
    DateTime? DueDate,
    InvoiceStatus Status,

    // Customer
    Guid CustomerId,
    string CustomerName,
    string? CustomerGSTIN,
    AddressDto? BillingAddress,
    AddressDto? ShippingAddress,

    // GST
    bool IsInterState,
    string? PlaceOfSupply,
    string? PlaceOfSupplyCode,

    // Line Items
    IEnumerable<InvoiceItemDto> Items,

    // Amounts
    decimal SubTotal,
    decimal DiscountAmount,
    DiscountType DiscountType,
    decimal? DiscountPercent,
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

    // Meta
    string? Notes,
    string? TermsAndConditions,
    string? EWayBillNumber,
    string? IRN,
    IEnumerable<PaymentDto> Payments,
    DateTime CreatedAt
);

public record InvoiceItemDto(
    Guid? Id,
    int SortOrder,
    Guid ProductId,
    string ProductName,
    string? HSNCode,
    string? Description,
    decimal Quantity,
    UnitOfMeasure Unit,
    decimal UnitPrice,
    decimal DiscountPercent,
    decimal DiscountAmount,
    decimal TaxableAmount,
    decimal GSTRate,
    decimal IGSTRate, decimal IGSTAmount,
    decimal CGSTRate, decimal CGSTAmount,
    decimal SGSTRate, decimal SGSTAmount,
    decimal CessRate, decimal CessAmount,
    decimal TotalAmount
);

public record CreateInvoiceDto(
    DateTime InvoiceDate,
    Guid CustomerId,
    bool IsInterState,
    IEnumerable<CreateInvoiceItemDto> Items,
    DiscountType DiscountType,
    DateTime? DueDate = null,
    string? PlaceOfSupply = null,
    string? PlaceOfSupplyCode = null,
    decimal? DiscountPercent = null,
    decimal? DiscountAmount = null,
    string? Notes = null,
    string? TermsAndConditions = null,
    string? ShippingDetails = null,
    string? VehicleNumber = null,
    string? EWayBillNumber = null,
    bool SaveAsDraft = false
);

public record CreateInvoiceItemDto(
    Guid ProductId,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountPercent,
    string? Description = null
);

public record UpdateInvoiceDto(
    DateTime InvoiceDate,
    bool IsInterState,
    IEnumerable<CreateInvoiceItemDto> Items,
    DiscountType DiscountType,
    DateTime? DueDate = null,
    string? PlaceOfSupply = null,
    decimal? DiscountPercent = null,
    decimal? DiscountAmount = null,
    string? Notes = null,
    string? TermsAndConditions = null,
    string? EWayBillNumber = null
);

public record InvoiceFilterDto(
    int Page = 1,
    int PageSize = 20,
    string SortBy = "InvoiceDate",
    bool SortDesc = true,
    string? Search = null,
    InvoiceStatus? Status = null,
    Guid? CustomerId = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    bool? OverdueOnly = null
);

public record GSTSummaryDto(
    int Month,
    int Year,
    decimal TaxableAmount,
    decimal TotalIGST,
    decimal TotalCGST,
    decimal TotalSGST,
    decimal TotalCess,
    decimal TotalTax,
    IEnumerable<GSTHSNSummaryDto> HSNSummary,
    IEnumerable<GSTStatewiseSummaryDto> StatewiseSummary
);

public record GSTHSNSummaryDto(string HSNCode, decimal TaxableAmount, decimal IGSTAmount, decimal CGSTAmount, decimal SGSTAmount, decimal TotalTax);
public record GSTStatewiseSummaryDto(string State, string StateCode, decimal TaxableAmount, decimal IGSTAmount, decimal CGSTAmount, decimal SGSTAmount, int InvoiceCount);
