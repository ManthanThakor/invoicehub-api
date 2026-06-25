using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class AIInsightConfiguration : IEntityTypeConfiguration<AIInsight>
{
    public void Configure(EntityTypeBuilder<AIInsight> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Title).IsRequired().HasMaxLength(250);
        b.Property(e => e.Description).IsRequired();
        b.Property(e => e.ImpactValue).HasPrecision(18, 2);
        b.Property(e => e.InsightType).HasConversion<string>();

        b.HasOne(e => e.Tenant).WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}
