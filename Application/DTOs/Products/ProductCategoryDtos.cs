namespace Application.DTOs;

public record ProductCategoryDto(
    Guid Id,
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    string? ParentCategoryName,
    DateTime CreatedAt
);

public record CreateProductCategoryDto(
    string Name,
    string? Description = null,
    Guid? ParentCategoryId = null
);

public record UpdateProductCategoryDto(
    string Name,
    string? Description = null,
    Guid? ParentCategoryId = null
);
