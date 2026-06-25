using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.PONumber).IsRequired().HasMaxLength(50);
        b.Property(e => e.Status).HasConversion<string>();
        
        // Decimals
        b.Property(e => e.SubTotal).HasPrecision(18, 2);
        b.Property(e => e.DiscountAmount).HasPrecision(18, 2);
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

        b.HasIndex(e => new { e.TenantId, e.PONumber }).IsUnique().HasFilter("\"IsDeleted\" = false");

        b.HasOne(e => e.Tenant).WithMany(t => t.PurchaseOrders).HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.Supplier).WithMany(s => s.PurchaseOrders).HasForeignKey(e => e.SupplierId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(e => e.Items).WithOne(i => i.PurchaseOrder).HasForeignKey(i => i.PurchaseOrderId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(e => e.Payments).WithOne(p => p.PurchaseOrder).HasForeignKey(p => p.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
    }
}
