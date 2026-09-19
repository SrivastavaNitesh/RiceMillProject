using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RiceMillProject.BAL;
using RiceMillProject.Models;
using System.Data;

namespace RiceMillProject.Controllers
{
    [Authorize(Roles = "Admin")]
    public class GateMasterController : Controller
    {
        private readonly BusinessLayer _busLayer;
        private readonly LocationMasterBAL _locationBal;

        public GateMasterController(IConfiguration configuration)
        {
            _busLayer = new BusinessLayer(configuration);
            _locationBal = new LocationMasterBAL(configuration);
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
                        OfficeId = row["OfficeId"] != DBNull.Value ? Convert.ToInt32(row["OfficeId"]) : 0,
                        OfficeName = row["OfficeName"] != DBNull.Value ? row["OfficeName"].ToString() : "",
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
            PopulateOffices();
            return View(new GateMaster { InwardAllowed = true, OutwardAllowed = true, IsActive = true });
        }

        [HttpPost]
        public IActionResult Create(GateMaster gate)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var result = _busLayer.ManageGateMaster("INSERT", gate);
                    if (result == null || result.Rows.Count == 0)
                        throw new InvalidOperationException("Database did not save the gate.");
                    TempData["SuccessMessage"] = "Gate saved successfully!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Gate could not be saved: " + ex.Message;
                }
            }
            TempData["ErrorMessage"] ??= "Please fill all mandatory gate fields.";
            PopulateOffices(gate.OfficeId);
            return View(gate);
        }

        private void PopulateOffices(int? selectedOfficeId = null)
        {
            ViewBag.Offices = new SelectList(
                _locationBal.GetActiveOffices(), "OfficeId", "OfficeName", selectedOfficeId);
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            _busLayer.ManageGateMaster("DELETE", new GateMaster { GateId = id });
            return RedirectToAction("Index");
        }
    }
}
