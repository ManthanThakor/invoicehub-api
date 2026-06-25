using Core.Enums;
using InvoiceHub.Application.DTOs.Finance;

namespace Application.Services.Utilities
{
    public class GSTCalculationService : IGSTCalculationService
    {
        // ── Per-line Calculation ──────────────────────────────────────────
        public GSTLineResult CalculateLine(
            decimal quantity,
            decimal unitPrice,
            decimal discountPercent,
            decimal gstRate,
            decimal? cessRate,
            bool isInterState)
        {
            var gross = Math.Round(quantity * unitPrice, 2);
            var discountAmount = Math.Round(gross * discountPercent / 100m, 2);
            var taxableAmount = gross - discountAmount;

            decimal igst = 0, cgst = 0, sgst = 0, cess = 0;
            decimal igstRate = 0, cgstRate = 0, sgstRate = 0;

            if (isInterState)
            {
                // Inter-state: full IGST
                igstRate = gstRate;
                igst = Math.Round(taxableAmount * igstRate / 100m, 2);
            }
            else
            {
                // Intra-state: split equally into CGST + SGST
                cgstRate = gstRate / 2m;
                sgstRate = gstRate / 2m;
                cgst = Math.Round(taxableAmount * cgstRate / 100m, 2);
                sgst = Math.Round(taxableAmount * sgstRate / 100m, 2);
            }

            if (cessRate.HasValue && cessRate > 0)
                cess = Math.Round(taxableAmount * cessRate.Value / 100m, 2);

            var totalAmount = taxableAmount + igst + cgst + sgst + cess;

            return new GSTLineResult(
                taxableAmount, igst, cgst, sgst, cess,
                Math.Round(totalAmount, 2), discountAmount,
                igstRate, cgstRate, sgstRate);
        }

        // ── Invoice-level Totals ──────────────────────────────────────────
        public GSTTotals CalculateTotals(
            IEnumerable<GSTLineResult> lines,
            decimal? headerDiscountAmount,
            DiscountType discountType,
            decimal? headerDiscountPercent,
            bool isInterState)
        {
            var lineList = lines.ToList();

            // SubTotal = sum of (taxable + discount) per line = pre-discount total
            var subTotal = lineList.Sum(l => l.TaxableAmount + l.DiscountAmount);
            var lineLevelDiscount = lineList.Sum(l => l.DiscountAmount);
            var afterLineDiscount = subTotal - lineLevelDiscount;

            // Header-level discount (applied on top of line discounts)
            decimal headerDiscount = discountType switch
            {
                DiscountType.FixedAmount when headerDiscountAmount.HasValue
                    => headerDiscountAmount.Value,
                DiscountType.Percentage when headerDiscountPercent.HasValue
                    => Math.Round(afterLineDiscount * headerDiscountPercent.Value / 100m, 2),
                _ => 0
            };

            var taxableAmount = afterLineDiscount - headerDiscount;

            // Re-compute tax on final taxable amount using weighted average rates
            decimal igst = 0, cgst = 0, sgst = 0, cess = 0;
            var totalLineGross = lineList.Sum(l => l.TaxableAmount);

            if (totalLineGross > 0 && taxableAmount > 0)
            {
                // Weighted avg rates (handles mixed-GST-rate line items)
                var avgIGSTRate = isInterState
                    ? lineList.Sum(l => l.IGSTAmount) / totalLineGross * 100m : 0;
                var avgCGSTRate = !isInterState
                    ? lineList.Sum(l => l.CGSTAmount) / totalLineGross * 100m : 0;
                var avgSGSTRate = !isInterState
                    ? lineList.Sum(l => l.SGSTAmount) / totalLineGross * 100m : 0;
                var avgCessRate = lineList.Sum(l => l.CessAmount) / totalLineGross * 100m;

                igst = Math.Round(taxableAmount * avgIGSTRate / 100m, 2);
                cgst = Math.Round(taxableAmount * avgCGSTRate / 100m, 2);
                sgst = Math.Round(taxableAmount * avgSGSTRate / 100m, 2);
                cess = Math.Round(taxableAmount * avgCessRate / 100m, 2);
            }

            var totalTax = igst + cgst + sgst + cess;
            var grandBeforeRound = taxableAmount + totalTax;
            var grandRounded = Math.Round(grandBeforeRound, 0, MidpointRounding.AwayFromZero);
            var roundOff = grandRounded - grandBeforeRound;

            return new GSTTotals(
                subTotal,
                lineLevelDiscount + headerDiscount,
                taxableAmount,
                igst, cgst, sgst, cess,
                totalTax,
                Math.Round(roundOff, 2),
                grandRounded);
        }

        // ── Amount in Words (for invoice footer) ─────────────────────────
        public string NumberToWords(decimal amount)
        {
            var intPart = (long)Math.Floor(amount);
            var decPart = (int)Math.Round((amount - intPart) * 100);

            var result = $"Rupees {ConvertToWords(intPart)}";
            if (decPart > 0)
                result += $" and {ConvertToWords(decPart)} Paise";
            result += " Only";

            return result;
        }

        private static string ConvertToWords(long number)
        {
            if (number == 0) return "Zero";

            string[] ones =
            {
            "", "One", "Two", "Three", "Four", "Five", "Six", "Seven",
            "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen",
            "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen"
        };
            string[] tens =
            {
            "", "", "Twenty", "Thirty", "Forty", "Fifty",
            "Sixty", "Seventy", "Eighty", "Ninety"
        };

            if (number < 0) return "Minus " + ConvertToWords(-number);
            if (number < 20) return ones[number];
            if (number < 100) return tens[number / 10] + (number % 10 > 0 ? " " + ones[number % 10] : "");
            if (number < 1_000) return ones[number / 100] + " Hundred" + (number % 100 > 0 ? " " + ConvertToWords(number % 100) : "");
            if (number < 1_00_000) return ConvertToWords(number / 1_000) + " Thousand" + (number % 1_000 > 0 ? " " + ConvertToWords(number % 1_000) : "");
            if (number < 1_00_00_000) return ConvertToWords(number / 1_00_000) + " Lakh" + (number % 1_00_000 > 0 ? " " + ConvertToWords(number % 1_00_000) : "");
            return ConvertToWords(number / 1_00_00_000) + " Crore" + (number % 1_00_00_000 > 0 ? " " + ConvertToWords(number % 1_00_00_000) : "");
        }
    }
}
