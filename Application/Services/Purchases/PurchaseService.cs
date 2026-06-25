using Application.DTOs;
using Application.Services.Sales;
using Application.Services.System;
using Application.Services.Tenancy;
using Application.Services.Utilities;
using Core.Entities;
using Core.Enums;
using Core.Interfaces.Repositories;
using InvoiceHub.Application.DTOs.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Services.Purchases
{

    public class PurchaseService : IPurchaseService
    {
        private readonly IPurchaseOrderRepository _pos;
        private readonly IProductRepository _products;
        private readonly ISupplierRepository _suppliers;
        private readonly ITenantRepository _tenants;
        private readonly IGSTCalculationService _gst;
        private readonly IPdfService _pdf;
        private readonly IInventoryRepository _inventory;
        private readonly IAuditService _audit;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<PurchaseService> _log;

        public PurchaseService(
            IPurchaseOrderRepository pos, IProductRepository products,
            ISupplierRepository suppliers, ITenantRepository tenants,
            IGSTCalculationService gst, IPdfService pdf,
            IInventoryRepository inventory, IAuditService audit,
            IUnitOfWork uow, ILogger<PurchaseService> log)
        {
            _pos = pos; _products = products; _suppliers = suppliers;
            _tenants = tenants; _gst = gst; _pdf = pdf;
            _inventory = inventory; _audit = audit; _uow = uow; _log = log;
        }

        public async Task<ApiResponse<PagedResult<PurchaseOrderListDto>>> GetListAsync(
            Guid tenantId, PurchaseOrderFilterDto filter, CancellationToken ct = default)
        {
            var query = _pos.Query(tenantId)
                .Include(p => p.Supplier)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Search))
                query = query.Where(p =>
                    p.PONumber.Contains(filter.Search) ||
                    p.Supplier.Name.Contains(filter.Search));

            if (filter.Status.HasValue) query = query.Where(p => p.Status == filter.Status);
            if (filter.SupplierId.HasValue) query = query.Where(p => p.SupplierId == filter.SupplierId);
            if (filter.FromDate.HasValue) query = query.Where(p => p.PODate >= filter.FromDate);
            if (filter.ToDate.HasValue) query = query.Where(p => p.PODate <= filter.ToDate);

            query = filter.SortDesc
                ? query.OrderByDescending(p => p.PODate)
                : query.OrderBy(p => p.PODate);

            var total = await query.CountAsync(ct);
            var items = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(p => new PurchaseOrderListDto(
                    p.Id, p.PONumber, p.PODate, p.Supplier.Name,
                    p.GrandTotal, p.PaidAmount, p.BalanceDue,
                    p.Status, p.ExpectedDeliveryDate))
                .ToListAsync(ct);

            return ApiResponse<PagedResult<PurchaseOrderListDto>>.Ok(
                new PagedResult<PurchaseOrderListDto>(items, total, filter.Page, filter.PageSize));
        }

        public async Task<ApiResponse<PurchaseOrderDto>> GetAsync(
            Guid tenantId, Guid id, CancellationToken ct = default)
        {
            var po = await _pos.GetWithItemsAsync(id, tenantId, ct);
            if (po == null) return ApiResponse<PurchaseOrderDto>.Fail("Purchase order not found.");
            return ApiResponse<PurchaseOrderDto>.Ok(MapPO(po));
        }

        public async Task<ApiResponse<PurchaseOrderDto>> CreateAsync(
    Guid tenantId, Guid userId, CreatePurchaseOrderDto dto, CancellationToken ct = default)
        {
            var supplier = await _suppliers.GetByIdWithTenantAsync(dto.SupplierId, tenantId, ct);
            if (supplier == null) return ApiResponse<PurchaseOrderDto>.Fail("Supplier not found.");

            PurchaseOrder? po = null;

            try
            {
                await _uow.ExecuteInTransactionAsync(async () =>
                {
                    var poNumber = await _pos.GeneratePONumberAsync(tenantId, ct);

                    po = new PurchaseOrder
                    {
                        TenantId = tenantId,
                        PONumber = poNumber,
                        PODate = dto.PODate,
                        ExpectedDeliveryDate = dto.ExpectedDeliveryDate,
                        SupplierId = dto.SupplierId,
                        IsInterState = dto.IsInterState,
                        SupplierInvoiceNumber = dto.SupplierInvoiceNumber,
                        SupplierInvoiceDate = dto.SupplierInvoiceDate,
                        Notes = dto.Notes,
                        Status = dto.SaveAsDraft ? PurchaseOrderStatus.Draft : PurchaseOrderStatus.Ordered
                    };

                    var lineResults = new List<GSTLineResult>();
                    var sortOrder = 1;

                    foreach (var itemDto in dto.Items)
                    {
                        var product = await _products.GetByIdWithTenantAsync(itemDto.ProductId, tenantId, ct);
                        if (product == null)
                            throw new KeyNotFoundException($"Product {itemDto.ProductId} not found.");

                        var line = _gst.CalculateLine(
                            itemDto.OrderedQty, itemDto.UnitPrice, itemDto.DiscountPercent,
                            product.GSTRate, product.CessRate, dto.IsInterState);

                        po.Items.Add(new PurchaseOrderItem
                        {
                            SortOrder = sortOrder++,
                            ProductId = product.Id,
                            Description = itemDto.Description,
                            OrderedQty = itemDto.OrderedQty,
                            Unit = product.Unit,
                            UnitPrice = itemDto.UnitPrice,
                            DiscountPercent = itemDto.DiscountPercent,
                            DiscountAmount = line.DiscountAmount,
                            TaxableAmount = line.TaxableAmount,
                            HSNCode = product.HSNCode,
                            GSTRate = product.GSTRate,
                            IGSTAmount = line.IGSTAmount,
                            CGSTAmount = line.CGSTAmount,
                            SGSTAmount = line.SGSTAmount,
                            CessAmount = line.CessAmount,
                            TotalAmount = line.TotalAmount
                        });
                        lineResults.Add(line);
                    }

                    var totals = _gst.CalculateTotals(
                        lineResults, null, DiscountType.None, null, dto.IsInterState);

                    po.SubTotal = totals.SubTotal;
                    po.DiscountAmount = totals.DiscountAmount;
                    po.TaxableAmount = totals.TaxableAmount;
                    po.IGSTAmount = totals.IGSTAmount;
                    po.CGSTAmount = totals.CGSTAmount;
                    po.SGSTAmount = totals.SGSTAmount;
                    po.CessAmount = totals.CessAmount;
                    po.TotalTaxAmount = totals.TotalTaxAmount;
                    po.RoundOff = totals.RoundOff;
                    po.GrandTotal = totals.GrandTotal;
                    po.BalanceDue = totals.GrandTotal;

                    await _pos.AddAsync(po, ct);
                    await _uow.SaveChangesAsync(ct);

                }, ct);

                await _audit.LogAsync(tenantId, userId, "PurchaseOrder", po!.Id, "Create", ct: ct);
                _log.LogInformation("PO created: {PONumber} for tenant {TenantId}",
                    po!.PONumber, tenantId);

                var created = await _pos.GetWithItemsAsync(po!.Id, tenantId, ct);
                return ApiResponse<PurchaseOrderDto>.Ok(MapPO(created!), "Purchase order created.");
            }
            catch (KeyNotFoundException ex)
            {
                return ApiResponse<PurchaseOrderDto>.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to create PO for tenant {TenantId}", tenantId);
                return ApiResponse<PurchaseOrderDto>.Fail("Failed to create purchase order.");
            }
        }

        public async Task<ApiResponse<PurchaseOrderDto>> UpdateAsync(
            Guid tenantId, Guid id, UpdatePurchaseOrderDto dto, CancellationToken ct = default)
        {
            var po = await _pos.GetWithItemsAsync(id, tenantId, ct);
            if (po == null) return ApiResponse<PurchaseOrderDto>.Fail("Purchase order not found.");

            if (po.Status is PurchaseOrderStatus.Received or PurchaseOrderStatus.Cancelled)
                return ApiResponse<PurchaseOrderDto>.Fail("Cannot edit a received or cancelled purchase order.");

            po.PODate = dto.PODate;
            po.ExpectedDeliveryDate = dto.ExpectedDeliveryDate;
            po.IsInterState = dto.IsInterState;
            po.SupplierInvoiceNumber = dto.SupplierInvoiceNumber;
            po.SupplierInvoiceDate = dto.SupplierInvoiceDate;
            po.Notes = dto.Notes;

            po.Items.Clear();
            var lineResults = new List<GSTLineResult>();
            var sortOrder = 1;

            foreach (var itemDto in dto.Items)
            {
                var product = await _products.GetByIdWithTenantAsync(itemDto.ProductId, tenantId, ct);
                if (product == null)
                    return ApiResponse<PurchaseOrderDto>.Fail($"Product {itemDto.ProductId} not found.");

                var line = _gst.CalculateLine(
                    itemDto.OrderedQty, itemDto.UnitPrice, itemDto.DiscountPercent,
                    product.GSTRate, product.CessRate, dto.IsInterState);

                po.Items.Add(new PurchaseOrderItem
                {
                    SortOrder = sortOrder++,
                    ProductId = product.Id,
                    Description = itemDto.Description,
                    OrderedQty = itemDto.OrderedQty,
                    Unit = product.Unit,
                    UnitPrice = itemDto.UnitPrice,
                    DiscountPercent = itemDto.DiscountPercent,
                    DiscountAmount = line.DiscountAmount,
                    TaxableAmount = line.TaxableAmount,
                    HSNCode = product.HSNCode,
                    GSTRate = product.GSTRate,
                    IGSTAmount = line.IGSTAmount,
                    CGSTAmount = line.CGSTAmount,
                    SGSTAmount = line.SGSTAmount,
                    CessAmount = line.CessAmount,
                    TotalAmount = line.TotalAmount
                });
                lineResults.Add(line);
            }

            var totals = _gst.CalculateTotals(lineResults, null, DiscountType.None, null, dto.IsInterState);
            po.SubTotal = totals.SubTotal; po.DiscountAmount = totals.DiscountAmount;
            po.TaxableAmount = totals.TaxableAmount; po.IGSTAmount = totals.IGSTAmount;
            po.CGSTAmount = totals.CGSTAmount; po.SGSTAmount = totals.SGSTAmount;
            po.CessAmount = totals.CessAmount; po.TotalTaxAmount = totals.TotalTaxAmount;
            po.RoundOff = totals.RoundOff; po.GrandTotal = totals.GrandTotal;
            po.BalanceDue = po.GrandTotal - po.PaidAmount;

            _pos.Update(po);
            await _uow.SaveChangesAsync(ct);
            return ApiResponse<PurchaseOrderDto>.Ok(MapPO(po), "Purchase order updated.");
        }

        public async Task<ApiResponse<bool>> MarkReceivedAsync(
    Guid tenantId, Guid id, CancellationToken ct = default)
        {
            var po = await _pos.GetWithItemsAsync(id, tenantId, ct);
            if (po == null) return ApiResponse<bool>.Fail("Purchase order not found.");

            if (po.Status == PurchaseOrderStatus.Cancelled)
                return ApiResponse<bool>.Fail("Cannot mark a cancelled PO as received.");

            try
            {
                await _uow.ExecuteInTransactionAsync(async () =>
                {
                    po.Status = PurchaseOrderStatus.Received;
                    po.ReceivedDate = DateTime.UtcNow;

                    foreach (var item in po.Items)
                    {
                        item.ReceivedQty = item.OrderedQty;
                        var product = await _products.GetByIdAsync(item.ProductId, ct);
                        if (product is { TrackInventory: true })
                        {
                            var movement = new InventoryMovement
                            {
                                TenantId = tenantId,
                                ProductId = item.ProductId,
                                MovementType = InventoryMovementType.Purchase,
                                Quantity = item.OrderedQty,
                                StockBefore = product.CurrentStock,
                                StockAfter = product.CurrentStock + item.OrderedQty,
                                UnitCost = item.UnitPrice,
                                TotalCost = item.OrderedQty * item.UnitPrice,
                                ReferenceType = "PurchaseOrder",
                                ReferenceId = po.Id
                            };
                            await _inventory.AddAsync(movement, ct);
                            await _inventory.UpdateStockAsync(item.ProductId, item.OrderedQty, ct);
                        }
                    }

                    _pos.Update(po);
                    await _uow.SaveChangesAsync(ct);

                }, ct);

                _log.LogInformation("PO {PONumber} marked as received", po.PONumber);
                return ApiResponse<bool>.Ok(true, "Purchase order marked as received. Stock updated.");
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to mark PO {Id} as received", id);
                return ApiResponse<bool>.Fail("Failed to update purchase order.");
            }
        }

        public async Task<ApiResponse<bool>> CancelAsync(
            Guid tenantId, Guid id, CancellationToken ct = default)
        {
            var po = await _pos.GetByIdWithTenantAsync(id, tenantId, ct);
            if (po == null) return ApiResponse<bool>.Fail("Purchase order not found.");

            if (po.PaidAmount > 0)
                return ApiResponse<bool>.Fail("Cannot cancel a partially or fully paid purchase order.");

            po.Status = PurchaseOrderStatus.Cancelled;
            _pos.Update(po);
            await _uow.SaveChangesAsync(ct);
            return ApiResponse<bool>.Ok(true, "Purchase order cancelled.");
        }

        public async Task<ApiResponse<byte[]>> GeneratePdfAsync(
            Guid tenantId, Guid id, CancellationToken ct = default)
        {
            var po = await _pos.GetWithItemsAsync(id, tenantId, ct);
            if (po == null) return ApiResponse<byte[]>.Fail("Purchase order not found.");
            var tenant = await _tenants.GetByIdAsync(tenantId, ct);
            if (tenant == null) return ApiResponse<byte[]>.Fail("Tenant not found.");
            var pdf = _pdf.GeneratePurchaseOrderPdf(MapPO(po), TenantService.MapTenant(tenant));
            return ApiResponse<byte[]>.Ok(pdf);
        }

        // ── Mapper ────────────────────────────────────────────────────────
        private static PurchaseOrderDto MapPO(PurchaseOrder p) => new(
            p.Id, p.PONumber, p.PODate, p.ExpectedDeliveryDate, p.ReceivedDate,
            p.Status, p.SupplierId, p.Supplier?.Name ?? "",
            p.Supplier?.GSTIN, p.SupplierInvoiceNumber, p.SupplierInvoiceDate,
            p.Items.OrderBy(x => x.SortOrder).Select(i => new PurchaseOrderItemDto(
                i.Id, i.SortOrder, i.ProductId, i.Product?.Name ?? "",
                i.HSNCode, i.Description, i.OrderedQty, i.ReceivedQty, i.Unit,
                i.UnitPrice, i.DiscountPercent, i.DiscountAmount, i.TaxableAmount,
                i.GSTRate, i.IGSTAmount, i.CGSTAmount, i.SGSTAmount, i.CessAmount, i.TotalAmount)),
            p.SubTotal, p.DiscountAmount, p.TaxableAmount,
            p.IGSTAmount, p.CGSTAmount, p.SGSTAmount, p.CessAmount, p.TotalTaxAmount,
            p.RoundOff, p.GrandTotal, p.PaidAmount, p.BalanceDue,
            p.IsInterState, p.Notes,
            p.Payments?.Select(InvoiceService.MapPayment) ?? [],
            p.CreatedAt);
    }
}
