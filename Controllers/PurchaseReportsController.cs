using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using RiceMillProject.BAL;
using RiceMillProject.DAL;
using RiceMillProject.Models;

namespace RiceMillProject.Controllers;

[Authorize(Policy = "BillingAccess")]
public class PurchaseReportsController(IConfiguration configuration, ILogger<PurchaseReportsController> logger) : Controller
{
    private readonly PurchaseReportDAL dal = new(configuration);
    public IActionResult Index() => RedirectToAction(nameof(Monthly));
    public IActionResult Monthly(PurchaseFilter filter, bool excel = false) => Report(filter, "Monthly", excel);
    public IActionResult Daily(PurchaseFilter filter, bool excel = false) => Report(filter, "Daily", excel);
    public IActionResult DateRange(PurchaseFilter filter, bool excel = false) => Report(filter, "DateRange", excel);
    public IActionResult Party(PurchaseFilter filter, bool excel = false) => Report(filter, "Party", excel);
    public IActionResult Locations(PurchaseFilter filter, bool excel = false) => Report(filter, "Locations", excel);
    public IActionResult LocationDetails(PurchaseFilter filter, bool excel = false) => Report(filter, "LocationDetails", excel);

    private IActionResult Report(PurchaseFilter filter, string mode, bool excel)
    {
        PurchasePage page = new() { Filter = filter };
        try
        {
            PurchaseReportBuilder.Normalize(filter, mode);
            if (ModelState.IsValid) page = dal.Load(filter);
            if (mode == "LocationDetails" && ModelState.IsValid && !page.Locations.Any(l => l.LocationId == filter.LocationId)) return NotFound();
        }
        catch (ArgumentException ex) { ModelState.AddModelError("", ex.Message); }
        catch (SqlException ex)
        {
            logger.LogError(ex, "Unable to load purchase report {Mode}", mode);
            ModelState.AddModelError("", "Purchase data could not be loaded. Please retry or contact your administrator.");
        }
        page.Title = mode switch
        {
            "Monthly" => "Month Wise Purchase Report", "Daily" => "Date Wise Purchase Report", "DateRange" => "Date Range Purchase Report",
            "Party" => "Party Wise Purchase Report", "LocationDetails" => page.Locations.FirstOrDefault(l => l.LocationId == filter.LocationId)?.LocationName + " - Purchase Details",
            _ => "Location Wise Purchase Report"
        };
        if (excel && !ModelState.IsValid) return BadRequest(ModelState);
        var table = mode switch
        {
            "Locations" => PurchaseTableBuilder.Locations(page),
            "LocationDetails" => PurchaseTableBuilder.Location(page),
            _ => PurchaseTableBuilder.Register(page)
        };
        if (excel) return File(PurchaseExcel.Export(page, table), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Purchase-{mode}-{PurchaseReportBuilder.Today:yyyyMMdd}.xlsx");
        ViewBag.ReportTable = table;
        return View("Report", page);
    }
}
