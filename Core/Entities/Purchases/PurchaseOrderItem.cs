using Core.Common;
using Core.Enums;

namespace Core.Entities;

public class PurchaseOrderItem : BaseEntity
{
    public int SortOrder { get; set; }
    public string? Description { get; set; }
    public decimal OrderedQty { get; set; }
    public decimal ReceivedQty { get; set; } = 0;
    public UnitOfMeasure Unit { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; } = 0;
    public decimal DiscountAmount { get; set; } = 0;
    public decimal TaxableAmount { get; set; }
    public string? HSNCode { get; set; }
    public decimal GSTRate { get; set; }
    public decimal IGSTAmount { get; set; }
    public decimal CGSTAmount { get; set; }
    public decimal SGSTAmount { get; set; }
    public decimal CessAmount { get; set; }
    public decimal TotalAmount { get; set; }

    // Relations
    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
}
