using Microsoft.AspNetCore.Mvc;
using RiceMillProject.Models;
using RiceMillProject.DAL;
using RiceMillProject.BAL;
namespace RiceMillProject.Controllers;
public partial class UnloadController
{
 [HttpGet]
 public IActionResult PaddyRecord(PaddyRecordFilter filter)=>GrainRecord(filter,"Paddy");
 private IActionResult GrainRecord(PaddyRecordFilter filter,string category)
 {
  if(!ModelState.IsValid)return BadRequest("Select valid report filters.");
  int person=GetCurrentPersonId(),office=GetCurrentOfficeId();if(person<=0||office<=0)return Forbid();
  var today=PurchaseReportBuilder.Today;
  filter.ToDate=(filter.ToDate??today).Date;
  if(filter.ToDate.Value.Year is <1900 or >9998)return BadRequest("Select a date between 1900 and 9998.");
  filter.FromDate=(filter.FromDate??filter.ToDate.Value.AddDays(-29)).Date;
  if(filter.FromDate.Value.Year<1900||filter.FromDate>filter.ToDate||(filter.ToDate.Value-filter.FromDate.Value).TotalDays>366)return BadRequest("Select a valid date range (up to 366 days).");
  try{return View("PaddyRecord",new PaddyRecordDAL(HttpContext.RequestServices.GetRequiredService<IConfiguration>()).Load(person,office,filter,category));}
  catch(ArgumentException ex){return BadRequest(ex.Message);}
 }
}
