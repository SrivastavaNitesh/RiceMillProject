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
        private readonly LabWorkflowBAL _labWorkflow;
        private readonly BagTypeBAL _bagBal;
        private readonly OfficeBAL _officeBal;

        public MethController(IConfiguration configuration)
        {
            _unloadBal = new UnloadTransactionBAL(configuration);
            _labWorkflow = new LabWorkflowBAL(configuration);
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

            var allPersons = _unloadBal.GetUnloadingPeople();
            ViewBag.GateMen = new SelectList(allPersons.Where(p => p.PersonType == "Gate Man"), "PersonId", "PersonName");
            ViewBag.Workers = allPersons.Where(p => p.PersonType == "Worker").ToList();
            ViewBag.BagTypes = new SelectList(_bagBal.GetAllBagTypes(), "BagTypeId", "BagTypeName");
            ViewBag.Locations = new SelectList(_officeBal.GetAllLocations(), "LocationId", "LocationName");
            ViewBag.UnloadItems = _labWorkflow.GetItems();
            
            return View(unload);
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult ExecuteUnload(UnloadTransaction unload, int[] SelectedWorkers, string Shift, int ActualLocationId)
        {
            if (unload.BagTypeId.GetValueOrDefault() <= 0 || unload.NumberOfBags.GetValueOrDefault() <= 0 || ActualLocationId <= 0)
                ModelState.AddModelError("", "Select a bag type, unloading location and a positive number of bags.");
            if (ModelState.IsValid)
            {
                try
                {
                    _unloadBal.CompleteWithItems(unload, ActualLocationId, Shift, SelectedWorkers);
                    return RedirectToAction("PrintSlip", new { id = unload.UnloadId });
                }
                catch (Microsoft.Data.SqlClient.SqlException ex)
                {
                    ModelState.AddModelError("", ex.Number == 50001 ? ex.Message : "Unloading could not be saved. Reload the assignment and try again.");
                }
            }

            var allPersons = _unloadBal.GetUnloadingPeople();
            ViewBag.GateMen = new SelectList(allPersons.Where(p => p.PersonType == "Gate Man"), "PersonId", "PersonName", unload.GateManId);
            ViewBag.Workers = allPersons.Where(p => p.PersonType == "Worker").ToList();
            ViewBag.BagTypes = new SelectList(_bagBal.GetAllBagTypes(), "BagTypeId", "BagTypeName", unload.BagTypeId);
            ViewBag.Locations = new SelectList(_officeBal.GetAllLocations(), "LocationId", "LocationName", ActualLocationId);
            ViewBag.UnloadItems = _labWorkflow.GetItems();
            ViewBag.SelectedWorkers = SelectedWorkers;
            ViewBag.SelectedShift = Shift;

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
