using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using RiceMillProject.DAL;
using RiceMillProject.Models;

namespace RiceMillProject.Controllers;

[Authorize, AutoValidateAntiforgeryToken]
public class RecordEditController(IConfiguration configuration, ILogger<RecordEditController> logger) : Controller
{
    private readonly RecordEditDAL edits = new(configuration);
    private int UserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    [HttpGet("/Gateman/GateInwardReport")]
    public IActionResult GateInwardReport(DateTime? from, DateTime? to)
    {
        if(from>to)ModelState.AddModelError("","From date must be on or before To date.");
        ViewBag.From=from?.ToString("yyyy-MM-dd");ViewBag.To=to?.ToString("yyyy-MM-dd");
        return View("GateInwardReport",ModelState.IsValid?edits.List(UserId,"GateInward",from,to):[]);
    }
    [HttpGet]
    public IActionResult Index(string? module, DateTime? from, DateTime? to)
    {
        if(module != null && !RecordEditDAL.Modules.Contains(module))return BadRequest();
        if(from>to)ModelState.AddModelError("","From date must be on or before To date.");
        ViewBag.Module=module;ViewBag.From=from?.ToString("yyyy-MM-dd");ViewBag.To=to?.ToString("yyyy-MM-dd");
        return View("Index",ModelState.IsValid?edits.List(UserId,module,from,to):[]);
    }
    [HttpGet]
    public IActionResult Edit(string module,string id)
    {
        try {
            var model=edits.Load(UserId,module,id);
            if(!model.Access.CanEdit)return StatusCode(403,model.Access.LockedReason);
            return View(model.ModuleKey=="Weight"?"WeightEdit":"Edit",model);
        }catch(UnauthorizedAccessException){return Forbid();}
        catch(ArgumentException){return NotFound();}
    }
    [HttpPost]
    public IActionResult Edit(RecordEditForm model)
    {
        try {
            if(ModelState.IsValid){edits.Save(UserId,model);TempData["SuccessMessage"]="Entry updated successfully.";
                if(model.ModuleKey=="Weight")return RedirectToAction("Report","Gateman");
                if(model.ModuleKey=="Assignment")return RedirectToAction("AssignmentRegister","Unload");
                if(model.ModuleKey=="GateInward")return RedirectToAction(nameof(GateInwardReport));
                return RedirectToAction(nameof(Index),new{module=model.ModuleKey});}
        }catch(UnauthorizedAccessException){return Forbid();}
        catch(ArgumentException ex){ModelState.AddModelError("",ex.Message);}
        catch(SqlException ex){logger.LogWarning(ex,"Record correction failed");ModelState.AddModelError("",ex.Number>=50000?ex.Message:"The correction could not be saved. No changes were committed.");}
        try {
            var fresh=edits.Load(UserId,model.ModuleKey,model.RecordId);
            model.Access=fresh.Access;model.Fields=fresh.Fields;model.DetailFields=fresh.DetailFields;model.InwardNo=fresh.InwardNo;model.HasRecordedTare=fresh.HasRecordedTare;model.NetWeight=fresh.NetWeight;
            return View(model.ModuleKey=="Weight"?"WeightEdit":"Edit",model);
        }catch(UnauthorizedAccessException){return Forbid();}
        catch(ArgumentException){return NotFound();}
    }
    [HttpPost]
    public IActionResult FinalizeRecord(string module,string id,string version)
    {
        try {edits.Finalize(UserId,module,id,version);TempData["SuccessMessage"]="Record finalized. Only admin may correct it within the admin edit period.";}
        catch(ArgumentException ex){TempData["ErrorMessage"]=ex.Message;}
        catch(SqlException ex){logger.LogWarning(ex,"Finalization failed");TempData["ErrorMessage"]=ex.Number>=50000?ex.Message:"The record could not be finalized.";}
        return RedirectToAction(nameof(Index),new{module});
    }
    [HttpGet]
    public IActionResult History(string module,string id)
    {
        try {ViewBag.Reference=id;return View(edits.History(UserId,module,id));}
        catch(UnauthorizedAccessException){return Forbid();}
    }
    [HttpGet]
    public IActionResult Policies()
    {
        if(!edits.IsAdministrator(UserId))return Forbid();
        ViewBag.Bindings=edits.Bindings();return View(edits.Policies());
    }
    [HttpPost]
    public IActionResult Policies(EditPolicyForm model)
    {
        if(!edits.IsAdministrator(UserId))return Forbid();
        try {if(ModelState.IsValid){edits.SavePolicy(UserId,model);TempData["SuccessMessage"]="Edit policy updated.";return RedirectToAction(nameof(Policies));}}
        catch(ArgumentException ex){ModelState.AddModelError("",ex.Message);}
        catch(SqlException ex){logger.LogWarning(ex,"Policy update failed");ModelState.AddModelError("","Policy could not be saved.");}
        ViewBag.Bindings=edits.Bindings();return View(edits.Policies());
    }
    [HttpPost]
    public IActionResult BindPolicy(string module,int postId,int policyId)
    {
        if(!edits.IsAdministrator(UserId))return Forbid();
        try {edits.BindPolicy(UserId,module,postId,policyId);TempData["SuccessMessage"]="Policy assignment updated.";}
        catch(ArgumentException ex){TempData["ErrorMessage"]=ex.Message;}
        catch(SqlException ex){logger.LogWarning(ex,"Policy assignment failed");TempData["ErrorMessage"]=ex.Number>=50000?ex.Message:"Policy assignment could not be saved.";}
        return RedirectToAction(nameof(Policies));
    }
}
