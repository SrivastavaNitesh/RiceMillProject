using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RiceMillProject.Models;
using System.Data;

namespace RiceMillProject.Controllers
{
    [Authorize(Roles = "Admin")]
    public class GateMasterController : Controller
    {
        private readonly BusinessLayer _busLayer;

        public GateMasterController(IConfiguration configuration)
        {
            _busLayer = new BusinessLayer(configuration);
        }

        public IActionResult Index()
        {
            var dt = _busLayer.ManageGateMaster("GETALL");
            var gates = new List<GateMaster>();
            if (dt != null && dt.Rows.Count > 0)
            {
                foreach (DataRow row in dt.Rows)
                {
                    gates.Add(new GateMaster
                    {
                        GateId = Convert.ToInt32(row["GateId"]),
                        GateCode = row["GateCode"].ToString() ?? "",
                        GateName = row["GateName"].ToString() ?? "",
                        GateType = row["GateType"].ToString() ?? "",
                        LocationArea = row["LocationArea"].ToString(),
                        DepartmentName = row["DepartmentName"] != DBNull.Value ? row["DepartmentName"].ToString() : "All",
                        InwardAllowed = Convert.ToBoolean(row["InwardAllowed"]),
                        OutwardAllowed = Convert.ToBoolean(row["OutwardAllowed"]),
                        IsActive = Convert.ToBoolean(row["IsActive"])
                    });
                }
            }
            return View(gates);
        }

        [HttpGet]
        public IActionResult Create()
        {
            DataTable dtDept = _busLayer.ManageDepartmentMaster("GETALL");
            ViewBag.Departments = Allclass.CreateDropdown(dtDept);
            return View(new GateMaster { InwardAllowed = true, OutwardAllowed = true, IsActive = true });
        }

        [HttpPost]
        public IActionResult Create(GateMaster gate)
        {
            if (ModelState.IsValid)
            {
                _busLayer.ManageGateMaster("INSERT", gate);
                return RedirectToAction("Index");
            }
            DataTable dtDept = _busLayer.ManageDepartmentMaster("GETALL");
            ViewBag.Departments = Allclass.CreateDropdown(dtDept);
            return View(gate);
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            _busLayer.ManageGateMaster("DELETE", new GateMaster { GateId = id });
            return RedirectToAction("Index");
        }
    }
}
