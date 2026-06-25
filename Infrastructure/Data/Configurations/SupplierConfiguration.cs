using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Name).IsRequired().HasMaxLength(200);
        b.Property(e => e.Email).HasMaxLength(200);
        b.Property(e => e.Phone).HasMaxLength(20);
        b.Property(e => e.GSTIN).HasMaxLength(15);
        b.Property(e => e.PAN).HasMaxLength(10);
        b.Property(e => e.Status).HasConversion<string>();

        b.HasIndex(e => new { e.TenantId, e.Name });
        b.HasOne(e => e.Tenant).WithMany(t => t.Suppliers).HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}
