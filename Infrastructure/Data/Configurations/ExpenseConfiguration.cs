using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Title).IsRequired().HasMaxLength(200);
        b.Property(e => e.Amount).HasPrecision(18, 2);
        b.Property(e => e.GSTAmount).HasPrecision(18, 2);
        b.Property(e => e.TotalAmount).HasPrecision(18, 2);
        b.Property(e => e.Category).HasConversion<string>();
        b.Property(e => e.PaymentMethod).HasConversion<string>();

        b.HasOne(e => e.Tenant).WithMany(t => t.Expenses).HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.RecordedByUser).WithMany().HasForeignKey(e => e.RecordedBy).OnDelete(DeleteBehavior.SetNull);
    }
}
