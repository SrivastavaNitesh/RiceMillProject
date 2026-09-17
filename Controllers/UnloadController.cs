using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using RiceMillProject.BAL;
using RiceMillProject.Models;
using System.Linq;
using System.Collections.Generic;
using System.Data;

namespace RiceMillProject.Controllers
{
    public class UnloadController : Controller
    {
        private readonly UnloadTransactionBAL _unloadBal;
        private readonly GateEntryBAL _gateBal;
        private readonly PersonBAL _personBal;
        private readonly ItemBAL _itemBal;

        public UnloadController(IConfiguration configuration)
        {
            _unloadBal = new UnloadTransactionBAL(configuration);
            _gateBal = new GateEntryBAL(configuration);
            _personBal = new PersonBAL(configuration);
            _itemBal = new ItemBAL(configuration);
        }

        public IActionResult Index()
        {
            var gateEntries = _gateBal.GetAllGateEntries();
            var unloads = _unloadBal.GetAllUnloading();
            
            // Pending Assignment: GateEntries that have no UnloadTransaction
            var assignedRsts = unloads.Select(u => u.RSTNumber).Distinct().ToList();
            var pendingAssignments = gateEntries.Where(e => !assignedRsts.Contains(e.RSTNumber)).ToList();

            // Pending Verification: UnloadTransactions
            var pendingVerifications = unloads.Where(u => u.Status == "Unloaded" || u.Status == "Assigned").ToList();
            
            ViewBag.PendingAssignments = pendingAssignments;
            ViewBag.PendingVerifications = pendingVerifications;

            return View(unloads);
        }

        [HttpGet]
        public IActionResult Assign(string rstNumber)
        {
            ViewBag.RSTNumber = rstNumber;
            var allPersons = _personBal.GetAllPersons();
            
            int currentSupervisorId = 0;
            var personClaim = User.Claims.FirstOrDefault(c => c.Type == "PersonId")?.Value;
            if (!string.IsNullOrEmpty(personClaim) && int.TryParse(personClaim, out int pid))
            {
                currentSupervisorId = pid;
            }

            ViewBag.Supervisors = new SelectList(allPersons.Where(p => p.PersonType == "Supervisor"), "PersonId", "PersonName", currentSupervisorId);
            ViewBag.Meths = new SelectList(allPersons.Where(p => p.PersonType == "Meth"), "PersonId", "PersonName");
            ViewBag.Items = new SelectList(_itemBal.GetAllItems().Where(i => i.IsActive), "ItemId", "ItemName");
            
            return View(new UnloadTransaction { RSTNumber = rstNumber, SupervisorId = currentSupervisorId });
        }

        [HttpPost]
        public IActionResult Assign(UnloadTransaction unload)
        {
            if (unload.SupervisorId <= 0)
            {
                var personClaim = User.Claims.FirstOrDefault(c => c.Type == "PersonId")?.Value;
                if (!string.IsNullOrEmpty(personClaim) && int.TryParse(personClaim, out int pid))
                {
                    unload.SupervisorId = pid;
                }
            }

            if (unload.SupervisorId > 0 && unload.MethId > 0)
            {
                int unloadId = _unloadBal.AssignUnloading(unload.RSTNumber, unload.SupervisorId, unload.MethId, unload.ItemId);
                return RedirectToAction("PrintChallan", new { id = unloadId });
            }
            
            var allPersons = _personBal.GetAllPersons();
            ViewBag.Supervisors = new SelectList(allPersons.Where(p => p.PersonType == "Supervisor"), "PersonId", "PersonName", unload.SupervisorId);
            ViewBag.Meths = new SelectList(allPersons.Where(p => p.PersonType == "Meth"), "PersonId", "PersonName", unload.MethId);
            ViewBag.Items = new SelectList(_itemBal.GetAllItems().Where(i => i.IsActive), "ItemId", "ItemName", unload.ItemId);
            return View(unload);
        }

        [HttpGet]
        public IActionResult Verify(int id)
        {
            var unload = _unloadBal.GetAllUnloading().FirstOrDefault(u => u.UnloadId == id);
            if (unload == null) return NotFound();
            
            return View(unload);
        }

        [HttpPost]
        public IActionResult Verify(int UnloadId, decimal TotalBagDeductionGrams)
        {
            _unloadBal.VerifyUnloading(UnloadId, TotalBagDeductionGrams);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult PrintChallan(int id)
        {
            var unload = _unloadBal.GetAllUnloading().FirstOrDefault(u => u.UnloadId == id);
            if (unload == null) return NotFound();
            return View(unload);
        }
    }
}
