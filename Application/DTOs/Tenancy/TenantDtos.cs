namespace Application.DTOs;

public record TenantDto(
    Guid Id,
    string BusinessName,
    string? BusinessLogo,
    string? LegalName,
    string? GSTIN,
    string? PAN,
    string? TAN,
    string? CIN,
    string? Email,
    string? Phone,
    string? Website,
    AddressDto Address,
    string CurrencyCode,
    string? InvoicePrefix,
    string? PurchasePrefix,
    string? FinancialYearStart,
    bool IsGSTRegistered,
    string? BankName,
    string? BankAccountNumber,
    string? BankIFSC,
    string? BankBranch,
    string? UPIId
);

public record UpdateTenantDto(
    string BusinessName,
    AddressDto Address,
    bool IsGSTRegistered,
    string? LegalName = null,
    string? GSTIN = null,
    string? PAN = null,
    string? TAN = null,
    string? CIN = null,
    string? Email = null,
    string? Phone = null,
    string? Website = null,
    string? InvoicePrefix = null,
    string? PurchasePrefix = null,
    string? FinancialYearStart = null,
    string? BankName = null,
    string? BankAccountNumber = null,
    string? BankIFSC = null,
    string? BankBranch = null,
    string? UPIId = null
);
