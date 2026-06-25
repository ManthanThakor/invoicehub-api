using Core.Enums;

namespace Application.DTOs;

public record CustomerListDto(
    Guid Id,
    string Name,
    string? Email,
    string? Phone,
    CustomerType CustomerType,
    CustomerStatus Status,
    string? GSTIN,
    string? BillingCity,
    string? BillingState,
    decimal OutstandingBalance,
    DateTime CreatedAt
);

public record CustomerDto(
    Guid Id,
    string Name,
    string? ContactPerson,
    string? Email,
    string? Phone,
    string? AlternatePhone,
    CustomerType CustomerType,
    CustomerStatus Status,
    string? GSTIN,
    string? PAN,
    bool IsGSTRegistered,
    AddressDto BillingAddress,
    AddressDto? ShippingAddress,
    bool ShippingSameAsBilling,
    decimal CreditLimit,
    int? PaymentTermDays,
    string? Notes,
    string? Tags,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateCustomerDto(
    string Name,
    CustomerType CustomerType,
    AddressDto BillingAddress,
    bool ShippingSameAsBilling,
    decimal CreditLimit,
    string? ContactPerson = null,
    string? Email = null,
    string? Phone = null,
    string? AlternatePhone = null,
    string? GSTIN = null,
    string? PAN = null,
    AddressDto? ShippingAddress = null,
    int? PaymentTermDays = null,
    string? Notes = null,
    string? Tags = null
);

public record UpdateCustomerDto(
    string Name,
    CustomerType CustomerType,
    CustomerStatus Status,
    AddressDto BillingAddress,
    bool ShippingSameAsBilling,
    decimal CreditLimit,
    string? ContactPerson = null,
    string? Email = null,
    string? Phone = null,
    string? AlternatePhone = null,
    string? GSTIN = null,
    string? PAN = null,
    AddressDto? ShippingAddress = null,
    int? PaymentTermDays = null,
    string? Notes = null,
    string? Tags = null
);

public record CustomerFilterDto(
    int Page = 1,
    int PageSize = 20,
    string SortBy = "Name",
    bool SortDesc = false,
    string? Search = null,
    CustomerType? Type = null,
    CustomerStatus? Status = null
);

public record CustomerStatisticsDto(
    Guid CustomerId,
    string Name,
    int TotalInvoices,
    decimal TotalRevenue,
    decimal OutstandingBalance,
    decimal AverageInvoiceValue,
    DateTime? LastInvoiceDate,
    int TotalDaysOverdue
);
