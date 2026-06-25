using Core.Common;
using Core.Enums;

namespace Core.Entities;

public class ProductCategory : BaseEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public ProductCategory? ParentCategory { get; set; }
    public ICollection<ProductCategory> SubCategories { get; set; } = new List<ProductCategory>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
