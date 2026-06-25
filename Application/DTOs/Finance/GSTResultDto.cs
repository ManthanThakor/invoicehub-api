namespace InvoiceHub.Application.DTOs.Finance;

/// <summary>
/// DTO for GST line-level calculation results
/// </summary>
public record GSTLineResult(
    decimal TaxableAmount,
    decimal IGSTAmount,
    decimal CGSTAmount,
    decimal SGSTAmount,
    decimal CessAmount,
    decimal TotalAmount,
    decimal DiscountAmount,
    decimal IGSTRate,
    decimal CGSTRate,
    decimal SGSTRate
);

/// <summary>
/// DTO for GST totals across invoice/document
/// </summary>
public record GSTTotals(
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxableAmount,
    decimal IGSTAmount,
    decimal CGSTAmount,
    decimal SGSTAmount,
    decimal CessAmount,
    decimal TotalTaxAmount,
    decimal RoundOff,
    decimal GrandTotal
);
