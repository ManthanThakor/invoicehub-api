using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Name).IsRequired().HasMaxLength(200);
        b.Property(e => e.Email).HasMaxLength(200);
        b.Property(e => e.Phone).HasMaxLength(20);
        b.Property(e => e.GSTIN).HasMaxLength(15);
        b.Property(e => e.PAN).HasMaxLength(10);
        b.Property(e => e.CreditLimit).HasPrecision(18, 2);
        b.Property(e => e.CustomerType).HasConversion<string>();
        b.Property(e => e.Status).HasConversion<string>();

        b.HasIndex(e => new { e.TenantId, e.Name });
        b.HasIndex(e => new { e.TenantId, e.GSTIN }).HasFilter("\"GSTIN\" IS NOT NULL");

        b.HasOne(e => e.Tenant).WithMany(t => t.Customers)
            .HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}
