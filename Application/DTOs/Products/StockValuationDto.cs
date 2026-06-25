using Core.Enums;

namespace InvoiceHub.Application.DTOs.Products;

/// <summary>
/// DTO for inventory movement records
/// </summary>
public record InventoryMovementDto(
    Guid Id,
        InventoryMovementType MovementType,
    decimal Quantity,
    decimal StockBefore,
    decimal StockAfter,
    decimal UnitCost,
    decimal TotalCost,
    string? ReferenceType,
    Guid? ReferenceId,
    string? Notes,
    string? ProductName,
    DateTime CreatedAt,
    string? PerformedByName
);

/// <summary>
/// DTO for stock valuation summary
/// </summary>
public record StockValuationDto(
    decimal TotalStockValue,
    int TotalProducts,
    int LowStockCount,
    int OutOfStockCount,
    IEnumerable<StockValuationItemDto> Items
);

/// <summary>
/// DTO for individual stock valuation item
/// </summary>
public record StockValuationItemDto(
    Guid ProductId,
    string Name,
    string? SKU,
    decimal CurrentStock,
    decimal PurchasePrice,
    decimal StockValue,
    bool IsLowStock
);
