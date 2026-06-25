using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Name).IsRequired().HasMaxLength(300);
        b.Property(e => e.SKU).HasMaxLength(100);
        b.Property(e => e.HSNCode).HasMaxLength(20);
        b.Property(e => e.Barcode).HasMaxLength(100);
        b.Property(e => e.PurchasePrice).HasPrecision(18, 4);
        b.Property(e => e.SalePrice).HasPrecision(18, 4);
        b.Property(e => e.MRP).HasPrecision(18, 4);
        b.Property(e => e.GSTRate).HasPrecision(5, 2);
        b.Property(e => e.CessRate).HasPrecision(5, 2);
        b.Property(e => e.CurrentStock).HasPrecision(18, 4);
        b.Property(e => e.MinimumStock).HasPrecision(18, 4);
        b.Property(e => e.ReorderQty).HasPrecision(18, 4);
        b.Property(e => e.ProductType).HasConversion<string>();
        b.Property(e => e.Unit).HasConversion<string>();

        b.HasIndex(e => new { e.TenantId, e.SKU }).IsUnique().HasFilter("\"SKU\" IS NOT NULL AND \"IsDeleted\" = false");
        b.HasIndex(e => new { e.TenantId, e.HSNCode });

        b.HasOne(e => e.Tenant).WithMany(t => t.Products)
            .HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(e => e.Category).WithMany(c => c.Products)
            .HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.SetNull);
    }
}
