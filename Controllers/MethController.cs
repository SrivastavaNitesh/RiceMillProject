using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using RiceMillProject.BAL;
using RiceMillProject.Models;
using System.Linq;

namespace RiceMillProject.Controllers
{
    public class MethController : Controller
    {
        private readonly UnloadTransactionBAL _unloadBal;
        private readonly PersonBAL _personBal;
        private readonly BagTypeBAL _bagBal;
        private readonly OfficeBAL _officeBal;

        public MethController(IConfiguration configuration)
        {
            _unloadBal = new UnloadTransactionBAL(configuration);
            _personBal = new PersonBAL(configuration);
            _bagBal = new BagTypeBAL(configuration);
            _officeBal = new OfficeBAL(configuration);
        }

        public IActionResult Index()
        {
            var unloads = _unloadBal.GetAllUnloading();
            
            // Pending Unloads: Assigned to a Meth, but not yet unloaded.
            var pendingUnloads = unloads.Where(u => u.Status == "Assigned").ToList();
            
            // Completed Unloads: Unloaded but waiting for verification, or verified
            var completedUnloads = unloads.Where(u => u.Status == "Unloaded" || u.Status == "Verified").ToList();

            ViewBag.PendingUnloads = pendingUnloads;
            ViewBag.CompletedUnloads = completedUnloads;

            return View();
        }

        [HttpGet]
        public IActionResult ExecuteUnload(int id)
        {
            var unload = _unloadBal.GetAllUnloading().FirstOrDefault(u => u.UnloadId == id);
            if (unload == null) return NotFound();

            var allPersons = _personBal.GetAllPersons();
            ViewBag.GateMen = new SelectList(allPersons.Where(p => p.PersonType == "Gate Man"), "PersonId", "PersonName");
            ViewBag.Workers = allPersons.Where(p => p.PersonType == "Worker").ToList();
            ViewBag.BagTypes = new SelectList(_bagBal.GetAllBagTypes(), "BagTypeId", "BagTypeName");
            ViewBag.Locations = new SelectList(_officeBal.GetAllLocations(), "LocationId", "LocationName");
            
            return View(unload);
        }

        [HttpPost]
        public IActionResult ExecuteUnload(UnloadTransaction unload, int[] SelectedWorkers, string Shift, int ActualLocationId)
        {
            if (ModelState.IsValid)
            {
                // BagTypeId and NumberOfBags are nullable in model but required for this step
                if (unload.BagTypeId.HasValue && unload.NumberOfBags.HasValue)
                {
                    _unloadBal.SubmitUnloading(unload.UnloadId, unload.GateManId, unload.BagTypeId.Value, unload.NumberOfBags.Value);
                    
                    if (ActualLocationId > 0)
                    {
                        _unloadBal.SaveUnloadLocation(unload.UnloadId, ActualLocationId);
                    }
                    
                    decimal palledariAmount = Shift == "Night" ? 15.0m : 10.0m;
                    if (SelectedWorkers != null)
                    {
                        foreach (var workerId in SelectedWorkers)
                        {
                            _unloadBal.SaveWorkerAllocation(unload.UnloadId, workerId, palledariAmount);
                        }
                    }

                    return RedirectToAction("PrintSlip", new { id = unload.UnloadId });
                }
            }

            var allPersons = _personBal.GetAllPersons();
            ViewBag.GateMen = new SelectList(allPersons.Where(p => p.PersonType == "Gate Man"), "PersonId", "PersonName", unload.GateManId);
            ViewBag.Workers = allPersons.Where(p => p.PersonType == "Worker").ToList();
            ViewBag.BagTypes = new SelectList(_bagBal.GetAllBagTypes(), "BagTypeId", "BagTypeName", unload.BagTypeId);
            ViewBag.Locations = new SelectList(_officeBal.GetAllLocations(), "LocationId", "LocationName");

            return View(unload);
        }

        [HttpGet]
        public IActionResult PrintSlip(int id)
        {
            var unload = _unloadBal.GetAllUnloading().FirstOrDefault(u => u.UnloadId == id);
            if (unload == null) return NotFound();
            return View(unload);
        }
    }
}
