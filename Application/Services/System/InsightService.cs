using Application.DTOs;

using Core.Entities;
using Core.Enums;
using Core.Interfaces.Repositories;

using Microsoft.AspNetCore.Http;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System.Net.Http.Json;

namespace Application.Services.System
{

    public class InsightService : IInsightService
    {
        private readonly IAIInsightRepository _insightRepo;
        private readonly IInvoiceRepository _invoiceRepo;
        private readonly IExpenseRepository _expenseRepo;
        private readonly IProductRepository _productRepo;
        private readonly IUnitOfWork _uow;
        private readonly IHttpClientFactory _httpFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<InsightService> _log;

        // Config shortcuts
        private string ApiKey => _config["AI:ApiKey"] ?? "";
        private string Model => _config["AI:Model"] ?? "llama3-8b-8192";
        private string BaseUrl => _config["AI:BaseUrl"] ?? "https://api.groq.com/openai/v1";
        private bool IsAiConfigured => !string.IsNullOrWhiteSpace(ApiKey) && ApiKey != "my key";

        public InsightService(
            IAIInsightRepository insightRepo,
            IInvoiceRepository invoiceRepo,
            IExpenseRepository expenseRepo,
            IProductRepository productRepo,
            IUnitOfWork uow,
            IHttpClientFactory httpFactory,
            IConfiguration config,
            ILogger<InsightService> log)
        {
            _insightRepo = insightRepo; _invoiceRepo = invoiceRepo;
            _expenseRepo = expenseRepo; _productRepo = productRepo;
            _uow = uow; _httpFactory = httpFactory;
            _config = config; _log = log;
        }

        // ── Generate All Insights ─────────────────────────────────────────
        public async Task GenerateInsightsAsync(Guid tenantId, CancellationToken ct = default)
        {
            _log.LogInformation("Generating AI insights for tenant {TenantId}", tenantId);
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var lastMonthStart = monthStart.AddMonths(-1);
            var lastMonthEnd = monthStart.AddDays(-1);

            // Gather financial data
            var thisRevenue = await _invoiceRepo.GetTotalRevenueAsync(tenantId, monthStart, now, ct);
            var lastRevenue = await _invoiceRepo.GetTotalRevenueAsync(tenantId, lastMonthStart, lastMonthEnd, ct);
            var thisExpenses = await _expenseRepo.GetTotalExpensesAsync(tenantId, monthStart, now, ct);
            var overdue = (await _invoiceRepo.GetOverdueAsync(tenantId, ct)).ToList();
            var lowStock = (await _productRepo.GetLowStockAsync(tenantId, ct)).ToList();
            var expenseByCategory = (await _expenseRepo.GetByCategory(tenantId, monthStart, now, ct)).ToList();

            var revenueGrowth = lastRevenue > 0
                ? Math.Round((thisRevenue - lastRevenue) / lastRevenue * 100, 1) : 0;

            var expenseSummary = string.Join(", ",
                expenseByCategory.Select(e => $"{e.Category}: ₹{e.Total:N0}"));

            var context = $"""
            Business Financial Summary (Current Month):
            - Revenue This Month: ₹{thisRevenue:N0}
            - Revenue Last Month: ₹{lastRevenue:N0}
            - Revenue Growth: {revenueGrowth}%
            - Total Expenses: ₹{thisExpenses:N0}
            - Net Profit: ₹{thisRevenue - thisExpenses:N0}
            - Overdue Invoices: {overdue.Count} worth ₹{overdue.Sum(o => o.BalanceDue):N0}
            - Low Stock Products: {lowStock.Count}
            - Expense Breakdown: {expenseSummary}
            """;

            // Clear old insights of these types before regenerating (prevents duplicates)
            await _insightRepo.ClearInsightTypeAsync(tenantId, InsightType.SalesTrend, ct);
            await _insightRepo.ClearInsightTypeAsync(tenantId, InsightType.ProfitAlert, ct);
            await _insightRepo.ClearInsightTypeAsync(tenantId, InsightType.PaymentReminder, ct);
            await _insightRepo.ClearInsightTypeAsync(tenantId, InsightType.StockAlert, ct);

            // Run insight generators concurrently (non-AI ones return immediately)
            var results = await Task.WhenAll(
                GenerateSalesTrendInsightAsync(tenantId, context, revenueGrowth, thisRevenue, ct),
                GenerateProfitInsightAsync(tenantId, context, thisRevenue, thisExpenses, ct),
                GenerateOverdueInsightAsync(tenantId, overdue, ct),
                GenerateStockAlertInsightAsync(tenantId, lowStock, ct),
                GenerateExpenseInsightAsync(tenantId, context, expenseByCategory, ct));

            foreach (var insight in results.Where(r => r != null))
            {
                await _insightRepo.AddAsync(insight!, ct);
            }

            await _uow.SaveChangesAsync(ct);
            var count = results.Count(r => r != null);
            _log.LogInformation("Generated {Count} insights for tenant {TenantId}", count, tenantId);
        }

        public async Task<ApiResponse<IEnumerable<AIInsightDto>>> GetInsightsAsync(
            Guid tenantId, CancellationToken ct = default)
        {
            var insights = await _insightRepo.GetActiveAsync(tenantId, ct);
            return ApiResponse<IEnumerable<AIInsightDto>>.Ok(insights.Select(MapInsight));
        }

        public async Task<ApiResponse<bool>> MarkReadAsync(
            Guid tenantId, Guid insightId, CancellationToken ct = default)
        {
            var insight = await _insightRepo.GetByIdWithTenantAsync(insightId, tenantId, ct);
            if (insight == null) return ApiResponse<bool>.Fail("Insight not found.");
            insight.IsRead = true;
            await _uow.SaveChangesAsync(ct);
            return ApiResponse<bool>.Ok(true);
        }

        // ── Free-form financial Q&A ───────────────────────────────────────
        public async Task<ApiResponse<string>> AskFinancialQuestionAsync(
            Guid tenantId, string question, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var revenue = await _invoiceRepo.GetTotalRevenueAsync(tenantId, monthStart, now, ct);
            var expenses = await _expenseRepo.GetTotalExpensesAsync(tenantId, monthStart, now, ct);
            var overdue = (await _invoiceRepo.GetOverdueAsync(tenantId, ct)).ToList();

            var systemPrompt = $"""
            You are a concise financial advisor AI for a small Indian business using InvoiceHub.
            Current month data:
            - Revenue: ₹{revenue:N0}
            - Expenses: ₹{expenses:N0}
            - Net Profit: ₹{revenue - expenses:N0}
            - Overdue invoices: {overdue.Count} worth ₹{overdue.Sum(o => o.BalanceDue):N0}
            Answer concisely (max 3 paragraphs). Use ₹ for Indian Rupees.
            """;

            var answer = await CallAIAsync(systemPrompt, question, ct);
            if (answer != null)
                return ApiResponse<string>.Ok(answer);

            // Fallback: built-in response when AI is unavailable
            var fallback = GenerateFallbackAnswer(question, revenue, expenses, overdue);
            return ApiResponse<string>.Ok(fallback);
        }

        // ═════════════════════════════════════════════════════════════════
        //  INDIVIDUAL INSIGHT GENERATORS
        // ═════════════════════════════════════════════════════════════════

        private async Task<AIInsight?> GenerateSalesTrendInsightAsync(
            Guid tenantId, string context, decimal growth, decimal revenue, CancellationToken ct)
        {
            var prompt = $"""
            {context}
            Analyse the revenue trend. Give exactly 2 sentences about sales performance 
            and one specific actionable recommendation.
            Format strictly as: INSIGHT: <text> | RECOMMENDATION: <text>
            """;

            var response = await CallAIAsync(
                "You are a concise financial analyst. Always use the exact format requested.",
                prompt, ct);

            if (response == null)
            {
                var fallback = growth >= 0
                    ? "INSIGHT: Your revenue has grown, indicating strong sales performance this month compared to last month. The positive trend suggests your current marketing and sales strategies are effective. | RECOMMENDATION: Consider investing more in your best-performing product lines and channels to sustain this growth."
                    : "INSIGHT: Your revenue has decreased compared to last month, which may signal a need to review your sales approach. Market conditions or seasonal factors could be contributing to this decline. | RECOMMENDATION: Analyse which products or services saw the biggest drop and run targeted promotions to recover lost ground.";
                var (fbInsight, fbRecommendation) = ParseAIResponse(fallback);
                return new AIInsight
                {
                    TenantId = tenantId,
                    InsightType = InsightType.SalesTrend,
                    Title = growth >= 0 ? $"Revenue Up {growth}% Month-on-Month" : $"Revenue Down {Math.Abs(growth)}%",
                    Description = fbInsight,
                    Recommendation = fbRecommendation,
                    ImpactValue = revenue,
                    GeneratedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(7)
                };
            }

            var (insight, recommendation) = ParseAIResponse(response);
            return new AIInsight
            {
                TenantId = tenantId,
                InsightType = InsightType.SalesTrend,
                Title = growth >= 0 ? $"Revenue Up {growth}% Month-on-Month" : $"Revenue Down {Math.Abs(growth)}%",
                Description = insight,
                Recommendation = recommendation,
                ImpactValue = revenue,
                GeneratedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };
        }

        private async Task<AIInsight?> GenerateProfitInsightAsync(
            Guid tenantId, string context, decimal revenue, decimal expenses, CancellationToken ct)
        {
            var margin = revenue > 0
                ? Math.Round((revenue - expenses) / revenue * 100, 1) : 0;

            var prompt = $"""
            {context}
            Net profit margin this month: {margin}%.
            Give exactly 2 sentences analysing the margin and one recommendation to improve it.
            Format strictly as: INSIGHT: <text> | RECOMMENDATION: <text>
            """;

            var response = await CallAIAsync(
                "You are a concise financial analyst. Always use the exact format requested.",
                prompt, ct);

            if (response == null)
            {
                var fallback = margin >= 20
                    ? $"INSIGHT: Your net profit margin of {margin}% is healthy, indicating efficient cost management relative to revenue. This strong margin provides a good buffer for unexpected expenses or market changes. | RECOMMENDATION: Maintain your current cost control measures while exploring ways to further automate and streamline operations."
                    : margin > 0
                        ? $"INSIGHT: Your net profit margin of {margin}% is positive but has room for improvement. Keeping a close watch on both revenue growth and expense management will be key to expanding this margin. | RECOMMENDATION: Review your top three expense categories for potential savings, and consider renegotiating supplier contracts."
                        : $"INSIGHT: Your net profit margin is negative at {margin}%, meaning expenses currently exceed revenue. This requires immediate attention to avoid cash flow strain. | RECOMMENDATION: Focus on reducing discretionary spending and consider increasing prices or offering premium services to improve margins.";
                var (fbInsight, fbRecommendation) = ParseAIResponse(fallback);
                return new AIInsight
                {
                    TenantId = tenantId,
                    InsightType = InsightType.ProfitAlert,
                    Title = $"Profit Margin: {margin}%",
                    Description = fbInsight,
                    Recommendation = fbRecommendation,
                    ImpactValue = revenue - expenses,
                    GeneratedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(7)
                };
            }

            var (insight, recommendation) = ParseAIResponse(response);
            return new AIInsight
            {
                TenantId = tenantId,
                InsightType = InsightType.ProfitAlert,
                Title = $"Profit Margin: {margin}%",
                Description = insight,
                Recommendation = recommendation,
                ImpactValue = revenue - expenses,
                GeneratedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };
        }

        // Rule-based (no AI needed) — fast and always available
        private static Task<AIInsight?> GenerateOverdueInsightAsync(
            Guid tenantId, List<Invoice> overdue, CancellationToken _)
        {
            if (!overdue.Any()) return Task.FromResult<AIInsight?>(null);

            var total = overdue.Sum(o => o.BalanceDue);
            var oldest = overdue.Min(o => o.DueDate);
            var daysOldest = oldest.HasValue ? (DateTime.UtcNow - oldest.Value).Days : 0;

            return Task.FromResult<AIInsight?>(new AIInsight
            {
                TenantId = tenantId,
                InsightType = InsightType.PaymentReminder,
                Title = $"{overdue.Count} Overdue Invoice{(overdue.Count > 1 ? "s" : "")} — ₹{total:N0} at Risk",
                Description = $"You have {overdue.Count} overdue invoices totalling ₹{total:N0}. " +
                              $"The oldest is {daysOldest} days overdue. " +
                              "Delayed receivables hurt your cash flow significantly.",
                Recommendation = "Send personalised payment reminders today. " +
                                 "For invoices over 60 days overdue, consider calling the customer directly " +
                                 "or offering a structured repayment plan.",
                ImpactValue = total,
                GeneratedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(3)
            });
        }

        private static Task<AIInsight?> GenerateStockAlertInsightAsync(
            Guid tenantId, List<Product> lowStock, CancellationToken _)
        {
            if (!lowStock.Any()) return Task.FromResult<AIInsight?>(null);

            var criticallyLow = lowStock.Where(p => p.CurrentStock == 0).ToList();
            var names = string.Join(", ", lowStock.Take(5).Select(p =>
                $"{p.Name} ({p.CurrentStock} {p.Unit})"));

            return Task.FromResult<AIInsight?>(new AIInsight
            {
                TenantId = tenantId,
                InsightType = InsightType.StockAlert,
                Title = $"{lowStock.Count} Product{(lowStock.Count > 1 ? "s" : "")} Below Minimum Stock",
                Description = $"{criticallyLow.Count} product(s) are completely out of stock. " +
                              $"Products needing attention: {names}.",
                Recommendation = "Raise purchase orders for low-stock items immediately. " +
                                 "Review your reorder quantities for fast-moving products " +
                                 "to avoid future stockouts during peak demand.",
                GeneratedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(2)
            });
        }

        private async Task<AIInsight?> GenerateExpenseInsightAsync(
            Guid tenantId, string context,
            List<(ExpenseCategory Category, decimal Total)> expenses, CancellationToken ct)
        {
            var top = expenses.OrderByDescending(e => e.Total).FirstOrDefault();
            if (top.Total == 0) return null;

            var prompt = $"""
            {context}
            The highest expense category is {top.Category} at ₹{top.Total:N0}.
            In exactly 2 sentences, state whether this is concerning and suggest one cost reduction tactic.
            Format strictly as: INSIGHT: <text> | RECOMMENDATION: <text>
            """;

            var response = await CallAIAsync(
                "You are a concise financial analyst. Always use the exact format requested.",
                prompt, ct);

            if (response == null)
            {
                var fallback = $"INSIGHT: Your highest expense category is {top.Category} at ₹{top.Total:N0}, which represents a significant portion of your total costs. Monitoring this category closely is important to ensure spending remains aligned with your budget. | RECOMMENDATION: Review your {top.Category} expenses in detail to identify any non-essential costs that can be reduced or eliminated.";
                var (fbInsight, fbRecommendation) = ParseAIResponse(fallback);
                return new AIInsight
                {
                    TenantId = tenantId,
                    InsightType = InsightType.ProfitAlert,
                    Title = $"Highest Expense: {top.Category} — ₹{top.Total:N0}",
                    Description = fbInsight,
                    Recommendation = fbRecommendation,
                    ImpactValue = top.Total,
                    GeneratedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(7)
                };
            }

            var (insight, recommendation) = ParseAIResponse(response);
            return new AIInsight
            {
                TenantId = tenantId,
                InsightType = InsightType.ProfitAlert,
                Title = $"Highest Expense: {top.Category} — ₹{top.Total:N0}",
                Description = insight,
                Recommendation = recommendation,
                ImpactValue = top.Total,
                GeneratedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };
        }

        // ═════════════════════════════════════════════════════════════════
        //  AI API CALLER — OpenAI-compatible (Groq / OpenRouter / Ollama)
        // ═════════════════════════════════════════════════════════════════

        private async Task<string?> CallAIAsync(
            string systemPrompt, string userPrompt, CancellationToken ct)
        {
            if (!IsAiConfigured)
            {
                _log.LogWarning("AI API key not configured. Using fallback responses.");
                return null;
            }

            try
            {
                var client = _httpFactory.CreateClient("AI");
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");

                var body = new
                {
                    model = Model,
                    messages = new[]
                    {
                    new { role = "system", content = systemPrompt },
                    new { role = "user",   content = userPrompt   }
                },
                    max_tokens = 400,
                    temperature = 0.3
                };

                var response = await client.PostAsJsonAsync("chat/completions", body, ct);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(ct);
                    _log.LogError("Groq API error {Status}: {Error}", response.StatusCode, error);
                    return null;
                }

                var result = await response.Content.ReadFromJsonAsync<GroqResponse>(
                    cancellationToken: ct);
                return result?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "AI API call failed");
                return null;
            }
        }

        // Parse "INSIGHT: ... | RECOMMENDATION: ..." format
        private static (string Insight, string Recommendation) ParseAIResponse(string response)
        {
            if (response.Contains("INSIGHT:") && response.Contains("RECOMMENDATION:"))
            {
                var parts = response.Split('|');
                return (
                    parts[0].Replace("INSIGHT:", "").Trim(),
                    parts.Length > 1 ? parts[1].Replace("RECOMMENDATION:", "").Trim() : ""
                );
            }

            // Fallback: use full response as insight
            return (response.Trim(), "");
        }

        private static string GenerateFallbackAnswer(
            string question, decimal revenue, decimal expenses, List<Invoice> overdue)
        {
            var q = question.ToLowerInvariant();
            var profit = revenue - expenses;
            var overdueTotal = overdue.Sum(o => o.BalanceDue);

            if (q.Contains("profit") || q.Contains("margin"))
            {
                var margin = revenue > 0 ? Math.Round(profit / revenue * 100, 1) : 0;
                return $"Your current net profit is ₹{profit:N0} with a profit margin of {margin}%. " +
                       $"Revenue is ₹{revenue:N0} and expenses are ₹{expenses:N0}. " +
                       (margin < 10
                           ? "Consider reviewing your top expense categories to improve margins."
                           : "Your margins are looking healthy. Keep monitoring your expenses to maintain this.");
            }

            if (q.Contains("revenue") || q.Contains("sale") || q.Contains("income"))
            {
                return $"Your current month revenue is ₹{revenue:N0}. Your expenses are ₹{expenses:N0}, " +
                       $"leaving a net profit of ₹{profit:N0}. " +
                       (revenue > 0
                           ? "Revenue is being generated — focus on your top-selling products or services to grow further."
                           : "No revenue recorded yet this month. Consider reaching out to customers or running promotions.");
            }

            if (q.Contains("expense") || q.Contains("spend") || q.Contains("cost"))
            {
                return $"Your total expenses this month are ₹{expenses:N0} against revenue of ₹{revenue:N0}. " +
                       (expenses > revenue
                           ? "Expenses are exceeding revenue. Review discretionary spending and renegotiate supplier terms where possible."
                           : $"Expenses are well within revenue, leaving a profit of ₹{profit:N0}. Keep up the good cost management.");
            }

            if (q.Contains("overdue") || q.Contains("outstanding") || q.Contains("pending"))
            {
                if (overdue.Count > 0)
                    return $"You have {overdue.Count} overdue invoice(s) totalling ₹{overdueTotal:N0}. " +
                           "Send payment reminders immediately and consider calling customers with the largest outstanding amounts.";
                return "Great news — you have no overdue invoices right now. All payments are on track.";
            }

            if (q.Contains("cash flow") || q.Contains("cashflow"))
            {
                return $"Your current cash position shows revenue of ₹{revenue:N0} and expenses of ₹{expenses:N0}, " +
                       $"resulting in a net cash flow of ₹{profit:N0}. " +
                       (overdue.Count > 0
                           ? $" Collecting the ₹{overdueTotal:N0} in overdue payments would significantly improve your cash flow."
                           : " Your cash flow is positive. Consider setting aside a portion for future investments.");
            }

            return $"Based on your current data: Revenue is ₹{revenue:N0}, expenses are ₹{expenses:N0}, " +
                   $"net profit is ₹{profit:N0}, and you have {overdue.Count} overdue invoice(s) worth ₹{overdueTotal:N0}. " +
                   "What specific area would you like to explore further?";
        }

        private static AIInsightDto MapInsight(AIInsight a) => new(
            a.Id, a.InsightType, a.Title, a.Description,
            a.Recommendation, a.ImpactValue, a.IsRead, a.GeneratedAt);
    }
}
