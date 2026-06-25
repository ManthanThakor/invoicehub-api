using Core.Enums;

namespace Application.DTOs;

public record AIInsightDto(
    Guid Id,
    InsightType InsightType,
    string Title,
    string Description,
    string? Recommendation,
    decimal? ImpactValue,
    bool IsRead,
    DateTime GeneratedAt
);
