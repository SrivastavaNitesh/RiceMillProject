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
                DataTable dtInward = _busLayer.GetActiveInwardEntriesForDropdown();
                ViewBag.InwardEntries = Allclass.CreateDropdown(dtInward);

                ViewBag.Vehicles = new SelectList(_vehicleBal.GetAllVehicles(), "VehicleId", "VehicleNumber");
                var allPersons = _personBal.GetAllPersons();
                ViewBag.Parties = new SelectList(allPersons.Where(p => p.PersonType == "Kisan" || p.PersonType == "Government Office"), "PersonId", "PersonName");
                ViewBag.Drivers = new SelectList(allPersons.Where(p => p.PersonType == "Driver"), "PersonId", "PersonName");
                
                DataTable dtOffice = _busLayer.GetAllMainoffice();
                ViewBag.Offices = Allclass.CreateDropdown(dtOffice);
                
                return View(new GateEntry());
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while loading the page: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public IActionResult Create(GateEntry entry)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    _gateBal.CreateGateEntry(entry);
                    return RedirectToAction("Index");
                }
                
                DataTable dtInward = _busLayer.GetActiveInwardEntriesForDropdown();
                ViewBag.InwardEntries = Allclass.CreateDropdown(dtInward);

                ViewBag.Vehicles = new SelectList(_vehicleBal.GetAllVehicles(), "VehicleId", "VehicleNumber", entry.VehicleId);
                var allPersons = _personBal.GetAllPersons();
                ViewBag.Parties = new SelectList(allPersons.Where(p => p.PersonType == "Kisan" || p.PersonType == "Government Office"), "PersonId", "PersonName", entry.PartyId);
                ViewBag.Drivers = new SelectList(allPersons.Where(p => p.PersonType == "Driver"), "PersonId", "PersonName", entry.DriverId);
                
                DataTable dtOffice = _busLayer.GetAllMainoffice();
                ViewBag.Offices = Allclass.CreateDropdown(dtOffice);
                
                return View(entry);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
                return RedirectToAction("Index");
            }
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
    }
}
