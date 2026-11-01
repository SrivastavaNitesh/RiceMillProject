using Microsoft.AspNetCore.Mvc;
using RiceMillProject.DAL;
using RiceMillProject.Models;
namespace RiceMillProject.Controllers;
public partial class DashboardController
{
 [HttpGet]
 public IActionResult Stock(DateTime? fromDate=null,DateTime? toDate=null,int? categoryId=null,int? itemId=null,string mode="item")=>MaterialReport(false,fromDate,toDate,categoryId,itemId,mode);
 [HttpGet]
 public IActionResult Palledari(DateTime? fromDate=null,DateTime? toDate=null,int? categoryId=null,int? itemId=null,string mode="item")=>MaterialReport(true,fromDate,toDate,categoryId,itemId,mode);
 private IActionResult MaterialReport(bool palledari,DateTime? fromDate,DateTime? toDate,int? categoryId,int? itemId,string mode)
 {
  if(!User.IsAdminUser())return Forbid();
  if(!ModelState.IsValid||mode is not("item" or "rst")||categoryId<0||itemId<0)return BadRequest("Select valid report filters.");
  var end=(toDate??RiceMillProject.BAL.PurchaseReportBuilder.Today).Date;var start=(fromDate??end.AddDays(-29)).Date;
  if(start>end||end.Year>9998||(end-start).TotalDays>366||User.CompanyId()<=0)return BadRequest("Select a valid company and date range (up to 366 days).");
  var page=new DashboardMaterialDAL(configuration).Load(User.CompanyId(),start,end);page.Mode=mode;page.CategoryId=categoryId;page.ItemId=itemId;
  if(categoryId.HasValue&&!page.Categories.Any(c=>c.CategoryId==categoryId.Value))return NotFound();
  if(categoryId.HasValue){page.Stock=page.Stock.Where(l=>l.CategoryId==categoryId).ToList();page.Charges=page.Charges.Where(l=>l.CategoryId==categoryId).ToList();}
  if(itemId.HasValue){page.Stock=page.Stock.Where(l=>l.ItemId==itemId).ToList();page.Charges=page.Charges.Where(l=>l.ItemId==itemId).ToList();}
  ViewData["Palledari"]=palledari;return View("MaterialReport",page);
 }
}
