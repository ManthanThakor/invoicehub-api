using Core.Common;
using Core.Enums;

namespace Core.Entities;

public class PurchaseOrder : BaseEntity
{
    public string PONumber { get; set; } = null!;
    public DateTime PODate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public DateTime? ReceivedDate { get; set; }
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

    public string? SupplierInvoiceNumber { get; set; }
    public DateTime? SupplierInvoiceDate { get; set; }

    // Amounts
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; } = 0;
    public decimal TaxableAmount { get; set; }
    public decimal IGSTAmount { get; set; } = 0;
    public decimal CGSTAmount { get; set; } = 0;
    public decimal SGSTAmount { get; set; } = 0;
    public decimal CessAmount { get; set; } = 0;
    public decimal TotalTaxAmount { get; set; }
    public decimal RoundOff { get; set; } = 0;
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; } = 0;
    public decimal BalanceDue { get; set; }

    public bool IsInterState { get; set; } = false;
    public string? Notes { get; set; }

    // Relations

    public Tenant Tenant { get; set; } = null!;
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
