using ClosedXML.Excel;
using RiceMillProject.Models;

namespace RiceMillProject.BAL;

public static class MethWorkSettlementExcel
{
    public static byte[] Export(MethPaymentReport report)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Labour Settlement");
        const int columnCount = 13;

        sheet.Cell(1, 1).Value = "Labour Work Settlement Report";
        sheet.Range(1, 1, 1, columnCount).Merge();
        sheet.Cell(2, 1).Value = BuildFilterText(report);
        sheet.Range(2, 1, 2, columnCount).Merge();

        var headers = new[]
        {
            "Supervisor", "Meth", "RST Number", "Work Date", "Work Type", "Bag Type",
            "Bag Count", "Per Bag Rate", "Line Amount", "Total Unloaded Bags",
            "Total Return Bags", "Final Retained Bags", "RST Total Payable"
        };
        for (var column = 0; column < headers.Length; column++)
            sheet.Cell(4, column + 1).Value = headers[column];

        var excelRow = 5;
        foreach (var row in report.Rows)
        {
            var details = row.Details.Count > 0
                ? row.Details
                : [new MethPaymentReportDetail { WorkType = "No bag detail" }];

            foreach (var detail in details)
            {
                sheet.Cell(excelRow, 1).Value = row.SupervisorName;
                sheet.Cell(excelRow, 2).Value = row.MethName;
                sheet.Cell(excelRow, 3).Value = row.RSTNumber;
                sheet.Cell(excelRow, 4).Value = row.WorkDate;
                sheet.Cell(excelRow, 5).Value = detail.WorkType == "ReturnLoading" ? "Return Loading" : detail.WorkType;
                sheet.Cell(excelRow, 6).Value = detail.BagTypeName;
                sheet.Cell(excelRow, 7).Value = detail.BagCount;
                sheet.Cell(excelRow, 8).Value = detail.PerBagPrice;
                sheet.Cell(excelRow, 9).Value = detail.Amount;
                sheet.Cell(excelRow, 10).Value = row.UnloadingBags;
                sheet.Cell(excelRow, 11).Value = row.ReturnBags;
                sheet.Cell(excelRow, 12).Value = row.FinalBags;
                sheet.Cell(excelRow, 13).Value = row.TotalPayable;
                excelRow++;
            }
        }

        sheet.Cell(excelRow + 1, 1).Value = "Grand Total";
        sheet.Range(excelRow + 1, 1, excelRow + 1, 9).Merge();
        sheet.Cell(excelRow + 1, 10).Value = report.UnloadingBags;
        sheet.Cell(excelRow + 1, 11).Value = report.ReturnBags;
        sheet.Cell(excelRow + 1, 12).Value = report.FinalBags;
        sheet.Cell(excelRow + 1, 13).Value = report.TotalPayable;

        sheet.Range(1, 1, 1, columnCount).Style
            .Font.SetBold().Font.SetFontSize(16).Font.SetFontColor(XLColor.White)
            .Fill.SetBackgroundColor(XLColor.FromHtml("#087D74"));
        sheet.Range(4, 1, 4, columnCount).Style
            .Font.SetBold().Font.SetFontColor(XLColor.White)
            .Fill.SetBackgroundColor(XLColor.FromHtml("#17443F"));
        sheet.Range(excelRow + 1, 1, excelRow + 1, columnCount).Style
            .Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#E8F6F3"));
        var reportBorder = sheet.Range(4, 1, excelRow + 1, columnCount).Style.Border;
        reportBorder.InsideBorder = XLBorderStyleValues.Thin;
        reportBorder.OutsideBorder = XLBorderStyleValues.Thin;
        sheet.Column(4).Style.DateFormat.Format = "dd-mmm-yyyy";
        sheet.Columns(8, 9).Style.NumberFormat.Format = "₹#,##0.00";
        sheet.Column(13).Style.NumberFormat.Format = "₹#,##0.00";
        sheet.Columns().AdjustToContents(10, 35);
        sheet.SheetView.FreezeRows(4);
        sheet.Range(4, 1, Math.Max(4, excelRow - 1), columnCount).SetAutoFilter();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string BuildFilterText(MethPaymentReport report)
    {
        var period = report.FromDate.HasValue || report.ToDate.HasValue
            ? $"Period: {report.FromDate?.ToString("dd-MMM-yyyy") ?? "Beginning"} to {report.ToDate?.ToString("dd-MMM-yyyy") ?? "Latest"}"
            : "Period: All available records";
        var supervisor = report.IsAdminView
            ? "Supervisor: " + (report.Supervisors.FirstOrDefault(x => x.Id == report.SupervisorId)?.Name ?? "All Supervisors")
            : "Supervisor: My Data";
        var meth = "Meth: " + (report.Meths.FirstOrDefault(x => x.Id == report.MethId)?.Name ?? "All Meth");
        return $"{period} | {supervisor} | {meth}";
    }
}
