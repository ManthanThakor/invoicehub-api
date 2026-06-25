using Application.DTOs;

namespace Application.Services.System
{
    public interface IInsightService
    {
        Task GenerateInsightsAsync(Guid tenantId, CancellationToken ct = default);
        Task<ApiResponse<IEnumerable<AIInsightDto>>> GetInsightsAsync(Guid tenantId, CancellationToken ct = default);
        Task<ApiResponse<bool>> MarkReadAsync(Guid tenantId, Guid insightId, CancellationToken ct = default);
        Task<ApiResponse<string>> AskFinancialQuestionAsync(Guid tenantId, string question, CancellationToken ct = default);
    }
}