using Core.Common;
using Core.Enums;

namespace Core.Entities;

public class AIInsight : BaseEntity
{
    public InsightType InsightType { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string? DataJson { get; set; }      // Serialised chart/table data
    public string? Recommendation { get; set; }
    public decimal? ImpactValue { get; set; }  // e.g. revenue opportunity in INR
    public bool IsRead { get; set; } = false;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }


    public Tenant Tenant { get; set; } = null!;
}
