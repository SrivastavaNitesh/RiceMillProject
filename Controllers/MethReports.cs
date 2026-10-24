using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using RiceMillProject.Models;
namespace RiceMillProject.Controllers;
public partial class MethController
{
    private int ActorId=>int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,out var id)?id:0;
    private HashSet<string> SavedIds=>((ViewBag.MethAccess as List<RecordEditRow>)??[]).Select(r=>r.RecordId).ToHashSet();
    private List<UnloadTransaction> PrepareMethReport()
    {
        var person=CurrentPersonId();
        ViewBag.MethAccess=_edits.List(ActorId,"Labour");
        ViewBag.CategoryNames=_reports.CategoryNames(person,User.CompanyId(),true);
        return _unloadBal.GetMethAssignments(person).Where(a=>a.MethId==person).OrderByDescending(a=>a.UnloadTime).ThenByDescending(a=>a.UnloadId).ToList();
    }
    [HttpGet]
    public IActionResult WorkReport(string status="all",DateTime? from=null,DateTime? to=null,string? search=null)
    {
        if(!ModelState.IsValid||from>to||status is not("all" or "pending" or "completed")||(search?.Length??0)>200)return BadRequest("Select valid report filters.");
        var rows=PrepareMethReport().AsEnumerable();var saved=SavedIds;
        if(status=="pending")rows=rows.Where(a=>!saved.Contains(a.UnloadId.ToString()));
        if(status=="completed")rows=rows.Where(a=>saved.Contains(a.UnloadId.ToString()));
        if(from.HasValue)rows=rows.Where(a=>a.UnloadTime?.Date>=from.Value.Date);
        if(to.HasValue)rows=rows.Where(a=>a.UnloadTime?.Date<=to.Value.Date);
        if(!string.IsNullOrWhiteSpace(search))rows=rows.Where(a=>($"{a.RSTNumber} {a.VehicleNumber} {a.PartyName}").Contains(search.Trim(),StringComparison.OrdinalIgnoreCase));
        ViewBag.Status=status;ViewBag.From=from?.ToString("yyyy-MM-dd");ViewBag.To=to?.ToString("yyyy-MM-dd");ViewBag.Search=search;
        return View(rows.ToList());
    }
    private void SeedMethDetails(UnloadTransaction model,RecordEditForm saved,bool viewOnly)
    {
        model.EditVersion=saved.Version;model.EditExpiresAt=saved.Access.ExpiresAt;
        var details=saved.Details.Select(d=>new Dictionary<string,string?>(d){["WorkType"]=d.GetValueOrDefault("WorkType")=="Loading"?"Load":"Unload"}).ToList();
        var names=model.WorkerRows.GroupBy(w=>w.WorkerId).ToDictionary(g=>g.Key,g=>g.First().WorkerName);
        var cards=details.GroupBy(d=>(id:int.Parse(d["WorkerId"]!),work:d["WorkType"])).Select(g=>new WorkerAllocation{WorkerId=g.Key.id,WorkerName=names.GetValueOrDefault(g.Key.id,"Worker "+g.Key.id),WorkType=g.Key.work??""}).ToList();
        if(!viewOnly)cards.AddRange(model.WorkerRows.Where(w=>!cards.Any(c=>c.WorkerId==w.WorkerId)));
        model.WorkerRows=cards;ViewBag.SavedMethDetails=details;ViewBag.ReadOnly=viewOnly;
    }
    private IActionResult SaveMethCorrection(UnloadTransaction model)
    {
        try
        {
            if(ModelState.Any(m=>m.Key.StartsWith("WorkerRows")&&m.Value!.Errors.Count>0))throw new ArgumentException("Enter valid worker details.");
            var rows=(model.WorkerRows??[]).Where(r=>!string.IsNullOrWhiteSpace(r.WorkType)||r.BagTypeId.GetValueOrDefault()>0||r.BagCount!=0||r.PerBagCharge!=0).ToList();
            if(rows.Any(r=>r.PerBagCharge<=0))throw new ArgumentException("Per bag charge must be greater than zero.");
            var form=new RecordEditForm{ModuleKey="Labour",RecordId=model.UnloadId.ToString(),Version=model.EditVersion??"",Reason=model.CorrectionReason??"",Details=rows.Select(r=>new Dictionary<string,string?>{
                ["WorkerId"]=r.WorkerId.ToString(),["WorkType"]=r.WorkType switch{"Load"=>"Loading","Unload"=>"Unloading",var x=>x},["BagTypeId"]=r.BagTypeId?.ToString(),["BagCount"]=r.BagCount.ToString(),["PerBagCharge"]=r.PerBagCharge.ToString(CultureInfo.InvariantCulture)}).ToList()};
            _edits.Save(ActorId,form);TempData["SuccessMessage"]="Work details updated successfully.";return RedirectToAction(nameof(Index));
        }
        catch(UnauthorizedAccessException){return Forbid();}
        catch(Exception ex) when(ex is ArgumentException or SqlException){TempData["ErrorMessage"]=ex is SqlException sql&&sql.Number<50000?"Changes could not be saved. Please reload and try again.":ex.Message;return RedirectToAction(nameof(ExecuteUnload),new{id=model.UnloadId,edit=true});}
    }
}
