using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RiceMillProject.Models;
using System.Data;
using System.Security.Claims;

namespace RiceMillProject.Controllers
{
    [Authorize]
    public class VisitorController : Controller
    {
        private readonly BusinessLayer _busLayer;

        public VisitorController(IConfiguration configuration)
        {
            _busLayer = new BusinessLayer(configuration);
        }

        public IActionResult Index()
        {
            var dt = _busLayer.GetAllVisitors();
            var visitors = new List<VisitorRegister>();
            if (dt != null && dt.Rows.Count > 0)
            {
                foreach (DataRow row in dt.Rows)
                {
                    visitors.Add(new VisitorRegister
                    {
                        VisitorId = Convert.ToInt32(row["VisitorId"]),
                        VisitorNo = row["VisitorNo"].ToString() ?? "",
                        GateName = row["GateName"].ToString() ?? "",
                        VisitDateTime = Convert.ToDateTime(row["VisitDateTime"]),
                        VisitorName = row["VisitorName"].ToString() ?? "",
                        MobileNo = row["MobileNo"].ToString() ?? "",
                        CompanyOrg = row["CompanyOrg"].ToString(),
                        PurposeOfVisit = row["PurposeOfVisit"].ToString() ?? "",
                        PersonToMeetName = row["PersonToMeetName"].ToString(),
                        DepartmentName = row["DepartmentName"] != DBNull.Value ? row["DepartmentName"].ToString() : "N/A",
                        VisitorPassNo = row["VisitorPassNo"].ToString(),
                        PassStatus = row["PassStatus"].ToString() ?? "Active",
                        ExitDateTime = row["ExitDateTime"] != DBNull.Value ? Convert.ToDateTime(row["ExitDateTime"]) : null
                    });
                }
            }
            return View(visitors);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Gates = Allclass.CreateDropdown(_busLayer.ManageGateMaster("GETALL"));
            ViewBag.Departments = Allclass.CreateDropdown(_busLayer.ManageDepartmentMaster("GETALL"));
            return View(new VisitorRegister());
        }

        [HttpPost]
        public IActionResult Create(VisitorRegister visitor)
        {
            int createdBy = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var dt = _busLayer.CreateVisitorEntry(visitor, createdBy);
            
            if (dt != null && dt.Rows.Count > 0)
            {
                TempData["SuccessMessage"] = "Visitor Pass Generated Successfully! Pass No: " + dt.Rows[0]["VisitorPassNo"];
                return RedirectToAction("Index");
            }
            ViewBag.Gates = Allclass.CreateDropdown(_busLayer.ManageGateMaster("GETALL"));
            ViewBag.Departments = Allclass.CreateDropdown(_busLayer.ManageDepartmentMaster("GETALL"));
            return View(visitor);
        }

        [HttpPost]
        public IActionResult Exit(string visitorNo)
        {
            _busLayer.ExitVisitor(visitorNo);
            TempData["SuccessMessage"] = "Visitor Exit Recorded Successfully.";
            return RedirectToAction("Index");
        }

        public IActionResult PassPrint(string visitorNo)
        {
            ViewBag.VisitorNo = visitorNo;
            return View();
        }
    }
}
