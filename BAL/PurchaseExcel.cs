using ClosedXML.Excel;
using RiceMillProject.Models;

namespace RiceMillProject.BAL;

public static class PurchaseExcel
{
    public static byte[] Export(PurchasePage page, PurchaseTable table)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Purchase report");
        sheet.Cell(1, 1).Value = page.Title;
        sheet.Range(1, 1, 1, table.ColumnCount).Merge().Style.Font.SetBold().Font.SetFontSize(15);
        sheet.Cell(2, 1).Value = page.Filter.Mode is "Locations" or "LocationDetails" ? "All recorded purchase unloadings" : $"Weighing date: {page.Filter.From:dd-MM-yyyy} to {page.Filter.To:dd-MM-yyyy}";
        sheet.Cell(2, 1).Value = sheet.Cell(2, 1).GetString() + " | Company: " +
            (page.Companies.FirstOrDefault(c => c.Id == page.Filter.CompanyId)?.Name ?? "All companies") +
            (page.Filter.Mode == "Party" ? " | Party: " + (page.Parties.FirstOrDefault(p => p.Id == page.Filter.PartyId)?.Name ?? "All parties") : "");
        sheet.Range(2, 1, 2, table.ColumnCount).Merge();
        sheet.Cell(3, 1).Value = "* Weight allocated by bag count; actual weights are used where recorded. Lab deduction is a rate with unit, not a final billed amount.";
        sheet.Range(3, 1, 3, table.ColumnCount).Merge();
        var occupied = new HashSet<(int Row, int Col)>();
        for (var r = 0; r < table.Rows.Count; r++)
        {
            var col = 1;
            foreach (var source in table.Rows[r])
            {
                while (occupied.Contains((r, col))) col++;
                var cell = sheet.Cell(r + 5, col);
                // Strings remain text, including phone numbers and formula-looking party names.
                cell.Value = source.Value switch
                {
                    decimal n => XLCellValue.FromObject(n), int n => XLCellValue.FromObject(n),
                    _ => XLCellValue.FromObject(source.Value?.ToString() ?? "")
                };
                if (source.Value is decimal) cell.Style.NumberFormat.Format = "#,##0.000";
                if (source.Value is string estimated && estimated.EndsWith(" *", StringComparison.Ordinal) &&
                    decimal.TryParse(estimated[..^2], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var kilograms))
                {
                    cell.Value = kilograms;
                    cell.Style.NumberFormat.Format = "#,##0.000\" *\"";
                }
                if (source.RowSpan > 1 || source.ColSpan > 1)
                    sheet.Range(r + 5, col, r + 4 + source.RowSpan, col + source.ColSpan - 1).Merge();
                if (source.Heading)
                {
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E7F0ED");
                }
                for (var rr = r; rr < r + source.RowSpan; rr++)
                    for (var cc = col; cc < col + source.ColSpan; cc++) occupied.Add((rr, cc));
                col += source.ColSpan;
            }
        }
        sheet.Columns(1, table.ColumnCount).Width = 19;
        var range = sheet.Range(5, 1, table.Rows.Count + 4, table.ColumnCount);
        range.Style.Alignment.WrapText = true;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        sheet.Rows(5, table.Rows.Count + 4).Height = 45;
        sheet.SheetView.FreezeRows(4 + table.HeaderRows);
        sheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
