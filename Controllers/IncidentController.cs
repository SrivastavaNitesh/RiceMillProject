using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RiceMillProject.Models;
using System.Data;
using System.Security.Claims;

namespace RiceMillProject.Controllers
{
    [Authorize]
    public class IncidentController : Controller
    {
        private readonly BusinessLayer _busLayer;

        public IncidentController(IConfiguration configuration)
        {
            _busLayer = new BusinessLayer(configuration);
        }

        public IActionResult Index()
        {
            var dt = _busLayer.GetAllSecurityIncidents();
            var incidents = new List<SecurityIncident>();
            if (dt != null && dt.Rows.Count > 0)
            {
                foreach (DataRow row in dt.Rows)
                {
                    incidents.Add(new SecurityIncident
                    {
                        IncidentId = Convert.ToInt32(row["IncidentId"]),
                        IncidentNo = row["IncidentNo"].ToString() ?? "",
                        GateId = Convert.ToInt32(row["GateId"]),
                        IncidentDateTime = Convert.ToDateTime(row["IncidentDateTime"]),
                        IncidentType = row["IncidentType"].ToString() ?? "",
                        RelatedEntryNo = row["RelatedEntryNo"].ToString(),
                        Description = row["Description"].ToString() ?? "",
                        ActionTaken = row["ActionTaken"].ToString(),
                        Severity = row["Severity"].ToString() ?? "Medium",
                        Status = row["Status"].ToString() ?? "Open"
                    });
                }
            }
            return View(incidents);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Gates = Allclass.CreateDropdown(_busLayer.ManageGateMaster("GETALL"));
            return View(new SecurityIncident());
        }

        [HttpPost]
        public IActionResult Create(SecurityIncident inc)
        {
            int createdBy = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var dt = _busLayer.CreateSecurityIncident(inc, createdBy);

            if (dt != null && dt.Rows.Count > 0)
            {
                TempData["SuccessMessage"] = "Security Exception Logged! Incident No: " + dt.Rows[0]["IncidentNo"];
                return RedirectToAction("Index");
            }
            ViewBag.Gates = Allclass.CreateDropdown(_busLayer.ManageGateMaster("GETALL"));
            return View(inc);
        }
    }
}
