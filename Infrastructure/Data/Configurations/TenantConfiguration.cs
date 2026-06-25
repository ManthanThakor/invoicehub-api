using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.BusinessName).IsRequired().HasMaxLength(200);
        b.Property(e => e.GSTIN).HasMaxLength(15);
        b.Property(e => e.PAN).HasMaxLength(10);
        b.Property(e => e.Email).HasMaxLength(200);
        b.Property(e => e.Phone).HasMaxLength(20);
        b.Property(e => e.CurrencyCode).HasMaxLength(3).HasDefaultValue("INR");
        b.Property(e => e.InvoicePrefix).HasMaxLength(10);
        b.Property(e => e.PurchasePrefix).HasMaxLength(10);
        b.HasIndex(e => e.GSTIN).IsUnique().HasFilter("\"GSTIN\" IS NOT NULL AND \"IsDeleted\" = false");
    }
}
