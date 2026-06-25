using Core.Common;
using Core.Enums;

namespace Core.Entities;

public class Supplier : BaseEntity
{
    public string Name { get; set; } = null!;
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? GSTIN { get; set; }
    public string? PAN { get; set; }
    public SupplierStatus Status { get; set; } = SupplierStatus.Active;

    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? StateCode { get; set; }
    public string? PinCode { get; set; }
    public string? Country { get; set; } = "India";

    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankIFSC { get; set; }

    public int? PaymentTermDays { get; set; } = 30;
    public string? Notes { get; set; }

    // Relations

    public Tenant Tenant { get; set; } = null!;
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}
