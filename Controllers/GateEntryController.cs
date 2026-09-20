using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using RiceMillProject.BAL;
using RiceMillProject.Models;
using System.Data;
using System.Linq;

namespace RiceMillProject.Controllers
{
    public class GateEntryController : Controller
    {
        private readonly GateEntryBAL _gateBal;
        private readonly VehicleBAL _vehicleBal;
        private readonly PersonBAL _personBal;
        private readonly OfficeBAL _officeBal;
        private readonly BusinessLayer _busLayer;

        public GateEntryController(IConfiguration configuration)
        {
            _gateBal = new GateEntryBAL(configuration);
            _vehicleBal = new VehicleBAL(configuration);
            _personBal = new PersonBAL(configuration);
            _officeBal = new OfficeBAL(configuration);
            _busLayer = new BusinessLayer(configuration);
        }

        public IActionResult Index()
        {
            try
            {
                var entries = _gateBal.GetAllGateEntries();
                return View(entries);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while loading entries: " + ex.Message;
                return View(new List<GateEntry>());
            }
        }

        [HttpGet]
        public IActionResult Create()
        {
            try
            {
                PopulateDropdowns(new GateEntry());
                return View(new GateEntry());
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while loading the page: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(GateEntry entry)
        {
            if (string.IsNullOrWhiteSpace(entry.InwardNo)) ModelState.AddModelError("InwardNo", "Select a pending inward number.");
            if (entry.GrossWeight <= 0) ModelState.AddModelError("GrossWeight", "Enter a positive gross weight.");
            if (ModelState.IsValid)
            {
                try
                {
                    string rst = _gateBal.CreateGateEntry(entry);
                    TempData["SuccessMessage"] = $"RST Generated Successfully! RST Number: {rst}";
                    return RedirectToAction(nameof(Create));
                }
                catch (Microsoft.Data.SqlClient.SqlException ex)
                {
                    ModelState.AddModelError("", ex.Number == 50001 ? ex.Message : "Unable to generate RST. Please reload and try again.");
                }
            }
            PopulateDropdowns(entry);
            return View(entry);
        }

        private void PopulateDropdowns(GateEntry entry)
        {
            ViewBag.PendingInwards = new List<PendingRstInward>();
            ViewBag.Offices = Enumerable.Empty<SelectListItem>();
            try { ViewBag.PendingInwards = _gateBal.GetPendingRstInwards(); }
            catch (Exception) { ModelState.AddModelError("", "Pending inwards could not be loaded. Check the database connection and GateEntry_Inward_Workflow.sql migration."); }
            try { ViewBag.Offices = new SelectList(_officeBal.GetAllOffices().Where(x => x.IsActive), "OfficeId", "OfficeName", entry.TargetOfficeId); }
            catch (Exception) { ModelState.AddModelError("", "Company names could not be loaded."); }
        }
        [HttpGet]
        public IActionResult Outbound(string id)
        {
            try
            {
                var entry = _gateBal.GetAllGateEntries().Find(e => e.RSTNumber == id);
                if (entry == null) return NotFound();
                return View(entry);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public IActionResult Outbound(string rstNumber, decimal tareWeight)
        {
            try
            {
                _gateBal.CompleteGateExit(rstNumber, tareWeight);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public IActionResult Print(string id)
        {
            try
            {
                var entry = _gateBal.GetAllGateEntries().Find(e => e.RSTNumber == id);
                if (entry == null) return NotFound();
                return View(entry);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public IActionResult GetInwardDetails(string inwardNo)
        {
            try
            {
                var dt = _gateBal.GetInwardDetails(inwardNo);
                if (dt != null && dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    return Json(new
                    {
                        success = true,
                        inwardNo = row["InwardNo"]?.ToString(),
                        partyId = row["PartyId"] != DBNull.Value ? Convert.ToInt32(row["PartyId"]) : (int?)null,
                        partyName = row["PartyName"]?.ToString(),
                        vehicleId = row["VehicleId"] != DBNull.Value ? Convert.ToInt32(row["VehicleId"]) : (int?)null,
                        vehicleNo = row["VehicleNo"]?.ToString(),
                        driverId = row["DriverId"] != DBNull.Value ? Convert.ToInt32(row["DriverId"]) : (int?)null,
                        driverName = row["DriverName"]?.ToString(),
                        driverMobile = row["DriverMobile"]?.ToString()
                    });
                }
                return Json(new { success = false, message = "Inward details not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetLinkageByParty(int partyId)
        {
            try
            {
                var data = _gateBal.GetLinkageByParty(partyId);
                return Json(new { success = true, vehicleId = data.vehicleId, driverId = data.driverId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetPartyOptions(int partyId)
        {
            try
            {
                var vehicles = _gateBal.GetVehiclesByParty(partyId);
                var drivers = _gateBal.GetDriversByParty(partyId);
                
                int defaultVehicleId = 0;
                int defaultDriverId = 0;
                
                foreach (dynamic v in vehicles)
                {
                    if (v.IsLinked)
                    {
                        defaultVehicleId = v.VehicleId;
                        break;
                    }
                }
                foreach (dynamic d in drivers)
                {
                    if (d.IsLinked)
                    {
                        defaultDriverId = d.DriverId;
                        break;
                    }
                }

                return Json(new
                {
                    success = true,
                    vehicles = vehicles,
                    drivers = drivers,
                    defaultVehicleId = defaultVehicleId,
                    defaultDriverId = defaultDriverId
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
