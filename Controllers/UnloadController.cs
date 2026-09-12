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

        public UnloadController(IConfiguration configuration)
        {
            _unloadBal = new UnloadTransactionBAL(configuration);
            _gateBal = new GateEntryBAL(configuration);
            _personBal = new PersonBAL(configuration);
        }

        public IActionResult Index()
        {
            var gateEntries = _gateBal.GetAllGateEntries().Where(e => e.Status == "Entered" || e.Status == "Unloaded").ToList();
            var unloads = _unloadBal.GetAllUnloading();
            
            // Pending Assignment: GateEntries that have no UnloadTransaction
            var assignedRsts = unloads.Select(u => u.RSTNumber).Distinct().ToList();
            var pendingAssignments = gateEntries.Where(e => !assignedRsts.Contains(e.RSTNumber)).ToList();

            // Pending Verification: UnloadTransactions with Status == "Unloaded"
            var pendingVerifications = unloads.Where(u => u.Status == "Unloaded").ToList();
            
            ViewBag.PendingAssignments = pendingAssignments;
            ViewBag.PendingVerifications = pendingVerifications;

            return View();
        }

        [HttpGet]
        public IActionResult Assign(string rstNumber)
        {
            ViewBag.RSTNumber = rstNumber;
            var allPersons = _personBal.GetAllPersons();
            ViewBag.Supervisors = new SelectList(allPersons.Where(p => p.PersonType == "Supervisor"), "PersonId", "PersonName");
            ViewBag.Meths = new SelectList(allPersons.Where(p => p.PersonType == "Meth"), "PersonId", "PersonName");
            DataTable dt=new DataTable();
            
            return View(new UnloadTransaction { RSTNumber = rstNumber });
        }

        [HttpPost]
        public IActionResult Assign(UnloadTransaction unload)
        {
            if (unload.SupervisorId > 0 && unload.MethId > 0)
            {
                int unloadId = _unloadBal.AssignUnloading(unload.RSTNumber, unload.SupervisorId, unload.MethId);
                return RedirectToAction("PrintChallan", new { id = unloadId });
            }
            
            var allPersons = _personBal.GetAllPersons();
            ViewBag.Supervisors = new SelectList(allPersons.Where(p => p.PersonType == "Supervisor"), "PersonId", "PersonName", unload.SupervisorId);
            ViewBag.Meths = new SelectList(allPersons.Where(p => p.PersonType == "Meth"), "PersonId", "PersonName", unload.MethId);
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
