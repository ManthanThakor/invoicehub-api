using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Name).IsRequired().HasMaxLength(150);
        
        b.HasOne(e => e.ParentCategory).WithMany(c => c.SubCategories).HasForeignKey(e => e.ParentCategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}
