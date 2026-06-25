using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class CreditNoteConfiguration : IEntityTypeConfiguration<CreditNote>
{
    public void Configure(EntityTypeBuilder<CreditNote> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.CreditNoteNumber).IsRequired().HasMaxLength(50);
        b.Property(e => e.Amount).HasPrecision(18, 2);
        b.Property(e => e.TaxAmount).HasPrecision(18, 2);
        b.Property(e => e.TotalAmount).HasPrecision(18, 2);

        b.HasOne(e => e.Invoice).WithMany(i => i.CreditNotes).HasForeignKey(e => e.InvoiceId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.Customer).WithMany().HasForeignKey(e => e.CustomerId).OnDelete(DeleteBehavior.Restrict);
    }
}
