using Application.DTOs;
using Application.Services.System;
using Core.Entities;
using Core.Enums;
using Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Tenancy
{

    public class TenantService : ITenantService
    {
        private readonly ITenantRepository _tenants;
        private readonly IInvoiceRepository _invoices;
        private readonly IExpenseRepository _expenses;
        private readonly ICustomerRepository _customers;
        private readonly IProductRepository _products;
        private readonly IPurchaseOrderRepository _purchaseOrders;
        private readonly IAIInsightRepository _insights;
        private readonly IFileService _files;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<TenantService> _log;

        public TenantService(
            ITenantRepository tenants,
            IInvoiceRepository invoices,
            IExpenseRepository expenses,
            ICustomerRepository customers,
            IProductRepository products,
            IPurchaseOrderRepository purchaseOrders,
            IAIInsightRepository insights,
            IFileService files,
            IUnitOfWork uow,
            ILogger<TenantService> log)
        {
            _tenants = tenants; _invoices = invoices; _expenses = expenses;
            _customers = customers; _products = products; _purchaseOrders = purchaseOrders;
            _insights = insights; _files = files; _uow = uow; _log = log;
        }

        public async Task<ApiResponse<TenantDto>> GetAsync(Guid tenantId, CancellationToken ct = default)
        {
            var tenant = await _tenants.GetByIdAsync(tenantId, ct);
            if (tenant == null) return ApiResponse<TenantDto>.Fail("Tenant not found.");
            return ApiResponse<TenantDto>.Ok(MapTenant(tenant));
        }

        public async Task<ApiResponse<TenantDto>> UpdateAsync(Guid tenantId, UpdateTenantDto dto, CancellationToken ct = default)
        {
            var tenant = await _tenants.GetByIdAsync(tenantId, ct);
            if (tenant == null) return ApiResponse<TenantDto>.Fail("Tenant not found.");

            // GSTIN uniqueness check (exclude self)
            if (!string.IsNullOrEmpty(dto.GSTIN) && dto.GSTIN != tenant.GSTIN)
            {
                var isUnique = await _tenants.IsGSTINUniqueAsync(dto.GSTIN, tenantId, ct);
                if (!isUnique)
                    return ApiResponse<TenantDto>.Fail("GSTIN is already registered with another business.");
            }

            tenant.BusinessName = dto.BusinessName;
            tenant.LegalName = dto.LegalName;
            tenant.GSTIN = dto.GSTIN;
            tenant.PAN = dto.PAN;
            tenant.TAN = dto.TAN;
            tenant.CIN = dto.CIN;
            tenant.Email = dto.Email;
            tenant.Phone = dto.Phone;
            tenant.Website = dto.Website;
            tenant.AddressLine1 = dto.Address.Line1;
            tenant.AddressLine2 = dto.Address.Line2;
            tenant.City = dto.Address.City;
            tenant.State = dto.Address.State;
            tenant.StateCode = dto.Address.StateCode;
            tenant.PinCode = dto.Address.PinCode;
            tenant.Country = dto.Address.Country ?? "India";
            tenant.InvoicePrefix = dto.InvoicePrefix ?? "INV";
            tenant.PurchasePrefix = dto.PurchasePrefix ?? "PO";
            tenant.FinancialYearStart = dto.FinancialYearStart;
            tenant.IsGSTRegistered = dto.IsGSTRegistered;
            tenant.BankName = dto.BankName;
            tenant.BankAccountNumber = dto.BankAccountNumber;
            tenant.BankIFSC = dto.BankIFSC;
            tenant.BankBranch = dto.BankBranch;
            tenant.UPIId = dto.UPIId;

            _tenants.Update(tenant);
            await _uow.SaveChangesAsync(ct);

            _log.LogInformation("Tenant {TenantId} updated: {BusinessName}", tenantId, tenant.BusinessName);
            return ApiResponse<TenantDto>.Ok(MapTenant(tenant), "Business profile updated successfully.");
        }

        public async Task<ApiResponse<string>> UploadLogoAsync(Guid tenantId, IFormFile file, CancellationToken ct = default)
        {
            var tenant = await _tenants.GetByIdAsync(tenantId, ct);
            if (tenant == null) return ApiResponse<string>.Fail("Tenant not found.");

            if (!_files.IsValidImage(file))
                return ApiResponse<string>.Fail("Invalid image file. Please upload JPG, PNG, or WebP.");

            // Delete old logo
            if (!string.IsNullOrEmpty(tenant.BusinessLogo))
                await _files.DeleteAsync(tenant.BusinessLogo);

            var path = await _files.SaveAsync(file, $"logos/{tenantId}", ct);
            tenant.BusinessLogo = path;
            _tenants.Update(tenant);
            await _uow.SaveChangesAsync(ct);

            return ApiResponse<string>.Ok(_files.GetPublicUrl(path), "Logo uploaded successfully.");
        }

        public async Task<ApiResponse<bool>> DeleteLogoAsync(Guid tenantId, CancellationToken ct = default)
        {
            var tenant = await _tenants.GetByIdAsync(tenantId, ct);
            if (tenant == null) return ApiResponse<bool>.Fail("Tenant not found.");

            if (!string.IsNullOrEmpty(tenant.BusinessLogo))
            {
                await _files.DeleteAsync(tenant.BusinessLogo);
                tenant.BusinessLogo = null;
                _tenants.Update(tenant);
                await _uow.SaveChangesAsync(ct);
            }

            return ApiResponse<bool>.Ok(true, "Logo removed.");
        }

        public async Task<ApiResponse<DashboardSummaryDto>> GetDashboardSummaryAsync(Guid tenantId, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var prevMonthStart = monthStart.AddMonths(-1);
            var prevMonthEnd = monthStart.AddDays(-1);

            var thisRevenue = await _invoices.GetTotalRevenueAsync(tenantId, monthStart, now, ct);
            var prevRevenue = await _invoices.GetTotalRevenueAsync(tenantId, prevMonthStart, prevMonthEnd, ct);
            var thisExpenses = await _expenses.GetTotalExpensesAsync(tenantId, monthStart, now, ct);
            var overdue = (await _invoices.GetOverdueAsync(tenantId, ct)).ToList();
            var lowStock = (await _products.GetLowStockAsync(tenantId, ct)).ToList();
            var recentInsights = (await _insights.GetActiveAsync(tenantId, ct)).Take(5).ToList();

            var revenueGrowth = prevRevenue > 0
                ? Math.Round((thisRevenue - prevRevenue) / prevRevenue * 100, 1)
                : 0;

            var receivables = await _invoices.Query(tenantId)
                .Where(i => i.Status != InvoiceStatus.Cancelled
                         && i.Status != InvoiceStatus.Draft
                         && i.BalanceDue > 0)
                .SumAsync(i => (decimal?)i.BalanceDue, ct) ?? 0;

            var payables = await _purchaseOrders.Query(tenantId)
                .Where(po => po.Status != PurchaseOrderStatus.Cancelled
                          && po.Status != PurchaseOrderStatus.Draft
                          && po.BalanceDue > 0)
                .SumAsync(po => (decimal?)po.BalanceDue, ct) ?? 0;

            var allCustomers = await _customers.CountAsync(c => c.TenantId == tenantId, ct);
            var newCustomers = await _customers.CountAsync(c => c.TenantId == tenantId && c.CreatedAt >= monthStart, ct);
            var totalInvoices = await _invoices.CountAsync(i => i.TenantId == tenantId && i.InvoiceDate >= monthStart, ct);

            var summary = new DashboardSummaryDto(
                thisRevenue, prevRevenue, revenueGrowth,
                thisExpenses, thisRevenue - thisExpenses,
                receivables, payables,
                allCustomers, newCustomers, totalInvoices,
                overdue.Count, lowStock.Count,
                recentInsights.Select(a => new AIInsightDto(
                    a.Id, a.InsightType, a.Title, a.Description,
                    a.Recommendation, a.ImpactValue, a.IsRead, a.GeneratedAt)));

            return ApiResponse<DashboardSummaryDto>.Ok(summary);
        }

        // ── Mapper ────────────────────────────────────────────────────────
        internal static TenantDto MapTenant(Tenant t) => new(
            t.Id, t.BusinessName, t.BusinessLogo, t.LegalName,
            t.GSTIN, t.PAN, t.TAN, t.CIN, t.Email, t.Phone, t.Website,
            new AddressDto(t.AddressLine1, t.AddressLine2, t.City, t.State, t.StateCode, t.PinCode, t.Country),
            t.CurrencyCode, t.InvoicePrefix, t.PurchasePrefix, t.FinancialYearStart,
            t.IsGSTRegistered, t.BankName, t.BankAccountNumber, t.BankIFSC, t.BankBranch, t.UPIId);
    }
}
