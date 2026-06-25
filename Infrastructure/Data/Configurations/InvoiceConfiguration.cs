using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.InvoiceNumber).IsRequired().HasMaxLength(50);
        b.Property(e => e.SubTotal).HasPrecision(18, 2);
        b.Property(e => e.DiscountAmount).HasPrecision(18, 2);
        b.Property(e => e.DiscountPercent).HasPrecision(5, 2);
        b.Property(e => e.TaxableAmount).HasPrecision(18, 2);
        b.Property(e => e.IGSTAmount).HasPrecision(18, 2);
        b.Property(e => e.CGSTAmount).HasPrecision(18, 2);
        b.Property(e => e.SGSTAmount).HasPrecision(18, 2);
        b.Property(e => e.CessAmount).HasPrecision(18, 2);
        b.Property(e => e.TotalTaxAmount).HasPrecision(18, 2);
        b.Property(e => e.RoundOff).HasPrecision(5, 2);
        b.Property(e => e.GrandTotal).HasPrecision(18, 2);
        b.Property(e => e.PaidAmount).HasPrecision(18, 2);
        b.Property(e => e.BalanceDue).HasPrecision(18, 2);
        b.Property(e => e.Status).HasConversion<string>();
        b.Property(e => e.DiscountType).HasConversion<string>();

        b.HasIndex(e => new { e.TenantId, e.InvoiceNumber }).IsUnique().HasFilter("\"IsDeleted\" = false");
        b.HasIndex(e => new { e.TenantId, e.InvoiceDate });
        b.HasIndex(e => new { e.TenantId, e.Status });
        b.HasIndex(e => new { e.TenantId, e.DueDate });

        b.HasOne(e => e.Tenant).WithMany(t => t.Invoices)
            .HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.Customer).WithMany(c => c.Invoices)
            .HasForeignKey(e => e.CustomerId).OnDelete(DeleteBehavior.Restrict);

        b.HasMany(e => e.Items).WithOne(i => i.Invoice)
            .HasForeignKey(i => i.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(e => e.Payments).WithOne(p => p.Invoice)
            .HasForeignKey(p => p.InvoiceId).OnDelete(DeleteBehavior.Restrict);
    }
}
