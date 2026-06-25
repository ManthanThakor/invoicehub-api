using Core.Enums;

namespace Application.DTOs;

public record DashboardSummaryDto(
    decimal TotalRevenue,
    decimal TotalRevenuePrev,
    decimal RevenueGrowthPct,
    decimal TotalExpenses,
    decimal NetProfit,
    decimal OutstandingReceivables,
    decimal OutstandingPayables,
    int TotalCustomers,
    int NewCustomersThisPeriod,
    int TotalInvoices,
    int OverdueInvoices,
    int LowStockProducts,
    IEnumerable<AIInsightDto> RecentInsights
);

public record ChartDataDto(string Label, decimal Value, string? Color = null);

public record ProfitLossDto(
    DateTime From,
    DateTime To,
    decimal GrossRevenue,
    decimal CostOfGoods,
    decimal GrossProfit,
    decimal GrossProfitMarginPct,
    decimal TotalExpenses,
    decimal NetProfit,
    decimal NetProfitMarginPct,
    IEnumerable<ChartDataDto> ExpenseBreakdown,
    IEnumerable<ChartDataDto> MonthlyRevenue
);

public record TopCustomerDto(Guid Id, string Name, decimal Revenue, int InvoiceCount, decimal AverageInvoiceValue);
public record TopProductDto(Guid Id, string Name, string? SKU, decimal QuantitySold, decimal Revenue);
