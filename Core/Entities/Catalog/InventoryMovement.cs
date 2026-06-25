using Core.Common;
using Core.Enums;

namespace Core.Entities;

public class InventoryMovement : BaseEntity
{
    public InventoryMovementType MovementType { get; set; }
    public decimal Quantity { get; set; }          // positive = in, negative = out
    public decimal StockBefore { get; set; }
    public decimal StockAfter { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }

    public string? ReferenceType { get; set; }     // "Invoice" | "PurchaseOrder" | "Adjustment"
    public Guid? ReferenceId { get; set; }
    public string? Notes { get; set; }

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public Guid? PerformedBy { get; set; }
    public User? PerformedByUser { get; set; }
}
