using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using RiceMillProject.BAL;
using RiceMillProject.Models;
using System.Data;
using System.Security.Claims;
using System.Text;

namespace RiceMillProject.Controllers;

[Authorize(Policy = "LabAccess")]
[AutoValidateAntiforgeryToken]
public class LabController(IConfiguration configuration, ILogger<LabController> logger) : Controller
{
    private readonly LabWorkflowBAL lab = new(configuration);
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public IActionResult Index() => View(lab.GetRsts());

    [HttpGet]
    public IActionResult Create(string? rstNumber)
    {
        if (string.IsNullOrWhiteSpace(rstNumber)) return RedirectToAction(nameof(Index));
        return View(new LabEntryViewModel { RSTNumber = rstNumber, AvailableItems = lab.GetItems(rstNumber) });
    }
    [HttpPost]
    public IActionResult Create(LabEntryViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var reportId = lab.SaveReport(model, UserId);
                TempData["LabSuccess"] = "Lab results saved successfully.";
                return RedirectToAction(nameof(Report), new { id = reportId });
            }
            catch (ArgumentException ex) { ModelState.AddModelError("", ex.Message); }
            catch (SqlException ex) { DatabaseError(ex); }
        }
        model.AvailableItems = lab.GetItems(model.RSTNumber);
        return View(model);
    }
    [HttpGet]
    public IActionResult Tests(int? id)
    {
        var tests = lab.GetTests();
        var form = id.HasValue ? tests.FirstOrDefault(t => t.TestId == id) : new LabTestMaster();
        DataTable dtUnits = lab.GetUnits();
        if (!id.HasValue && form != null)
            form.UnitId = dtUnits.AsEnumerable().Where(r => string.Equals(Convert.ToString(r["UnitCode"]), "Q", StringComparison.OrdinalIgnoreCase)).Select(r => (int?)Convert.ToInt32(r["UnitId"])).FirstOrDefault();
        ViewBag.UnitTable = dtUnits;
        return form == null ? NotFound() : View(new LabTestMasterPage { Form = form, Tests = tests });
    }
    [HttpPost]
    public IActionResult Tests([Bind(Prefix = "Form")] LabTestMaster form)
    {
       
        if (ModelState.IsValid)
        {
            try { lab.SaveTest(form, UserId); TempData["LabSuccess"] = "Test saved."; return RedirectToAction(nameof(Tests)); }
            catch (SqlException ex) { DatabaseError(ex); }
        }
        DataTable dtUnits = lab.GetUnits();
        ViewBag.UnitTable = dtUnits;
        return View(new LabTestMasterPage { Form = form, Tests = lab.GetTests() });
    }
    [HttpPost]
    public IActionResult RemoveTest(int id)
    {
        lab.RemoveTest(id, UserId);
        TempData["LabSuccess"] = "Test removed from active selections. Saved reports are retained.";
        return RedirectToAction(nameof(Tests));
    }
    [HttpGet]
    public IActionResult Mappings(int? id)
    {
        var mappings = lab.GetMappings();
        var form = new LabItemTestSelection();
        if (id.HasValue)
        {
            var mapping = mappings.FirstOrDefault(m => m.MappingId == id);
            if (mapping == null) return NotFound();
            form.MappingId = mapping.MappingId;
            form.CategoryId = mapping.CategoryId;
            form.ItemId = mapping.ItemId;
            form.SelectedTestIds = mappings.Where(m => m.CategoryId == form.CategoryId && m.ItemId == form.ItemId).Select(m => m.TestId).Distinct().ToList();
            form.OriginalTestIds = form.SelectedTestIds.ToList();
        }
        return View(new LabMappingPage { Form = form, Mappings = mappings, Items = lab.GetItems(), Tests = lab.GetTests() });
    }
    [HttpPost]
    public IActionResult Mappings([Bind(Prefix = "Form")] LabItemTestSelection form)
    {
        if (ModelState.IsValid)
        {
            try { lab.SaveMappings(form, UserId); TempData["LabSuccess"] = "Item test selections saved."; return RedirectToAction(nameof(Mappings)); }
            catch (SqlException ex) { DatabaseError(ex); }
        }
        return View(new LabMappingPage { Form = form, Mappings = lab.GetMappings(), Items = lab.GetItems(), Tests = lab.GetTests() });
    }
    [HttpPost]
    public IActionResult RemoveMapping(int id)
    {
        lab.RemoveMapping(id, UserId);
        TempData["LabSuccess"] = "Mapping removed. Saved reports are retained.";
        return RedirectToAction(nameof(Mappings));
    }
    public IActionResult Reports(LabReportsPage model)
    {
        if (model.From > model.To) ModelState.AddModelError("", "From date must be on or before To date.");
        if (ModelState.IsValid) model.Reports = lab.GetReports(model.RstNumber, model.From, model.To);
        return View(model);
    }
    public IActionResult Report(int id)
    {
        var report = lab.GetReport(id);
        return report == null ? NotFound() : View(report);
    }
    public IActionResult Export(int id)
    {
        var report = lab.GetReport(id);
        if (report == null) return NotFound();
        static string Cell(string? text)
        {
            text ??= "";
            var trimmed = text.TrimStart();
            if ((trimmed.Length > 0 && "=+-@".Contains(trimmed[0])) || text.StartsWith('\t') || text.StartsWith('\r')) text = "'" + text;
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }
        var csv = new StringBuilder("Report,RST,Technician,Tested at,Category,Item,Test,Result,Unit,Remarks\r\n");
        foreach (var r in report.Results)
            csv.AppendLine(string.Join(",", new[] { report.ReportId.ToString(), report.RSTNumber, report.TechnicianName, report.TestedAt.ToString("yyyy-MM-dd HH:mm:ss"), r.CategoryName, r.ItemName, r.TestName, r.Value, r.Unit, report.Remarks }.Select(Cell)));
        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(), "text/csv", $"Lab-Report-{id}.csv");
    }
    private void DatabaseError(SqlException ex)
    {
        logger.LogWarning(ex, "Lab operation failed");
        ModelState.AddModelError("", ex.Number == 50001 ? ex.Message : ex.Number is 2601 or 2627 ? "This entry already exists. Refresh and try again." : "Unable to save. Please reload and try again.");
    }
}
