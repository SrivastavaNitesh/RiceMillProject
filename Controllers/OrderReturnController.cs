using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using RiceMillProject.BAL;
using RiceMillProject.Models;

namespace RiceMillProject.Controllers;

[Authorize]
public class OrderReturnController(IConfiguration configuration, ILogger<OrderReturnController> logger) : Controller
{
    private readonly OrderReturnBAL bal = new(configuration);
    private int UserId => int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,out var id)?id:0;
    private int OfficeId => User.CompanyId();

    public IActionResult Index(int? unloadId)
    {
        ViewBag.SelectedUnloadId=unloadId;
        return View(bal.GetPage(OfficeId));
    }
    public IActionResult Details(int id)
    {
        var model=bal.GetDetails(id,OfficeId);
        if(model is null) return NotFound();
        var page=bal.GetPage(OfficeId);
        ViewBag.Locations=page.Locations; ViewBag.Vehicles=page.Vehicles; ViewBag.BagTypes=page.BagTypes; ViewBag.Workers=page.Workers;
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Create(CreateOrderReturnInput model)
    {
        if (!(User.IsAdminUser()||User.IsMethUser()||User.IsSupervisorUser())) return Forbid();
        return Run(model,()=>{var id=bal.Create(model,UserId,OfficeId);return RedirectToAction(nameof(Details),new{id});},nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Decide(DecideOrderReturnInput model)
    {
        if (!(User.IsAdminUser()||User.IsLabUser())) return Forbid();
        return Run(model,()=>{bal.Decide(model,UserId,OfficeId);return RedirectToAction(nameof(Details),new{id=model.OrderReturnId});},nameof(Details),model.OrderReturnId);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult CreateDispatch(CreateReturnDispatchInput model)
    {
        if (!(User.IsAdminUser()||User.IsMethUser()||User.IsSupervisorUser())) return Forbid();
        return Run(model,()=>{bal.CreateDispatch(model,UserId,OfficeId);return RedirectToAction(nameof(Details),new{id=model.OrderReturnId});},nameof(Details),model.OrderReturnId);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult SaveMovement(SaveReturnMovementInput model)
    {
        if (!(User.IsAdminUser()||User.IsMethUser()||User.IsSupervisorUser())) return Forbid();
        return Run(model,()=>{bal.SaveMovement(model,UserId,OfficeId);return RedirectToAction(nameof(Details),new{id=model.OrderReturnId});},nameof(Details),model.OrderReturnId);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult SaveWeight(SaveReturnWeightInput model,int orderReturnId)
    {
        if (!(User.IsAdminUser()||User.IsWeightmanUser())) return Forbid();
        return Run(model,()=>{bal.SaveWeight(model,UserId,OfficeId);return RedirectToAction(nameof(Details),new{id=orderReturnId});},nameof(Details),orderReturnId);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult CompleteOutward(int dispatchId,int orderReturnId)
    {
        if (!(User.IsAdminUser()||User.IsGatemanUser())) return Forbid();
        try { bal.CompleteOutward(dispatchId,UserId,OfficeId);TempData["SuccessMessage"]="Return vehicle outward completed."; }
        catch(Exception ex){Handle(ex);}
        return RedirectToAction(nameof(Details),new{id=orderReturnId});
    }
    private IActionResult Run(object model,Func<IActionResult> action,string fallback,int? id=null)
    {
        if(!ModelState.IsValid){TempData["ErrorMessage"]="Please fill all required details correctly.";return RedirectToAction(fallback,id.HasValue?new{id}:null);}
        try{var result=action();TempData["SuccessMessage"]="Order return action saved successfully.";return result;}catch(Exception ex){Handle(ex);return RedirectToAction(fallback,id.HasValue?new{id}:null);}
    }
    private void Handle(Exception ex){logger.LogError(ex,"Order return operation failed");TempData["ErrorMessage"]=ex is SqlException?ex.Message:"Unable to complete order return action.";}
}
