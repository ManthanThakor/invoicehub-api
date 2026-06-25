using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Drawing;
using Color = System.Drawing.Color;
using System.Collections.Generic;
using System.Text;

namespace Application.Services.System
{

    internal static class ExcelHelper
    {
        /// <summary>Style row-1 as a bold indigo header.</summary>
        internal static void WriteHeader(ExcelWorksheet sheet, string[] headers)
        {
            for (var i = 0; i < headers.Length; i++)
            {
                var cell = sheet.Cells[1, i + 1];
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.Color.SetColor(Color.White);
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 79, 70, 229));   // indigo-600
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }
            sheet.Row(1).Height = 22;
        }

        /// <summary>Currency format  ₹#,##0.00  on a single cell.</summary>
        internal static void SetCurrency(ExcelRange cell)
            => cell.Style.Numberformat.Format = "\u20b9#,##0.00";   // ₹ unicode

        /// <summary>Auto-fit every column in the used range.</summary>
        internal static void AutoFit(ExcelWorksheet sheet)
        {
            if (sheet.Dimension != null)
                sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        }
    }
}
