using System.Data;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using RiceMillProject.Models;
namespace RiceMillProject.Controllers;
public partial class UnloadController
{
    private int ActorId => int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,out var id)?id:0;
    [HttpGet]
    public IActionResult AssignmentRegister(DateTime? from, DateTime? to)
    {
        if(!ModelState.IsValid || from>to)return BadRequest("Select a valid date range.");
        var access=_edits.List(ActorId,"Assignment",from,to);
        var ids=access.Select(a=>a.RecordId).ToHashSet();
        ViewBag.AssignmentAccess=access;ViewBag.From=from?.ToString("yyyy-MM-dd");ViewBag.To=to?.ToString("yyyy-MM-dd");
        return View(_unloadBal.GetAllUnloading().Where(u=>ids.Contains(u.UnloadId.ToString())).ToList());
    }
    [HttpGet]
    public IActionResult LocationCategories(int locationId, int? categoryId, DateTime? fromDate, DateTime? toDate, string? status, string? search)
    {
        var result=LocationReport(fromDate,toDate,locationId,status,search);
        if(result is not ViewResult view || view.Model is not SupervisorLocationReport report)return result;
        if(categoryId.HasValue&&!report.Materials.AsEnumerable().Any(r=>r.Field<int?>("LocationId")==locationId&&r.Field<int>("CategoryId")==categoryId.Value))return NotFound();
        report.CategoryId=categoryId;return View("LocationCategories",report);
    }
    private static int N(Dictionary<string,string?> d,string key)=>int.TryParse(d.GetValueOrDefault(key),out var n)?n:0;
    private void PrepareFieldCorrection(SupervisorUnloadActionViewModel model, RecordEditForm edit)
    {
        model.EditVersion=edit.Version;model.EditExpiresAt=edit.Access.ExpiresAt;
        model.CategoryRows=edit.Details.Select(d=>new SupervisorUnloadCategoryRow{CategoryId=N(d,"CategoryId"),ItemId=N(d,"ItemId"),LocationId=N(d,"LocationId"),BagTypeId=N(d,"BagTypeId"),BagCount=N(d,"BagCount")}).ToList();
        model.LocationRows=model.CategoryRows.GroupBy(r=>r.LocationId).Select(g=>new SupervisorUnloadLocationRow{LocationId=g.Key,BagRows=g.GroupBy(r=>r.BagTypeId).Select(b=>new SupervisorUnloadLocationBagRow{LocationId=g.Key,BagTypeId=b.Key,BagCount=b.Sum(r=>r.BagCount)}).ToList()}).ToList();
        ViewBag.SavedFieldDetails=model.CategoryRows;
    }
    private IActionResult SaveFieldCorrection(SupervisorUnloadActionViewModel model)
    {
        try
        {
            if(ModelState.Any(m=>(m.Key.StartsWith("CategoryRows")||m.Key.StartsWith("LocationRows")||m.Key=="UnloadId")&&m.Value!.Errors.Count>0))throw new ArgumentException("Complete every field with valid values.");
            var current=_edits.Load(ActorId,"Supervisor",model.UnloadId.ToString());
            var rows=model.CategoryRows??[];
            if(rows.Any(r=>r.BagCount<=0))throw new ArgumentException("Every material row must have positive bags.");
            var locationRows=(model.LocationRows??[]).SelectMany(l=>(l.BagRows??[]).Select(b=>new{l.LocationId,b.BagTypeId,b.BagCount})).ToList();
            if(locationRows.Any(r=>r.BagCount<0)||locationRows.GroupBy(r=>(r.LocationId,r.BagTypeId)).Any(g=>g.Count()>1))throw new ArgumentException("Invalid or duplicate location bag details.");
            var materials=rows.GroupBy(r=>(r.LocationId,r.BagTypeId)).ToDictionary(g=>g.Key,g=>g.Sum(r=>(long)r.BagCount));
            var locations=locationRows.Where(r=>r.BagCount>0).ToDictionary(r=>(r.LocationId,r.BagTypeId),r=>(long)r.BagCount);
            if(materials.Count!=locations.Count||materials.Any(m=>!locations.TryGetValue(m.Key,out var count)||count!=m.Value))throw new ArgumentException("Material bags must match each location and bag type total.");
            var posted=new RecordEditForm{ModuleKey="Supervisor",RecordId=model.UnloadId.ToString(),Version=model.EditVersion??"",Reason=model.CorrectionReason??""};
            posted.Details=rows.Select(r=>new Dictionary<string,string?>{
                ["CategoryId"]=r.CategoryId.ToString(),["ItemId"]=r.ItemId.ToString(),["LocationId"]=r.LocationId.ToString(),["BagTypeId"]=r.BagTypeId.ToString(),["BagCount"]=r.BagCount.ToString(),
                ["ActualWeightKg"]=current.Details.FirstOrDefault(d=>N(d,"CategoryId")==r.CategoryId&&N(d,"ItemId")==r.ItemId&&N(d,"LocationId")==r.LocationId&&N(d,"BagTypeId")==r.BagTypeId)?.GetValueOrDefault("ActualWeightKg")}).ToList();
            _edits.Save(ActorId,posted);TempData["SuccessMessage"]="Field Details updated successfully.";
            return RedirectToAction(nameof(MethWorkRegister));
        }
        catch(UnauthorizedAccessException){return Forbid();}
        catch(Exception ex) when(ex is ArgumentException or SqlException){TempData["ErrorMessage"]=ex is SqlException sql&&sql.Number<50000?"Could not save the changes. Please reload and try again.":ex.Message;return RedirectToAction(nameof(SupervisorAction),new{id=model.UnloadId,edit=true});}
    }
    private IActionResult SaveWorkerCorrection(UnloadTransaction model)
    {
        try
        {
            // Identity and item/location selection are read from the saved assignment, not from this form.
            if(ModelState.Any(m=>(m.Key.StartsWith("WorkerRows")||m.Key=="UnloadId")&&m.Value!.Errors.Count>0))throw new ArgumentException("Complete every worker row with valid values.");
            var posted=new RecordEditForm{ModuleKey="Labour",RecordId=model.UnloadId.ToString(),Version=model.EditVersion??"",Reason=model.CorrectionReason??""};
            posted.Details=(model.WorkerRows??[]).Where(r=>!string.IsNullOrWhiteSpace(r.WorkType)||r.BagTypeId.GetValueOrDefault()>0||r.BagCount!=0||r.PerBagCharge!=0).Select(r=>new Dictionary<string,string?>{
                ["WorkerId"]=r.WorkerId.ToString(),["WorkType"]=r.WorkType,["BagTypeId"]=r.BagTypeId?.ToString(),["BagCount"]=r.BagCount.ToString(),["PerBagCharge"]=r.PerBagCharge.ToString(CultureInfo.InvariantCulture)}).ToList();
            _edits.Save(ActorId,posted);TempData["SuccessMessage"]="Worker Details updated successfully.";
            return RedirectToAction(nameof(MethWorkRegister));
        }
        catch(UnauthorizedAccessException){return Forbid();}
        catch(Exception ex) when(ex is ArgumentException or SqlException){TempData["ErrorMessage"]=ex is SqlException sql&&sql.Number<50000?"Could not save the changes. Please reload and try again.":ex.Message;return RedirectToAction(nameof(SupervisorMethWork),new{id=model.UnloadId,edit=true});}
    }
}
