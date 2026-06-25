using Core.Enums;

namespace Application.DTOs;

public record PaymentDto(
    Guid Id,
    string PaymentNumber,
    DateTime PaymentDate,
    decimal Amount,
    PaymentMethod Method,
    PaymentStatus Status,
    Guid? InvoiceId,
    string? InvoiceNumber,
    Guid? CustomerId,
    string? CustomerName,
    string? ReferenceNumber,
    string? BankName,
    string? Notes,
    bool IsRefund,
    DateTime CreatedAt
);

public record PaymentListDto(
    Guid Id,
    string PaymentNumber,
    DateTime PaymentDate,
    decimal Amount,
    PaymentMethod Method,
    PaymentStatus Status,
    string? CustomerName,
    string? InvoiceNumber,
    bool IsRefund
);

public record RecordPaymentDto(
    decimal Amount,
    DateTime PaymentDate,
    PaymentMethod Method,
    Guid? InvoiceId = null,
    Guid? PurchaseOrderId = null,
    string? ReferenceNumber = null,
    string? BankName = null,
    string? Notes = null
);

public record PaymentFilterDto(
    int Page = 1,
    int PageSize = 20,
    Guid? InvoiceId = null,
    Guid? CustomerId = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    PaymentMethod? Method = null,
    PaymentStatus? Status = null
);
