using Core.Enums;
using InvoiceHub.Application.DTOs.Finance;

namespace Application.Services.Utilities
{
    public interface IGSTCalculationService
    {
        GSTLineResult CalculateLine(decimal quantity, decimal unitPrice, decimal discountPercent,
            decimal gstRate, decimal? cessRate, bool isInterState);
        GSTTotals CalculateTotals(IEnumerable<GSTLineResult> lines, decimal? headerDiscountAmount,
            DiscountType discountType, decimal? headerDiscountPercent, bool isInterState);
        string NumberToWords(decimal amount);
    }
}