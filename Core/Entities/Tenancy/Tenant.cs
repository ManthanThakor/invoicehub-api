using Core.Common;
using Core.Enums;

namespace Core.Entities;

public class Tenant : BaseEntity
{
    public string BusinessName { get; set; } = null!;
    public string? BusinessLogo { get; set; }
    public string? LegalName { get; set; }
    public string? GSTIN { get; set; }
    public string? PAN { get; set; }
    public string? TAN { get; set; }
    public string? CIN { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Website { get; set; }

    // Address
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? StateCode { get; set; }  // GST state code (e.g. 24 for Gujarat)
    public string? Country { get; set; } = "India";
    public string? PinCode { get; set; }

    // Business Settings
    public string CurrencyCode { get; set; } = "INR";
    public string? InvoicePrefix { get; set; } = "INV";
    public int InvoiceCounter { get; set; } = 1;
    public string? PurchasePrefix { get; set; } = "PO";
    public int PurchaseCounter { get; set; } = 1;
    public string? FinancialYearStart { get; set; } = "04-01"; // MM-DD
    public bool IsGSTRegistered { get; set; } = true;
    public bool IsActive { get; set; } = true;

    // Bank Details
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankIFSC { get; set; }
    public string? BankBranch { get; set; }
    public string? UPIId { get; set; }

    // Relations
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
    public ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
