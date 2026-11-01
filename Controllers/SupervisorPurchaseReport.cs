using Microsoft.AspNetCore.Mvc;
using RiceMillProject.BAL;
using RiceMillProject.Models;
namespace RiceMillProject.Controllers;
public partial class UnloadController
{
    [HttpGet]
    public IActionResult PurchaseReport(DateTime? fromDate,DateTime? toDate)
    {
        if(!ModelState.IsValid || fromDate>toDate)return BadRequest("Select valid report dates.");
        if(GetCurrentPersonId()<=0 || GetCurrentOfficeId()<=0)return Forbid();
        var config=HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var names=PurchaseAssignmentNames();
        var inward=new GateInwardBAL(config);
        var entries=inward.GetAllInwardEntries(includeItemCategories:true)
            .Where(e=>e.RSTNumber!=null && names.ContainsKey(e.RSTNumber))
            .Where(e=>!fromDate.HasValue || e.InwardDate.Date>=fromDate.Value.Date)
            .Where(e=>!toDate.HasValue || e.InwardDate.Date<=toDate.Value.Date).ToList();
        inward.PopulateReportDetails(entries);
        var actor=int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,out var id)?id:0;
        ViewBag.EditRows=_edits.List(actor,"Supervisor");
        ViewBag.AssignmentNames=names;ViewBag.FromDate=fromDate?.ToString("yyyy-MM-dd");ViewBag.ToDate=toDate?.ToString("yyyy-MM-dd");
        return View(entries);
    }
        private Dictionary<string,(string Meth,string Supervisor)> PurchaseAssignmentNames()
        {
            var config=HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var person=GetCurrentPersonId();
            using var con=new Microsoft.Data.SqlClient.SqlConnection(config.GetConnectionString("DefaultConnection"));
            using var cmd=new Microsoft.Data.SqlClient.SqlCommand(@"SELECT u.RSTNumber,m.PersonName MethName,s.PersonName SupervisorName FROM dbo.t_UnloadTransaction u JOIN dbo.t_GateEntry g ON g.RSTNumber=u.RSTNumber LEFT JOIN dbo.P02_Person m ON m.PersonId=u.MethId LEFT JOIN dbo.P02_Person s ON s.PersonId=u.SupervisorId WHERE u.SupervisorId=@Person AND g.TargetOfficeId=@Office",con);
            cmd.Parameters.AddWithValue("@Person",person);cmd.Parameters.AddWithValue("@Office",GetCurrentOfficeId());con.Open();
            using var reader=cmd.ExecuteReader();var rows=new List<(string Rst,string Meth,string Supervisor)>();
            while(reader.Read())rows.Add((reader.GetString(0),reader.IsDBNull(1)?"—":reader.GetString(1),reader.IsDBNull(2)?"—":reader.GetString(2)));
            return rows.GroupBy(r=>r.Rst).ToDictionary(g=>g.Key,g=>(string.Join(", ",g.Select(r=>r.Meth).Distinct()),string.Join(", ",g.Select(r=>r.Supervisor).Distinct())));
        }

}
