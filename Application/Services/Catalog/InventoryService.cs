 using Application.DTOs;
using Application.Services.System;
using Core.Entities;
using Core.Enums;
using Core.Interfaces.Repositories;
using InvoiceHub.Application.DTOs.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Services.Catalog
{

    public class InventoryService : IInventoryService
    {
        private readonly IInventoryRepository _inventory;
        private readonly IProductRepository _products;
        private readonly IAuditService _audit;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<InventoryService> _log;

        public InventoryService(
            IInventoryRepository inventory, IProductRepository products,
            IAuditService audit, IUnitOfWork uow, ILogger<InventoryService> log)
        {
            _inventory = inventory; _products = products;
            _audit = audit; _uow = uow; _log = log;
        }

        public async Task<ApiResponse<PagedResult<InventoryMovementDto>>> GetMovementsAsync(
            Guid tenantId, Guid? productId, int page, int pageSize, CancellationToken ct = default)
        {
            var query = _inventory.Query(tenantId)
                .Include(m => m.Product)
                .Include(m => m.PerformedByUser)
                .AsQueryable();

            if (productId.HasValue)
                query = query.Where(m => m.ProductId == productId);

            query = query.OrderByDescending(m => m.CreatedAt);
            var total = await query.CountAsync(ct);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new InventoryMovementDto(
                    m.Id, m.MovementType, m.Quantity, m.StockBefore, m.StockAfter,
                    m.UnitCost, m.TotalCost, m.ReferenceType, m.ReferenceId, m.Notes,
                    m.Product.Name, m.CreatedAt,
                    m.PerformedByUser != null
                        ? m.PerformedByUser.FirstName + " " + m.PerformedByUser.LastName
                        : null))
                .ToListAsync(ct);

            return ApiResponse<PagedResult<InventoryMovementDto>>.Ok(
                new PagedResult<InventoryMovementDto>(items, total, page, pageSize));
        }

        public async Task<ApiResponse<bool>> AdjustStockAsync(
            Guid tenantId, Guid productId, Guid userId,
            StockAdjustmentDto dto, CancellationToken ct = default)
        {
            var product = await _products.GetByIdWithTenantAsync(productId, tenantId, ct);
            if (product == null) return ApiResponse<bool>.Fail("Product not found.");

            if (!product.TrackInventory)
                return ApiResponse<bool>.Fail("Inventory tracking is disabled for this product.");

            var newStock = dto.MovementType switch
            {
                InventoryMovementType.Adjustment or
                InventoryMovementType.Return or
                InventoryMovementType.Purchase => product.CurrentStock + dto.Quantity,
                InventoryMovementType.Sale or
                InventoryMovementType.Damaged => product.CurrentStock - dto.Quantity,
                _ => product.CurrentStock + dto.Quantity
            };

            if (newStock < 0)
                return ApiResponse<bool>.Fail(
                    $"Insufficient stock. Available: {product.CurrentStock}");

            var movement = new InventoryMovement
            {
                TenantId = tenantId,
                ProductId = productId,
                MovementType = dto.MovementType,
                Quantity = dto.MovementType is InventoryMovementType.Sale or InventoryMovementType.Damaged
                    ? -dto.Quantity : dto.Quantity,
                StockBefore = product.CurrentStock,
                StockAfter = newStock,
                UnitCost = product.PurchasePrice,
                TotalCost = dto.Quantity * product.PurchasePrice,
                Notes = dto.Notes,
                PerformedBy = userId
            };

            await _inventory.AddAsync(movement, ct);

            // Update product stock directly
            product.CurrentStock = newStock;
            _products.Update(product);

            await _uow.SaveChangesAsync(ct);

            await _audit.LogAsync(tenantId, userId, "InventoryMovement", movement.Id,
                "Adjust", newValues: new { dto.Quantity, dto.MovementType }, ct: ct);

            _log.LogInformation("Stock adjusted for product {ProductId}: {Qty} ({Type})",
                productId, dto.Quantity, dto.MovementType);

            return ApiResponse<bool>.Ok(true, $"Stock adjusted. New stock: {newStock}");
        }

        public async Task<ApiResponse<IEnumerable<ProductListDto>>> GetLowStockProductsAsync(
            Guid tenantId, CancellationToken ct = default)
        {
            var products = await _products.GetLowStockAsync(tenantId, ct);
            return ApiResponse<IEnumerable<ProductListDto>>.Ok(
                products.Select(p => new ProductListDto(
                    p.Id, p.Name, p.SKU, p.HSNCode, p.ProductType,
                    p.SalePrice, p.GSTRate, p.CurrentStock, p.MinimumStock, true, p.IsActive)));
        }

        public async Task<ApiResponse<StockValuationDto>> GetStockValuationAsync(
            Guid tenantId, CancellationToken ct = default)
        {
            var products = (await _products.GetAllAsync(tenantId, ct))
                .Where(p => p.IsActive && p.TrackInventory)
                .ToList();

            var items = products.Select(p => new StockValuationItemDto(
                p.Id, p.Name, p.SKU, p.CurrentStock,
                p.PurchasePrice, p.CurrentStock * p.PurchasePrice,
                p.CurrentStock <= p.MinimumStock));

            return ApiResponse<StockValuationDto>.Ok(new StockValuationDto(
                items.Sum(i => i.StockValue),
                products.Count,
                products.Count(p => p.CurrentStock <= p.MinimumStock && p.CurrentStock > 0),
                products.Count(p => p.CurrentStock == 0),
                items));
        }
    }

}
