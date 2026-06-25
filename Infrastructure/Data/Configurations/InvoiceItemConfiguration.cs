using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class InvoiceItemConfiguration : IEntityTypeConfiguration<InvoiceItem>
{
    public void Configure(EntityTypeBuilder<InvoiceItem> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Quantity).HasPrecision(18, 4);
        b.Property(e => e.UnitPrice).HasPrecision(18, 4);
        b.Property(e => e.DiscountPercent).HasPrecision(5, 2);
        b.Property(e => e.DiscountAmount).HasPrecision(18, 2);
        b.Property(e => e.TaxableAmount).HasPrecision(18, 2);
        b.Property(e => e.GSTRate).HasPrecision(5, 2);
        b.Property(e => e.IGSTRate).HasPrecision(5, 2);
        b.Property(e => e.IGSTAmount).HasPrecision(18, 2);
        b.Property(e => e.CGSTRate).HasPrecision(5, 2);
        b.Property(e => e.CGSTAmount).HasPrecision(18, 2);
        b.Property(e => e.SGSTRate).HasPrecision(5, 2);
        b.Property(e => e.SGSTAmount).HasPrecision(18, 2);
        b.Property(e => e.CessRate).HasPrecision(5, 2);
        b.Property(e => e.CessAmount).HasPrecision(18, 2);
        b.Property(e => e.TotalAmount).HasPrecision(18, 2);
        b.Property(e => e.Unit).HasConversion<string>();
        b.Property(e => e.HSNCode).HasMaxLength(20);

        b.HasOne(e => e.Product).WithMany(p => p.InvoiceItems)
            .HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}
