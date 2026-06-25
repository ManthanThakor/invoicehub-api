using Core.Common;
using Core.Enums;

namespace Core.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? SKU { get; set; }
    public string? HSNCode { get; set; }   // GST HSN/SAC code
    public string? Barcode { get; set; }
    public ProductType ProductType { get; set; } = ProductType.Goods;
    public UnitOfMeasure Unit { get; set; } = UnitOfMeasure.Pieces;

    // Pricing
    public decimal PurchasePrice { get; set; } = 0;
    public decimal SalePrice { get; set; } = 0;
    public decimal MRP { get; set; } = 0;

    // GST
    public decimal GSTRate { get; set; } = 18;     // e.g. 18 = 18%
    public bool IsInterState { get; set; } = false; // Determines IGST vs CGST+SGST
    public decimal? CessRate { get; set; }

    // Inventory
    public bool TrackInventory { get; set; } = true;
    public decimal CurrentStock { get; set; } = 0;
    public decimal MinimumStock { get; set; } = 0;  // Reorder alert threshold
    public decimal ReorderQty { get; set; } = 0;
    public string? StorageLocation { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;

    // Relations

    public Tenant Tenant { get; set; } = null!;
    public Guid? CategoryId { get; set; }
    public ProductCategory? Category { get; set; }
    public ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();
    public ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();
    public ICollection<InventoryMovement> InventoryMovements { get; set; } = new List<InventoryMovement>();
}
