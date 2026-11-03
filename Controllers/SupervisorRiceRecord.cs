using Microsoft.AspNetCore.Mvc;
using RiceMillProject.Models;
namespace RiceMillProject.Controllers;
public partial class UnloadController
{
 [HttpGet]
 public IActionResult RiceRecord(PaddyRecordFilter filter)=>GrainRecord(filter,"Rice");
}
