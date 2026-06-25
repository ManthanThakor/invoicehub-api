namespace Application.DTOs;

public record CreditNoteDto(
    Guid Id,
    string CreditNoteNumber,
    DateTime CreditNoteDate,
    string? Reason,
    decimal Amount,
    decimal TaxAmount,
    decimal TotalAmount,
    bool IsApplied,
    Guid InvoiceId,
    string InvoiceNumber,
    Guid CustomerId,
    string CustomerName,
    DateTime CreatedAt
);

public record CreateCreditNoteDto(
    Guid InvoiceId,
    DateTime CreditNoteDate,
    decimal Amount,
    decimal TaxAmount,
    decimal TotalAmount,
    string? Reason = null
);
