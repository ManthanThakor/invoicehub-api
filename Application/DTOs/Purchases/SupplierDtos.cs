using Core.Enums;

namespace Application.DTOs;

public record SupplierListDto(
    Guid Id,
    string Name,
    string? Email,
    string? Phone,
    SupplierStatus Status,
    string? GSTIN,
    string? City
);

public record SupplierDto(
    Guid Id,
    string Name,
    string? ContactPerson,
    string? Email,
    string? Phone,
    string? GSTIN,
    string? PAN,
    SupplierStatus Status,
    AddressDto Address,
    string? BankName,
    string? BankAccountNumber,
    string? BankIFSC,
    int? PaymentTermDays,
    string? Notes,
    DateTime CreatedAt
);

public record CreateSupplierDto(
    string Name,
    AddressDto Address,
    string? ContactPerson = null,
    string? Email = null,
    string? Phone = null,
    string? GSTIN = null,
    string? PAN = null,
    string? BankName = null,
    string? BankAccountNumber = null,
    string? BankIFSC = null,
    int? PaymentTermDays = null,
    string? Notes = null
);

public record UpdateSupplierDto(
    string Name,
    SupplierStatus Status,
    AddressDto Address,
    string? ContactPerson = null,
    string? Email = null,
    string? Phone = null,
    string? GSTIN = null,
    string? PAN = null,
    string? BankName = null,
    string? BankAccountNumber = null,
    string? BankIFSC = null,
    int? PaymentTermDays = null,
    string? Notes = null
);

public record SupplierFilterDto(
    int Page = 1,
    int PageSize = 20,
    string SortBy = "Name",
    bool SortDesc = false,
    string? Search = null,
    SupplierStatus? Status = null
);
