using Application.DTOs;
using Application.Services.System;
using Core.Entities;
using Core.Enums;
using Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace Application.Services.Catalog
{

    public class ProductService : IProductService
    {
        private readonly IProductRepository _products;
        private readonly IInventoryRepository _inventory;
        private readonly IFileService _files;
        private readonly IAuditService _audit;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<ProductService> _log;

        public ProductService(
            IProductRepository products, IInventoryRepository inventory,
            IFileService files, IAuditService audit,
            IUnitOfWork uow, ILogger<ProductService> log)
        {
            _products = products; _inventory = inventory;
            _files = files; _audit = audit; _uow = uow; _log = log;
        }

        public async Task<ApiResponse<PagedResult<ProductListDto>>> GetListAsync(
            Guid tenantId, ProductFilterDto filter, CancellationToken ct = default)
        {
            var query = _products.Query(tenantId)
                .Include(p => p.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Search))
                query = query.Where(p =>
                    p.Name.Contains(filter.Search) ||
                    (p.SKU != null && p.SKU.Contains(filter.Search)) ||
                    (p.HSNCode != null && p.HSNCode.Contains(filter.Search)));

            if (filter.Type.HasValue) query = query.Where(p => p.ProductType == filter.Type);
            if (filter.CategoryId.HasValue) query = query.Where(p => p.CategoryId == filter.CategoryId);
            if (filter.IsActive.HasValue) query = query.Where(p => p.IsActive == filter.IsActive);
            if (filter.LowStockOnly == true)
                query = query.Where(p => p.TrackInventory && p.CurrentStock <= p.MinimumStock);

            query = filter.SortBy switch
            {
                "SalePrice" => filter.SortDesc
                    ? query.OrderByDescending(p => p.SalePrice)
                    : query.OrderBy(p => p.SalePrice),
                "CurrentStock" => filter.SortDesc
                    ? query.OrderByDescending(p => p.CurrentStock)
                    : query.OrderBy(p => p.CurrentStock),
                _ => filter.SortDesc
                    ? query.OrderByDescending(p => p.Name)
                    : query.OrderBy(p => p.Name)
            };

            var total = await query.CountAsync(ct);
            var items = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(p => new ProductListDto(
                    p.Id, p.Name, p.SKU, p.HSNCode, p.ProductType,
                    p.SalePrice, p.GSTRate, p.CurrentStock, p.MinimumStock,
                    p.TrackInventory && p.CurrentStock <= p.MinimumStock,
                    p.IsActive))
                .ToListAsync(ct);

            return ApiResponse<PagedResult<ProductListDto>>.Ok(
                new PagedResult<ProductListDto>(items, total, filter.Page, filter.PageSize));
        }

        public async Task<ApiResponse<ProductDto>> GetAsync(
            Guid tenantId, Guid id, CancellationToken ct = default)
        {
            var product = await _products.Query(tenantId)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id, ct);

            if (product == null) return ApiResponse<ProductDto>.Fail("Product not found.");
            return ApiResponse<ProductDto>.Ok(MapProduct(product));
        }

        public async Task<ApiResponse<ProductDto>> CreateAsync(
     Guid tenantId, Guid userId, CreateProductDto dto, CancellationToken ct = default)
        {
            if (!string.IsNullOrEmpty(dto.SKU))
            {
                var isUnique = await _products.IsSKUUniqueAsync(tenantId, dto.SKU, ct: ct);
                if (!isUnique)
                    return ApiResponse<ProductDto>.Fail($"SKU '{dto.SKU}' already exists.");
            }

            Product? product = null;

            try
            {
                await _uow.ExecuteInTransactionAsync(async () =>
                {
                    product = new Product
                    {
                        TenantId = tenantId,
                        Name = dto.Name?.Trim() ?? "",
                        Description = dto.Description?.Trim(),
                        SKU = dto.SKU?.Trim(),
                        HSNCode = dto.HSNCode?.Trim(),
                        Barcode = dto.Barcode?.Trim(),
                        ProductType = dto.ProductType,
                        Unit = dto.Unit,
                        PurchasePrice = dto.PurchasePrice,
                        SalePrice = dto.SalePrice,
                        MRP = dto.MRP,
                        GSTRate = dto.GSTRate,
                        CessRate = dto.CessRate,
                        TrackInventory = dto.TrackInventory,
                        CurrentStock = dto.OpeningStock,
                        MinimumStock = dto.MinimumStock,
                        ReorderQty = dto.ReorderQty,
                        StorageLocation = dto.StorageLocation?.Trim(),
                        CategoryId = dto.CategoryId,
                        IsActive = true
                    };

                    await _products.AddAsync(product, ct);
                    await _uow.SaveChangesAsync(ct);

                    if (dto.OpeningStock > 0 && dto.TrackInventory)
                    {
                        var movement = new InventoryMovement
                        {
                            TenantId = tenantId,
                            ProductId = product.Id,
                            MovementType = InventoryMovementType.Opening,
                            Quantity = dto.OpeningStock,
                            StockBefore = 0,
                            StockAfter = dto.OpeningStock,
                            UnitCost = dto.PurchasePrice,
                            TotalCost = dto.OpeningStock * dto.PurchasePrice,
                            Notes = "Opening stock entry",
                            PerformedBy = userId
                        };
                        await _inventory.AddAsync(movement, ct);
                        await _uow.SaveChangesAsync(ct);
                    }

                }, ct);

                await _audit.LogAsync(tenantId, userId, "Product", product!.Id, "Create", ct: ct);
                _log.LogInformation("Product created: {Name} for tenant {TenantId}",
                    product!.Name, tenantId);

                return ApiResponse<ProductDto>.Ok(MapProduct(product!), "Product created.");
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to create product for tenant {TenantId}", tenantId);
                return ApiResponse<ProductDto>.Fail("Failed to create product.");
            }
        }

        public async Task<ApiResponse<ProductDto>> UpdateAsync(
            Guid tenantId, Guid id, UpdateProductDto dto, CancellationToken ct = default)
        {
            var product = await _products.GetByIdWithTenantAsync(id, tenantId, ct);
            if (product == null) return ApiResponse<ProductDto>.Fail("Product not found.");

            if (!string.IsNullOrEmpty(dto.SKU) && dto.SKU != product.SKU)
            {
                var isUnique = await _products.IsSKUUniqueAsync(tenantId, dto.SKU, id, ct);
                if (!isUnique) return ApiResponse<ProductDto>.Fail($"SKU '{dto.SKU}' already exists.");
            }

            product.Name = dto.Name; product.Description = dto.Description;
            product.SKU = dto.SKU; product.HSNCode = dto.HSNCode;
            product.Barcode = dto.Barcode; product.ProductType = dto.ProductType;
            product.Unit = dto.Unit; product.PurchasePrice = dto.PurchasePrice;
            product.SalePrice = dto.SalePrice; product.MRP = dto.MRP;
            product.GSTRate = dto.GSTRate; product.CessRate = dto.CessRate;
            product.TrackInventory = dto.TrackInventory;
            product.MinimumStock = dto.MinimumStock;
            product.ReorderQty = dto.ReorderQty;
            product.StorageLocation = dto.StorageLocation;
            product.CategoryId = dto.CategoryId; product.IsActive = dto.IsActive;

            _products.Update(product);
            await _uow.SaveChangesAsync(ct);
            return ApiResponse<ProductDto>.Ok(MapProduct(product), "Product updated.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(
            Guid tenantId, Guid id, CancellationToken ct = default)
        {
            var product = await _products.GetByIdWithTenantAsync(id, tenantId, ct);
            if (product == null) return ApiResponse<bool>.Fail("Product not found.");

            // Check if used in any invoice
            var usedInInvoice = await _products.AnyAsync(
                p => p.InvoiceItems.Any(i => i.ProductId == id), ct);
            if (usedInInvoice)
                return ApiResponse<bool>.Fail(
                    "Cannot delete product used in invoices. Deactivate it instead.");

            _products.SoftDelete(product);
            await _uow.SaveChangesAsync(ct);
            return ApiResponse<bool>.Ok(true, "Product deleted.");
        }

        public async Task<ApiResponse<string>> UploadImageAsync(
            Guid tenantId, Guid id, IFormFile file, CancellationToken ct = default)
        {
            var product = await _products.GetByIdWithTenantAsync(id, tenantId, ct);
            if (product == null) return ApiResponse<string>.Fail("Product not found.");

            if (!_files.IsValidImage(file))
                return ApiResponse<string>.Fail("Invalid image. Please upload JPG, PNG, or WebP.");

            if (!string.IsNullOrEmpty(product.ImageUrl))
                await _files.DeleteAsync(product.ImageUrl);

            var path = await _files.SaveAsync(file, $"products/{tenantId}", ct);
            product.ImageUrl = path;
            _products.Update(product);
            await _uow.SaveChangesAsync(ct);

            return ApiResponse<string>.Ok(_files.GetPublicUrl(path), "Image uploaded.");
        }

        public async Task<ApiResponse<IEnumerable<ProductListDto>>> GetLowStockAsync(
            Guid tenantId, CancellationToken ct = default)
        {
            var products = await _products.GetLowStockAsync(tenantId, ct);
            return ApiResponse<IEnumerable<ProductListDto>>.Ok(
                products.Select(p => new ProductListDto(
                    p.Id, p.Name, p.SKU, p.HSNCode, p.ProductType,
                    p.SalePrice, p.GSTRate, p.CurrentStock,
                    p.MinimumStock, true, p.IsActive)));
        }

        public async Task<ApiResponse<IEnumerable<SelectOptionDto>>> SearchAsync(
            Guid tenantId, string term, CancellationToken ct = default)
        {
            var products = await _products.SearchAsync(tenantId, term, ct);
            return ApiResponse<IEnumerable<SelectOptionDto>>.Ok(
                products.Select(p => new SelectOptionDto(p.Id, p.Name,
                    $"₹{p.SalePrice:N2} | GST {p.GSTRate}%")));
        }

        // ── Mapper ────────────────────────────────────────────────────────
        private static ProductDto MapProduct(Product p) => new(
            p.Id, p.Name, p.Description, p.SKU, p.HSNCode, p.Barcode,
            p.ProductType, p.Unit, p.PurchasePrice, p.SalePrice, p.MRP,
            p.GSTRate, p.CessRate, p.TrackInventory, p.CurrentStock,
            p.MinimumStock, p.ReorderQty, p.StorageLocation, p.ImageUrl,
            p.IsActive, p.CategoryId, p.Category?.Name, p.CreatedAt);
    }
}
