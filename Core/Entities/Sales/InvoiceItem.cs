using Core.Common;
using Core.Enums;

namespace Core.Entities;

public class InvoiceItem : BaseEntity
{
    public int SortOrder { get; set; }
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public UnitOfMeasure Unit { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; } = 0;
    public decimal DiscountAmount { get; set; } = 0;
    public decimal TaxableAmount { get; set; }  // Qty × Price − Discount

    // Per-line GST breakdown
    public string? HSNCode { get; set; }
    public decimal GSTRate { get; set; }
    public decimal IGSTRate { get; set; }
    public decimal IGSTAmount { get; set; }
    public decimal CGSTRate { get; set; }
    public decimal CGSTAmount { get; set; }
    public decimal SGSTRate { get; set; }
    public decimal SGSTAmount { get; set; }
    public decimal CessRate { get; set; }
    public decimal CessAmount { get; set; }
    public decimal TotalAmount { get; set; }    // TaxableAmount + all taxes

    // Relations
    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
}
