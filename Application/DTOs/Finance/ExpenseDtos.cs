using Core.Enums;

namespace Application.DTOs;

public record ExpenseListDto(
    Guid Id,
    string Title,
    ExpenseCategory Category,
    DateTime ExpenseDate,
    decimal TotalAmount,
    PaymentMethod PaymentMethod,
    string? VendorName,
    string? ReferenceNumber
);

public record ExpenseDto(
    Guid Id,
    string Title,
    ExpenseCategory Category,
    DateTime ExpenseDate,
    decimal Amount,
    decimal? GSTAmount,
    decimal TotalAmount,
    PaymentMethod PaymentMethod,
    string? VendorName,
    string? ReferenceNumber,
    string? Notes,
    string? ReceiptUrl,
    DateTime CreatedAt
);

public record CreateExpenseDto(
    string Title,
    ExpenseCategory Category,
    DateTime ExpenseDate,
    decimal Amount,
    PaymentMethod PaymentMethod,
    decimal? GSTAmount = null,
    string? VendorName = null,
    string? ReferenceNumber = null,
    string? Notes = null
);

public record UpdateExpenseDto(
    string Title,
    ExpenseCategory Category,
    DateTime ExpenseDate,
    decimal Amount,
    PaymentMethod PaymentMethod,
    decimal? GSTAmount = null,
    string? VendorName = null,
    string? ReferenceNumber = null,
    string? Notes = null
);

public record ExpenseFilterDto(
    int Page = 1,
    int PageSize = 20,
    string SortBy = "ExpenseDate",
    bool SortDesc = true,
    string? Search = null,
    ExpenseCategory? Category = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null
);
