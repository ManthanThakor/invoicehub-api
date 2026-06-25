using Application.DTOs;
using Application.Services.System;
using Core.Entities;
using Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Services.Catalog;

public class ProductCategoryService : IProductCategoryService
{
    private readonly IProductCategoryRepository _categories;
    private readonly IAuditService _audit;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ProductCategoryService> _log;

    public ProductCategoryService(
        IProductCategoryRepository categories,
        IAuditService audit, IUnitOfWork uow,
        ILogger<ProductCategoryService> log)
    {
        _categories = categories; _audit = audit;
        _uow = uow; _log = log;
    }

    public async Task<ApiResponse<IEnumerable<ProductCategoryDto>>> GetAllAsync(
        Guid tenantId, CancellationToken ct = default)
    {
        var categories = await _categories.Query(tenantId)
            .Include(c => c.ParentCategory)
            .OrderBy(c => c.Name)
            .Select(c => MapCategory(c))
            .ToListAsync(ct);

        return ApiResponse<IEnumerable<ProductCategoryDto>>.Ok(categories);
    }

    public async Task<ApiResponse<ProductCategoryDto>> GetAsync(
        Guid tenantId, Guid id, CancellationToken ct = default)
    {
        var category = await _categories.Query(tenantId)
            .Include(c => c.ParentCategory)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (category == null)
            return ApiResponse<ProductCategoryDto>.Fail("Category not found.");

        return ApiResponse<ProductCategoryDto>.Ok(MapCategory(category));
    }

    public async Task<ApiResponse<ProductCategoryDto>> CreateAsync(
        Guid tenantId, Guid userId, CreateProductCategoryDto dto, CancellationToken ct = default)
    {
        var category = new ProductCategory
        {
            TenantId = tenantId,
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            ParentCategoryId = dto.ParentCategoryId
        };

        await _categories.AddAsync(category, ct);
        await _uow.SaveChangesAsync(ct);

        await _audit.LogAsync(tenantId, userId, "ProductCategory", category.Id, "Create", ct: ct);
        _log.LogInformation("Product category created: {Name} for tenant {TenantId}", category.Name, tenantId);

        return ApiResponse<ProductCategoryDto>.Ok(MapCategory(category), "Category created.");
    }

    public async Task<ApiResponse<ProductCategoryDto>> UpdateAsync(
        Guid tenantId, Guid id, UpdateProductCategoryDto dto, CancellationToken ct = default)
    {
        var category = await _categories.GetByIdWithTenantAsync(id, tenantId, ct);
        if (category == null)
            return ApiResponse<ProductCategoryDto>.Fail("Category not found.");

        category.Name = dto.Name.Trim();
        category.Description = dto.Description?.Trim();
        category.ParentCategoryId = dto.ParentCategoryId;

        _categories.Update(category);
        await _uow.SaveChangesAsync(ct);

        return ApiResponse<ProductCategoryDto>.Ok(MapCategory(category), "Category updated.");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(
        Guid tenantId, Guid id, CancellationToken ct = default)
    {
        var category = await _categories.GetByIdWithTenantAsync(id, tenantId, ct);
        if (category == null)
            return ApiResponse<bool>.Fail("Category not found.");

        _categories.SoftDelete(category);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<bool>.Ok(true, "Category deleted.");
    }

    public async Task<ApiResponse<IEnumerable<SelectOptionDto>>> SearchAsync(
        Guid tenantId, string term, CancellationToken ct = default)
    {
        var categories = await _categories.Query(tenantId)
            .Where(c => c.Name.Contains(term))
            .OrderBy(c => c.Name)
            .Take(20)
            .Select(c => new SelectOptionDto(c.Id, c.Name, c.Description))
            .ToListAsync(ct);

        return ApiResponse<IEnumerable<SelectOptionDto>>.Ok(categories);
    }

    private static ProductCategoryDto MapCategory(ProductCategory c) => new(
        c.Id, c.Name, c.Description, c.ParentCategoryId,
        c.ParentCategory?.Name, c.CreatedAt);
}
