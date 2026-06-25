using Core.Enums;

namespace Application.DTOs;

public record ProductListDto(
    Guid Id,
    string Name,
    string? SKU,
    string? HSNCode,
    ProductType ProductType,
    decimal SalePrice,
    decimal GSTRate,
    decimal CurrentStock,
    decimal MinimumStock,
    bool IsLowStock,
    bool IsActive
);

public record ProductDto(
    Guid Id,
    string Name,
    string? Description,
    string? SKU,
    string? HSNCode,
    string? Barcode,
    ProductType ProductType,
    UnitOfMeasure Unit,
    decimal PurchasePrice,
    decimal SalePrice,
    decimal MRP,
    decimal GSTRate,
    decimal? CessRate,
    bool TrackInventory,
    decimal CurrentStock,
    decimal MinimumStock,
    decimal ReorderQty,
    string? StorageLocation,
    string? ImageUrl,
    bool IsActive,
    Guid? CategoryId,
    string? CategoryName,
    DateTime CreatedAt
);

public record CreateProductDto(
    string Name,
    ProductType ProductType,
    UnitOfMeasure Unit,
    decimal PurchasePrice,
    decimal SalePrice,
    decimal MRP,
    decimal GSTRate,
    bool TrackInventory,
    decimal OpeningStock,
    decimal MinimumStock,
    decimal ReorderQty,
    string? Description = null,
    string? SKU = null,
    string? HSNCode = null,
    string? Barcode = null,
    decimal? CessRate = null,
    string? StorageLocation = null,
    Guid? CategoryId = null
);

public record UpdateProductDto(
    string Name,
    ProductType ProductType,
    UnitOfMeasure Unit,
    decimal PurchasePrice,
    decimal SalePrice,
    decimal MRP,
    decimal GSTRate,
    bool TrackInventory,
    decimal MinimumStock,
    decimal ReorderQty,
    bool IsActive,
    string? Description = null,
    string? SKU = null,
    string? HSNCode = null,
    string? Barcode = null,
    decimal? CessRate = null,
    string? StorageLocation = null,
    Guid? CategoryId = null
);

public record ProductFilterDto(
    int Page = 1,
    int PageSize = 20,
    string SortBy = "Name",
    bool SortDesc = false,
    string? Search = null,
    ProductType? Type = null,
    Guid? CategoryId = null,
    bool? IsActive = null,
    bool? LowStockOnly = null
);

public record StockAdjustmentDto(
    decimal Quantity,
    InventoryMovementType MovementType,
    string? Notes = null
);
