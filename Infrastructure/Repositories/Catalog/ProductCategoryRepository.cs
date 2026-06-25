using Core.Entities;
using Core.Interfaces.Repositories;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class ProductCategoryRepository : Repository<ProductCategory>, IProductCategoryRepository
{
    public ProductCategoryRepository(AppDbContext db) : base(db) { }
}
