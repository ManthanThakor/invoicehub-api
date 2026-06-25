using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Subject).IsRequired().HasMaxLength(250);
        b.Property(e => e.Body).IsRequired();
        b.Property(e => e.Recipient).HasMaxLength(200);
        b.Property(e => e.Type).HasConversion<string>();
        b.Property(e => e.Status).HasConversion<string>();

        b.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.SetNull);
    }
}
