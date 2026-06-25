using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
        b.Property(e => e.LastName).IsRequired().HasMaxLength(100);
        b.Property(e => e.Email).IsRequired().HasMaxLength(200);
        b.Property(e => e.PasswordHash).IsRequired().HasMaxLength(500);
        b.Property(e => e.PhoneNumber).HasMaxLength(20);
        b.Property(e => e.Role).HasConversion<string>();
        b.Property(e => e.Status).HasConversion<string>();

        b.HasIndex(e => e.Email).IsUnique().HasFilter("\"IsDeleted\" = false");
        b.HasIndex(e => new { e.TenantId, e.Email });

        b.HasOne(e => e.Tenant)
            .WithMany(t => t.Users)
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(e => e.RefreshTokens)
            .WithOne(r => r.User)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
