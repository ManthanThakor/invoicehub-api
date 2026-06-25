using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
        b.Property(e => e.Action).IsRequired().HasMaxLength(50);
        b.Property(e => e.IpAddress).HasMaxLength(50);
        b.Property(e => e.UserAgent).HasMaxLength(500);

        b.HasOne(e => e.User).WithMany(u => u.AuditLogs).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
