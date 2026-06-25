using Application.DTOs;
using Application.Services.System;
using Application.Services.Tenancy;
using Application.Services.Utilities;
using Core.Entities;
using Core.Enums;
using Core.Interfaces.Repositories;
using InvoiceHub.Application.DTOs.Finance;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Sales
{

    public class InvoiceService : IInvoiceService
    {
        private readonly IInvoiceRepository _invoices;
        private readonly IProductRepository _products;
        private readonly ITenantRepository _tenants;
        private readonly ICustomerRepository _customers;
        private readonly IGSTCalculationService _gst;
        private readonly IPdfService _pdf;
        private readonly IEmailService _email;
        private readonly IInventoryRepository _inventory;
        private readonly IAuditService _audit;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<InvoiceService> _log;

        public InvoiceService(
            IInvoiceRepository invoices, IProductRepository products,
            ITenantRepository tenants, ICustomerRepository customers,
            IGSTCalculationService gst, IPdfService pdf,
            IEmailService email, IInventoryRepository inventory,
            IAuditService audit, IUnitOfWork uow, ILogger<InvoiceService> log)
        {
            _invoices = invoices; _products = products; _tenants = tenants;
            _customers = customers; _gst = gst; _pdf = pdf;
            _email = email; _inventory = inventory;
            _audit = audit; _uow = uow; _log = log;
        }

        // ── List ──────────────────────────────────────────────────────────
        public async Task<ApiResponse<PagedResult<InvoiceListDto>>> GetListAsync(
            Guid tenantId, InvoiceFilterDto filter, CancellationToken ct = default)
        {
            var query = _invoices.Query(tenantId)
                .Include(i => i.Customer)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Search))
                query = query.Where(i =>
                    i.InvoiceNumber.Contains(filter.Search) ||
                    i.Customer.Name.Contains(filter.Search));

            if (filter.Status.HasValue) query = query.Where(i => i.Status == filter.Status);
            if (filter.CustomerId.HasValue) query = query.Where(i => i.CustomerId == filter.CustomerId);
            if (filter.FromDate.HasValue) query = query.Where(i => i.InvoiceDate >= filter.FromDate);
            if (filter.ToDate.HasValue) query = query.Where(i => i.InvoiceDate <= filter.ToDate);
            if (filter.OverdueOnly == true)
                query = query.Where(i =>
                    i.DueDate < DateTime.UtcNow &&
                    i.Status != InvoiceStatus.Paid &&
                    i.Status != InvoiceStatus.Cancelled);

            query = filter.SortBy switch
            {
                "CustomerName" => filter.SortDesc
                    ? query.OrderByDescending(i => i.Customer.Name)
                    : query.OrderBy(i => i.Customer.Name),
                "GrandTotal" => filter.SortDesc
                    ? query.OrderByDescending(i => i.GrandTotal)
                    : query.OrderBy(i => i.GrandTotal),
                "DueDate" => filter.SortDesc
                    ? query.OrderByDescending(i => i.DueDate)
                    : query.OrderBy(i => i.DueDate),
                _ => filter.SortDesc
                    ? query.OrderByDescending(i => i.InvoiceDate)
                    : query.OrderBy(i => i.InvoiceDate)
            };

            var total = await query.CountAsync(ct);
            var items = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(i => new InvoiceListDto(
                    i.Id, i.InvoiceNumber, i.InvoiceDate, i.DueDate,
                    i.CustomerId, i.Customer.Name,
                    i.GrandTotal, i.PaidAmount, i.BalanceDue, i.Status,
                    i.DueDate < DateTime.UtcNow &&
                    i.Status != InvoiceStatus.Paid &&
                    i.Status != InvoiceStatus.Cancelled))
                .ToListAsync(ct);

            return ApiResponse<PagedResult<InvoiceListDto>>.Ok(
                new PagedResult<InvoiceListDto>(items, total, filter.Page, filter.PageSize));
        }

        // ── Get ───────────────────────────────────────────────────────────
        public async Task<ApiResponse<InvoiceDto>> GetAsync(
            Guid tenantId, Guid id, CancellationToken ct = default)
        {
            var invoice = await _invoices.GetWithItemsAsync(id, tenantId, ct);
            if (invoice == null) return ApiResponse<InvoiceDto>.Fail("Invoice not found.");
            return ApiResponse<InvoiceDto>.Ok(MapInvoice(invoice));
        }

        // ── Create ────────────────────────────────────────────────────────
        public async Task<ApiResponse<InvoiceDto>> CreateAsync(
        Guid tenantId, Guid userId, CreateInvoiceDto dto, CancellationToken ct = default)
        {
            var customer = await _customers.GetByIdWithTenantAsync(dto.CustomerId, tenantId, ct);
            if (customer == null) return ApiResponse<InvoiceDto>.Fail("Customer not found.");

            Invoice? created = null;

            try
            {
                await _uow.ExecuteInTransactionAsync(async () =>
                {
                    var invoiceNumber = await _invoices.GenerateInvoiceNumberAsync(tenantId, ct);

                    var invoice = new Invoice
                    {
                        TenantId = tenantId,
                        InvoiceNumber = invoiceNumber,
                        InvoiceDate = dto.InvoiceDate,
                        DueDate = dto.DueDate,
                        CustomerId = dto.CustomerId,
                        IsInterState = dto.IsInterState,
                        PlaceOfSupply = dto.PlaceOfSupply,
                        PlaceOfSupplyCode = dto.PlaceOfSupplyCode,
                        DiscountType = dto.DiscountType,
                        DiscountPercent = dto.DiscountPercent,
                        Notes = dto.Notes,
                        TermsAndConditions = dto.TermsAndConditions,
                        ShippingDetails = dto.ShippingDetails,
                        VehicleNumber = dto.VehicleNumber,
                        EWayBillNumber = dto.EWayBillNumber,
                        Status = dto.SaveAsDraft ? InvoiceStatus.Draft : InvoiceStatus.Sent,
                        SalesAgentId = userId
                    };

                    var lineResults = new List<GSTLineResult>();
                    var sortOrder = 1;

                    foreach (var itemDto in dto.Items)
                    {
                        var product = await _products.GetByIdWithTenantAsync(itemDto.ProductId, tenantId, ct);
                        if (product == null)
                            throw new KeyNotFoundException($"Product {itemDto.ProductId} not found.");

                        if (product.TrackInventory && product.ProductType == ProductType.Goods)
                        {
                            if (product.CurrentStock < itemDto.Quantity)
                                throw new InvalidOperationException(
                                    $"Insufficient stock for '{product.Name}'. Available: {product.CurrentStock}");
                        }

                        var line = _gst.CalculateLine(
                            itemDto.Quantity, itemDto.UnitPrice, itemDto.DiscountPercent,
                            product.GSTRate, product.CessRate, dto.IsInterState);

                        invoice.Items.Add(new InvoiceItem
                        {
                            TenantId = tenantId,
                            SortOrder = sortOrder++,
                            ProductId = product.Id,
                            Description = itemDto.Description ?? product.Description,
                            Quantity = itemDto.Quantity,
                            Unit = product.Unit,
                            UnitPrice = itemDto.UnitPrice,
                            DiscountPercent = itemDto.DiscountPercent,
                            DiscountAmount = line.DiscountAmount,
                            TaxableAmount = line.TaxableAmount,
                            HSNCode = product.HSNCode,
                            GSTRate = product.GSTRate,
                            IGSTRate = line.IGSTRate,
                            IGSTAmount = line.IGSTAmount,
                            CGSTRate = line.CGSTRate,
                            CGSTAmount = line.CGSTAmount,
                            SGSTRate = line.SGSTRate,
                            SGSTAmount = line.SGSTAmount,
                            CessRate = product.CessRate ?? 0,
                            CessAmount = line.CessAmount,
                            TotalAmount = line.TotalAmount
                        });

                        lineResults.Add(line);
                    }

                    var totals = _gst.CalculateTotals(
                        lineResults, dto.DiscountAmount, dto.DiscountType,
                        dto.DiscountPercent, dto.IsInterState);

                    invoice.SubTotal = totals.SubTotal;
                    invoice.DiscountAmount = totals.DiscountAmount;
                    invoice.TaxableAmount = totals.TaxableAmount;
                    invoice.IGSTAmount = totals.IGSTAmount;
                    invoice.CGSTAmount = totals.CGSTAmount;
                    invoice.SGSTAmount = totals.SGSTAmount;
                    invoice.CessAmount = totals.CessAmount;
                    invoice.TotalTaxAmount = totals.TotalTaxAmount;
                    invoice.RoundOff = totals.RoundOff;
                    invoice.GrandTotal = totals.GrandTotal;
                    invoice.BalanceDue = totals.GrandTotal;

                    await _invoices.AddAsync(invoice, ct);

                    if (!dto.SaveAsDraft)
                    {
                        foreach (var item in invoice.Items)
                        {
                            var product = await _products.GetByIdAsync(item.ProductId, ct);
                            if (product is { TrackInventory: true })
                            {
                                var movement = new InventoryMovement
                                {
                                    TenantId = tenantId,
                                    ProductId = item.ProductId,
                                    MovementType = InventoryMovementType.Sale,
                                    Quantity = -item.Quantity,
                                    StockBefore = product.CurrentStock,
                                    StockAfter = product.CurrentStock - item.Quantity,
                                    UnitCost = item.UnitPrice,
                                    TotalCost = item.Quantity * item.UnitPrice,
                                    ReferenceType = "Invoice",
                                    ReferenceId = invoice.Id,
                                    PerformedBy = userId
                                };
                                await _inventory.AddAsync(movement, ct);
                                await _inventory.UpdateStockAsync(item.ProductId, -item.Quantity, ct);
                            }
                        }
                    }

                    await _uow.SaveChangesAsync(ct);

                    created = await _invoices.GetWithItemsAsync(invoice.Id, tenantId, ct);

                }, ct);

                await _audit.LogAsync(tenantId, userId, "Invoice", created!.Id, "Create", ct: ct);
                _log.LogInformation("Invoice created: {Number} for tenant {TenantId}",
                    created!.InvoiceNumber, tenantId);

                return ApiResponse<InvoiceDto>.Ok(MapInvoice(created!), "Invoice created successfully.");
            }
            catch (KeyNotFoundException ex)
            {
                return ApiResponse<InvoiceDto>.Fail(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponse<InvoiceDto>.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to create invoice for tenant {TenantId}", tenantId);
                var inner = ex.InnerException;
                var msg = inner != null
                    ? $"{ex.GetType().Name}: {ex.Message} → {inner.GetType().Name}: {inner.Message}"
                    : $"{ex.GetType().Name}: {ex.Message}";
                return ApiResponse<InvoiceDto>.Fail(msg);
            }
        }

        // ── Update ────────────────────────────────────────────────────────
        public async Task<ApiResponse<InvoiceDto>> UpdateAsync(
            Guid tenantId, Guid id, UpdateInvoiceDto dto, CancellationToken ct = default)
        {
            var invoice = await _invoices.GetWithItemsAsync(id, tenantId, ct);
            if (invoice == null) return ApiResponse<InvoiceDto>.Fail("Invoice not found.");

            if (invoice.Status is InvoiceStatus.Paid or InvoiceStatus.Cancelled)
                return ApiResponse<InvoiceDto>.Fail("Cannot edit a paid or cancelled invoice.");

            var oldValues = new { invoice.GrandTotal, invoice.Status };

            invoice.InvoiceDate = dto.InvoiceDate;
            invoice.DueDate = dto.DueDate;
            invoice.IsInterState = dto.IsInterState;
            invoice.PlaceOfSupply = dto.PlaceOfSupply;
            invoice.DiscountType = dto.DiscountType;
            invoice.DiscountPercent = dto.DiscountPercent;
            invoice.Notes = dto.Notes;
            invoice.TermsAndConditions = dto.TermsAndConditions;
            invoice.EWayBillNumber = dto.EWayBillNumber;

            // Rebuild line items
            invoice.Items.Clear();
            var lineResults = new List<GSTLineResult>();
            var sortOrder = 1;

            foreach (var itemDto in dto.Items)
            {
                var product = await _products.GetByIdWithTenantAsync(itemDto.ProductId, tenantId, ct);
                if (product == null)
                    return ApiResponse<InvoiceDto>.Fail($"Product {itemDto.ProductId} not found.");

                var line = _gst.CalculateLine(
                    itemDto.Quantity, itemDto.UnitPrice, itemDto.DiscountPercent,
                    product.GSTRate, product.CessRate, dto.IsInterState);

                invoice.Items.Add(new InvoiceItem
                {
                    TenantId = invoice.TenantId,
                    SortOrder = sortOrder++,
                    ProductId = product.Id,
                    Description = itemDto.Description ?? product.Description,
                    Quantity = itemDto.Quantity,
                    Unit = product.Unit,
                    UnitPrice = itemDto.UnitPrice,
                    DiscountPercent = itemDto.DiscountPercent,
                    DiscountAmount = line.DiscountAmount,
                    TaxableAmount = line.TaxableAmount,
                    HSNCode = product.HSNCode,
                    GSTRate = product.GSTRate,
                    IGSTRate = line.IGSTRate,
                    IGSTAmount = line.IGSTAmount,
                    CGSTRate = line.CGSTRate,
                    CGSTAmount = line.CGSTAmount,
                    SGSTRate = line.SGSTRate,
                    SGSTAmount = line.SGSTAmount,
                    CessRate = product.CessRate ?? 0,
                    CessAmount = line.CessAmount,
                    TotalAmount = line.TotalAmount
                });
                lineResults.Add(line);
            }

            var totals = _gst.CalculateTotals(
                lineResults, dto.DiscountAmount, dto.DiscountType,
                dto.DiscountPercent, dto.IsInterState);

            invoice.SubTotal = totals.SubTotal;
            invoice.DiscountAmount = totals.DiscountAmount;
            invoice.TaxableAmount = totals.TaxableAmount;
            invoice.IGSTAmount = totals.IGSTAmount;
            invoice.CGSTAmount = totals.CGSTAmount;
            invoice.SGSTAmount = totals.SGSTAmount;
            invoice.CessAmount = totals.CessAmount;
            invoice.TotalTaxAmount = totals.TotalTaxAmount;
            invoice.RoundOff = totals.RoundOff;
            invoice.GrandTotal = totals.GrandTotal;
            invoice.BalanceDue = invoice.GrandTotal - invoice.PaidAmount;

            _invoices.Update(invoice);
            await _uow.SaveChangesAsync(ct);

            // Re-fetch to load Product navigation properties on items
            invoice = await _invoices.GetWithItemsAsync(id, tenantId, ct)
                ?? throw new InvalidOperationException("Invoice not found after update.");

            await _audit.LogAsync(tenantId, Guid.Empty, "Invoice", id, "Update",
                oldValues: oldValues,
                newValues: new { invoice.GrandTotal, invoice.Status }, ct: ct);

            return ApiResponse<InvoiceDto>.Ok(MapInvoice(invoice), "Invoice updated.");
        }

        // ── Send ──────────────────────────────────────────────────────────
        public async Task<ApiResponse<bool>> SendAsync(
            Guid tenantId, Guid id, CancellationToken ct = default)
        {
            var invoice = await _invoices.GetWithItemsAsync(id, tenantId, ct);
            if (invoice == null) return ApiResponse<bool>.Fail("Invoice not found.");

            if (string.IsNullOrEmpty(invoice.Customer.Email))
                return ApiResponse<bool>.Fail("Customer email not available.");

            var tenant = await _tenants.GetByIdAsync(tenantId, ct);
            if (tenant == null) return ApiResponse<bool>.Fail("Tenant not found.");

            var pdfBytes = _pdf.GenerateInvoicePdf(MapInvoice(invoice), TenantService.MapTenant(tenant));

            await _email.SendInvoiceAsync(
                invoice.Customer.Email, invoice.Customer.Name,
                invoice.InvoiceNumber, pdfBytes, tenant.BusinessName);

            invoice.Status = InvoiceStatus.Sent;
            _invoices.Update(invoice);
            await _uow.SaveChangesAsync(ct);

            _log.LogInformation("Invoice {Number} sent to {Email}", invoice.InvoiceNumber, invoice.Customer.Email);
            return ApiResponse<bool>.Ok(true, "Invoice sent successfully.");
        }

        // ── Cancel ────────────────────────────────────────────────────────
        public async Task<ApiResponse<bool>> CancelAsync(
            Guid tenantId, Guid id, CancellationToken ct = default)
        {
            var invoice = await _invoices.GetByIdWithTenantAsync(id, tenantId, ct);
            if (invoice == null) return ApiResponse<bool>.Fail("Invoice not found.");

            if (invoice.PaidAmount > 0)
                return ApiResponse<bool>.Fail(
                    "Cannot cancel a partially or fully paid invoice. Issue a credit note instead.");

            invoice.Status = InvoiceStatus.Cancelled;
            _invoices.Update(invoice);
            await _uow.SaveChangesAsync(ct);

            _log.LogInformation("Invoice {Number} cancelled", invoice.InvoiceNumber);
            return ApiResponse<bool>.Ok(true, "Invoice cancelled.");
        }

        // ── Mark Overdue (batch job) ──────────────────────────────────────
        public async Task<ApiResponse<bool>> MarkOverdueAsync(
            Guid tenantId, CancellationToken ct = default)
        {
            var overdueInvoices = _invoices.Query(tenantId)
                .Where(i => i.DueDate < DateTime.UtcNow
                    && i.Status == InvoiceStatus.Sent
                    && i.BalanceDue > 0)
                .ToList();

            foreach (var inv in overdueInvoices)
                inv.Status = InvoiceStatus.Overdue;

            _invoices.UpdateRange(overdueInvoices);
            await _uow.SaveChangesAsync(ct);

            _log.LogInformation("Marked {Count} invoices as overdue for tenant {TenantId}",
                overdueInvoices.Count, tenantId);
            return ApiResponse<bool>.Ok(true, $"{overdueInvoices.Count} invoices marked as overdue.");
        }

        // ── PDF ───────────────────────────────────────────────────────────
        public async Task<ApiResponse<byte[]>> GeneratePdfAsync(
            Guid tenantId, Guid id, CancellationToken ct = default)
        {
            var invoice = await _invoices.GetWithItemsAsync(id, tenantId, ct);
            if (invoice == null) return ApiResponse<byte[]>.Fail("Invoice not found.");

            var tenant = await _tenants.GetByIdAsync(tenantId, ct);
            if (tenant == null) return ApiResponse<byte[]>.Fail("Tenant not found.");

            var pdf = _pdf.GenerateInvoicePdf(MapInvoice(invoice), TenantService.MapTenant(tenant));
            return ApiResponse<byte[]>.Ok(pdf);
        }

        // ── GST Summary ───────────────────────────────────────────────────
        public async Task<ApiResponse<GSTSummaryDto>> GetGSTSummaryAsync(
            Guid tenantId, int month, int year, CancellationToken ct = default)
        {
            var from = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var to = from.AddMonths(1).AddTicks(-1);

            var invoices = (await _invoices.GetByDateRangeAsync(tenantId, from, to, ct))
                .Where(i => i.Status != InvoiceStatus.Cancelled && i.Status != InvoiceStatus.Draft)
                .ToList();

            var hsnSummary = invoices
                .SelectMany(i => i.Items)
                .Where(item => !string.IsNullOrEmpty(item.HSNCode))
                .GroupBy(item => item.HSNCode!)
                .Select(g => new GSTHSNSummaryDto(
                    g.Key,
                    g.Sum(i => i.TaxableAmount),
                    g.Sum(i => i.IGSTAmount),
                    g.Sum(i => i.CGSTAmount),
                    g.Sum(i => i.SGSTAmount),
                    g.Sum(i => i.IGSTAmount + i.CGSTAmount + i.SGSTAmount)));

            var statewiseSummary = invoices
                .Where(i => !string.IsNullOrEmpty(i.PlaceOfSupply))
                .GroupBy(i => new { i.PlaceOfSupply, i.PlaceOfSupplyCode })
                .Select(g => new GSTStatewiseSummaryDto(
                    g.Key.PlaceOfSupply!, g.Key.PlaceOfSupplyCode ?? "",
                    g.Sum(i => i.TaxableAmount),
                    g.Sum(i => i.IGSTAmount),
                    g.Sum(i => i.CGSTAmount),
                    g.Sum(i => i.SGSTAmount),
                    g.Count()));

            return ApiResponse<GSTSummaryDto>.Ok(new GSTSummaryDto(
                month, year,
                invoices.Sum(i => i.TaxableAmount),
                invoices.Sum(i => i.IGSTAmount),
                invoices.Sum(i => i.CGSTAmount),
                invoices.Sum(i => i.SGSTAmount),
                invoices.Sum(i => i.CessAmount),
                invoices.Sum(i => i.TotalTaxAmount),
                hsnSummary, statewiseSummary));
        }

        // ── Mappers ───────────────────────────────────────────────────────
        internal static InvoiceDto MapInvoice(Invoice i) => new(
            i.Id, i.InvoiceNumber, i.InvoiceDate, i.DueDate, i.Status,
            i.CustomerId, i.Customer?.Name ?? "",
            i.Customer?.GSTIN,
            i.Customer != null ? new AddressDto(
                i.Customer.BillingAddressLine1, i.Customer.BillingAddressLine2,
                i.Customer.BillingCity, i.Customer.BillingState,
                i.Customer.BillingStateCode, i.Customer.BillingPinCode,
                i.Customer.BillingCountry) : null,
            null,
            i.IsInterState, i.PlaceOfSupply, i.PlaceOfSupplyCode,
            i.Items.OrderBy(x => x.SortOrder).Select(MapItem),
            i.SubTotal, i.DiscountAmount, i.DiscountType, i.DiscountPercent,
            i.TaxableAmount, i.IGSTAmount, i.CGSTAmount, i.SGSTAmount, i.CessAmount,
            i.TotalTaxAmount, i.RoundOff, i.GrandTotal, i.PaidAmount, i.BalanceDue,
            i.Notes, i.TermsAndConditions, i.EWayBillNumber, i.IRN,
            i.Payments?.Select(MapPayment) ?? [],
            i.CreatedAt);

        private static InvoiceItemDto MapItem(InvoiceItem x) => new(
            x.Id, x.SortOrder, x.ProductId, x.Product?.Name ?? "",
            x.HSNCode, x.Description, x.Quantity, x.Unit,
            x.UnitPrice, x.DiscountPercent, x.DiscountAmount, x.TaxableAmount,
            x.GSTRate, x.IGSTRate, x.IGSTAmount, x.CGSTRate, x.CGSTAmount,
            x.SGSTRate, x.SGSTAmount, x.CessRate, x.CessAmount, x.TotalAmount);

        internal static PaymentDto MapPayment(Payment p) => new(
            p.Id, p.PaymentNumber, p.PaymentDate, p.Amount, p.Method, p.Status,
            p.InvoiceId, p.Invoice?.InvoiceNumber, p.CustomerId,
            p.Customer?.Name, p.ReferenceNumber, p.BankName, p.Notes,
            p.IsRefund, p.CreatedAt);
    }

}
