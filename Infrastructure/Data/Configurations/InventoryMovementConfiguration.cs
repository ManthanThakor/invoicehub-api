using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Quantity).HasPrecision(18, 4);
        b.Property(e => e.StockBefore).HasPrecision(18, 4);
        b.Property(e => e.StockAfter).HasPrecision(18, 4);
        b.Property(e => e.UnitCost).HasPrecision(18, 4);
        b.Property(e => e.TotalCost).HasPrecision(18, 2);
        b.Property(e => e.MovementType).HasConversion<string>();

        b.HasIndex(e => new { e.ProductId, e.CreatedAt });
        b.HasIndex(e => new { e.TenantId, e.CreatedAt });

        b.HasOne(e => e.Product).WithMany(p => p.InventoryMovements)
            .HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}
