using Application.DTOs;
using Application.Services.System;
using Core.Entities;
using Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Services.Finance
{
    public class ExpenseService : IExpenseService
    {
        private readonly IExpenseRepository _expenses;
        private readonly IFileService _files;
        private readonly IAuditService _audit;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<ExpenseService> _log;

        public ExpenseService(
            IExpenseRepository expenses, IFileService files,
            IAuditService audit, IUnitOfWork uow, ILogger<ExpenseService> log)
        {
            _expenses = expenses; _files = files;
            _audit = audit; _uow = uow; _log = log;
        }

        public async Task<ApiResponse<PagedResult<ExpenseListDto>>> GetListAsync(
            Guid tenantId, ExpenseFilterDto filter, CancellationToken ct = default)
        {
            var query = _expenses.Query(tenantId).AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Search))
                query = query.Where(e =>
                    e.Title.Contains(filter.Search) ||
                    (e.VendorName != null && e.VendorName.Contains(filter.Search)));

            if (filter.Category.HasValue) query = query.Where(e => e.Category == filter.Category);
            if (filter.FromDate.HasValue) query = query.Where(e => e.ExpenseDate >= filter.FromDate);
            if (filter.ToDate.HasValue) query = query.Where(e => e.ExpenseDate <= filter.ToDate);

            query = filter.SortDesc
                ? query.OrderByDescending(e => e.ExpenseDate)
                : query.OrderBy(e => e.ExpenseDate);

            var total = await query.CountAsync(ct);
            var items = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(e => new ExpenseListDto(
                    e.Id, e.Title, e.Category, e.ExpenseDate,
                    e.TotalAmount, e.PaymentMethod, e.VendorName, e.ReferenceNumber))
                .ToListAsync(ct);

            return ApiResponse<PagedResult<ExpenseListDto>>.Ok(
                new PagedResult<ExpenseListDto>(items, total, filter.Page, filter.PageSize));
        }

        public async Task<ApiResponse<ExpenseDto>> GetAsync(
            Guid tenantId, Guid id, CancellationToken ct = default)
        {
            var expense = await _expenses.GetByIdWithTenantAsync(id, tenantId, ct);
            if (expense == null) return ApiResponse<ExpenseDto>.Fail("Expense not found.");
            return ApiResponse<ExpenseDto>.Ok(MapExpense(expense));
        }

        public async Task<ApiResponse<ExpenseDto>> CreateAsync(
            Guid tenantId, Guid userId, CreateExpenseDto dto, CancellationToken ct = default)
        {
            var expense = new Expense
            {
                TenantId = tenantId,
                Title = dto.Title,
                Category = dto.Category,
                ExpenseDate = dto.ExpenseDate,
                Amount = dto.Amount,
                GSTAmount = dto.GSTAmount,
                TotalAmount = dto.Amount + (dto.GSTAmount ?? 0),
                PaymentMethod = dto.PaymentMethod,
                VendorName = dto.VendorName,
                ReferenceNumber = dto.ReferenceNumber,
                Notes = dto.Notes,
                RecordedBy = userId
            };

            await _expenses.AddAsync(expense, ct);
            await _uow.SaveChangesAsync(ct);

            await _audit.LogAsync(tenantId, userId, "Expense", expense.Id, "Create", ct: ct);
            _log.LogInformation("Expense recorded: {Title} ₹{Amount} for tenant {TenantId}",
                expense.Title, expense.TotalAmount, tenantId);

            return ApiResponse<ExpenseDto>.Ok(MapExpense(expense), "Expense recorded.");
        }

        public async Task<ApiResponse<ExpenseDto>> UpdateAsync(
            Guid tenantId, Guid id, UpdateExpenseDto dto, CancellationToken ct = default)
        {
            var expense = await _expenses.GetByIdWithTenantAsync(id, tenantId, ct);
            if (expense == null) return ApiResponse<ExpenseDto>.Fail("Expense not found.");

            expense.Title = dto.Title; expense.Category = dto.Category;
            expense.ExpenseDate = dto.ExpenseDate; expense.Amount = dto.Amount;
            expense.GSTAmount = dto.GSTAmount;
            expense.TotalAmount = dto.Amount + (dto.GSTAmount ?? 0);
            expense.PaymentMethod = dto.PaymentMethod;
            expense.VendorName = dto.VendorName;
            expense.ReferenceNumber = dto.ReferenceNumber;
            expense.Notes = dto.Notes;

            _expenses.Update(expense);
            await _uow.SaveChangesAsync(ct);
            return ApiResponse<ExpenseDto>.Ok(MapExpense(expense), "Expense updated.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(
            Guid tenantId, Guid id, CancellationToken ct = default)
        {
            var expense = await _expenses.GetByIdWithTenantAsync(id, tenantId, ct);
            if (expense == null) return ApiResponse<bool>.Fail("Expense not found.");

            // Delete receipt file if exists
            if (!string.IsNullOrEmpty(expense.ReceiptUrl))
                await _files.DeleteAsync(expense.ReceiptUrl);

            _expenses.SoftDelete(expense);
            await _uow.SaveChangesAsync(ct);
            return ApiResponse<bool>.Ok(true, "Expense deleted.");
        }

        public async Task<ApiResponse<string>> UploadReceiptAsync(
            Guid tenantId, Guid id, IFormFile file, CancellationToken ct = default)
        {
            var expense = await _expenses.GetByIdWithTenantAsync(id, tenantId, ct);
            if (expense == null) return ApiResponse<string>.Fail("Expense not found.");

            if (!_files.IsValidDocument(file))
                return ApiResponse<string>.Fail(
                    "Invalid file. Allowed: JPG, PNG, PDF (max 10MB).");

            if (!string.IsNullOrEmpty(expense.ReceiptUrl))
                await _files.DeleteAsync(expense.ReceiptUrl);

            var path = await _files.SaveAsync(file, $"receipts/{tenantId}", ct);
            expense.ReceiptUrl = path;
            _expenses.Update(expense);
            await _uow.SaveChangesAsync(ct);

            return ApiResponse<string>.Ok(_files.GetPublicUrl(path), "Receipt uploaded.");
        }

        // ── Mapper ────────────────────────────────────────────────────────
        private static ExpenseDto MapExpense(Expense e) => new(
            e.Id, e.Title, e.Category, e.ExpenseDate,
            e.Amount, e.GSTAmount, e.TotalAmount,
            e.PaymentMethod, e.VendorName, e.ReferenceNumber,
            e.Notes, e.ReceiptUrl, e.CreatedAt);
    }
}
