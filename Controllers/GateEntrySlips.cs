using Microsoft.AspNetCore.Mvc;
using RiceMillProject.DAL;
using RiceMillProject.Models;
using System.Security.Claims;
namespace RiceMillProject.Controllers;
public partial class GateEntryController
{
 [HttpGet]
 public IActionResult WeighmentSlip(string id,string stage="first",string? next=null)
 {
  if(string.IsNullOrWhiteSpace(id)||stage is not("first" or "final"))return BadRequest();
  var config=HttpContext.RequestServices.GetRequiredService<IConfiguration>();var dal=new WeighmentSlipDAL(config);
  var slip=dal.Load(id);if(slip==null)return NotFound();
  int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var actor);
  var visible=new RecordEditDAL(config).VisibleInwardIds(actor,true);if(!visible.Contains(slip.InwardId))return Forbid();
  if(stage=="final"&&(!slip.Tare.HasValue||!slip.Net.HasValue))return BadRequest("Final weight has not been recorded yet.");
  slip.Stage=stage;var slips=new List<WeighmentSlipModel>{slip};
  if(!string.IsNullOrWhiteSpace(next)){
   var first=dal.Load(next);if(first==null||first.InwardId!=slip.InwardId||!visible.Contains(first.InwardId))return BadRequest();slips.Add(first);
  }
  return View("WeighmentSlip",slips);
 }
}
