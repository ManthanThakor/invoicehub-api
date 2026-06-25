using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.PaymentNumber).IsRequired().HasMaxLength(50);
        b.Property(e => e.Amount).HasPrecision(18, 2);
        b.Property(e => e.Method).HasConversion<string>();
        b.Property(e => e.Status).HasConversion<string>();
        b.Property(e => e.ReferenceNumber).HasMaxLength(100);

        b.HasIndex(e => new { e.TenantId, e.PaymentDate });
        b.HasIndex(e => e.InvoiceId).HasFilter("\"InvoiceId\" IS NOT NULL");
    }
}
