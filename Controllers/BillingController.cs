using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using RiceMillProject.BAL;
using RiceMillProject.DAL;
using RiceMillProject.Models;

namespace RiceMillProject.Controllers;

[Authorize(Policy="BillingAccess")]
[AutoValidateAntiforgeryToken]
public class BillingController(IConfiguration configuration,ILogger<BillingController> logger):Controller
{
    private readonly BillingDAL dal=new(configuration);
    private int UserId=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public IActionResult Index(string? search)
    {
        ViewBag.Search=search;
        var list=dal.List();
        if(!string.IsNullOrWhiteSpace(search))list=list.Where(r=>r.RSTNumber.Contains(search,StringComparison.OrdinalIgnoreCase)||r.PartyName.Contains(search,StringComparison.OrdinalIgnoreCase)||r.PartyMobile.Contains(search)).ToList();
        return View(list);
    }
    public IActionResult Details(string rstNumber)
    {
        var source=dal.GetSource(rstNumber);if(source==null)return NotFound();
        if(source.Header.BillId.HasValue)
        {
            var bill=dal.GetBill(source.Header.BillId.Value)!;
            bill.Calculation.Source.Header.BillId=bill.BillId;
            bill.Calculation.Source.Header.TotalAmount=bill.Calculation.Total;
            bill.Calculation.Source.Header.PaidAmount=bill.Paid;
            return View(bill.Calculation);
        }
        return View(BillingCalculator.Calculate(source,BillingCalculator.NewForm(source)));
    }
    [HttpGet] public IActionResult Generate(string rstNumber)
    {
        var source=dal.GetSource(rstNumber);if(source==null)return NotFound();
        if(source.Header.BillId.HasValue)return RedirectToAction(nameof(Bill),new{id=source.Header.BillId});
        if(!source.Header.LabComplete){TempData["BillingInfo"]="Complete all lab tests before generating a bill.";return RedirectToAction(nameof(Details),new{rstNumber});}
        var form=BillingCalculator.NewForm(source);
        return View(new BillingPage{Form=form,Calculation=BillingCalculator.Calculate(source,form)});
    }
    [HttpPost] public IActionResult Generate([Bind(Prefix="Form")] BillingForm form,string intent="preview")
    {
        var source=dal.GetSource(form.RSTNumber);if(source==null)return NotFound();
        if(!source.Header.LabComplete){TempData["BillingInfo"]="Lab tests are incomplete. Bill generation is blocked.";return RedirectToAction(nameof(Details),new{rstNumber=form.RSTNumber});}
        if(source.Header.BillId.HasValue)return RedirectToAction(nameof(Bill),new{id=source.Header.BillId});
        if(ModelState.IsValid && intent=="save")
        {
            try {var id=dal.Save(form,UserId);TempData["BillingSuccess"]=$"Bill RM-{id:D6} generated.";return RedirectToAction(nameof(Bill),new{id});}
            catch(ArgumentException ex){ModelState.AddModelError("",ex.Message);}
            catch(SqlException ex){Error(ex);}
        }
        var calculation=BillingCalculator.Calculate(source,form,true);
        if(ModelState.IsValid && calculation.Issues.Count==0)ViewBag.PreviewReady=true;
        return View(new BillingPage{Form=form,Calculation=calculation});
    }
    public IActionResult Bill(int id)
    {
        var bill=dal.GetBill(id);if(bill==null)return NotFound();
        ViewBag.Payment=new BillingPaymentInput{BillId=id,Amount=bill.Balance};
        return View(bill);
    }
    [HttpPost] public IActionResult Pay([Bind(Prefix="Payment")] BillingPaymentInput payment)
    {
        if(ModelState.IsValid)
        {
            try{dal.Pay(payment,UserId);TempData["BillingSuccess"]="Payment recorded.";return RedirectToAction(nameof(Bill),new{id=payment.BillId});}
            catch(ArgumentException ex){ModelState.AddModelError("",ex.Message);}
            catch(SqlException ex){Error(ex);}
        }
        var bill=dal.GetBill(payment.BillId);if(bill==null)return NotFound();
        ViewBag.Payment=payment;return View("Bill",bill);
    }
    public IActionResult Rules(int? categoryId,int? testId)
    {
        var rules=dal.Rules();ViewBag.Rules=rules;
        return View(rules.FirstOrDefault(r=>r.CategoryId==categoryId && r.TestId==testId)??new BillingRule());
    }
    [HttpPost] public IActionResult Rules(BillingRule rule)
    {
        if(ModelState.IsValid)
        {
            try{dal.SaveRule(rule,UserId);TempData["BillingSuccess"]="Billing deduction rule saved.";return RedirectToAction(nameof(Rules));}
            catch(ArgumentException ex){ModelState.AddModelError("",ex.Message);}
            catch(SqlException ex){Error(ex);}
        }
        ViewBag.Rules=dal.Rules();return View(rule);
    }
    private void Error(SqlException ex)
    {
        logger.LogWarning(ex,"Billing operation failed");
        ModelState.AddModelError("",ex.Number>=50000?ex.Message:ex.Number is 2601 or 2627?"This bill or payment already exists. Reload the register.":"Billing could not be saved. Your form values are retained.");
    }
}
