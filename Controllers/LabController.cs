using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using RiceMillProject.BAL;
using RiceMillProject.Models;
using System.Linq;

namespace RiceMillProject.Controllers
{
    public class LabController : Controller
    {
        private readonly LabQualityCheckBAL _labBal;
        private readonly GateEntryBAL _gateBal;
        private readonly PersonBAL _personBal;

        public LabController(IConfiguration configuration)
        {
            _labBal = new LabQualityCheckBAL(configuration);
            _gateBal = new GateEntryBAL(configuration);
            _personBal = new PersonBAL(configuration);
        }

        public IActionResult Index()
        {
            // Show only trucks that are 'Entered' or 'Exited' but not yet Tested or Settled
            var pendingTests = _gateBal.GetAllGateEntries()
                .Where(g => g.Status == "Entered" || g.Status == "Exited")
                .ToList();
            return View(pendingTests);
        }

        [HttpGet]
        public IActionResult Create(string rstNumber)
        {
            ViewBag.RSTNumber = rstNumber;
            var allPersons = _personBal.GetAllPersons();
            // Assuming we added a 'Lab Technician' PersonType
            ViewBag.LabTechs = new SelectList(allPersons.Where(p => p.PersonType == "Employee"), "PersonId", "PersonName");
            
            return View(new LabQualityCheck { RSTNumber = rstNumber });
        }

        [HttpPost]
        public IActionResult Create(LabQualityCheck labCheck)
        {
            if (ModelState.IsValid)
            {
                _labBal.SaveLabQualityCheck(labCheck);
                return RedirectToAction("Index");
            }
            
            ViewBag.RSTNumber = labCheck.RSTNumber;
            var allPersons = _personBal.GetAllPersons();
            ViewBag.LabTechs = new SelectList(allPersons.Where(p => p.PersonType == "Employee"), "PersonId", "PersonName", labCheck.TestedBy);
            
            return View(labCheck);
        }
    }
}
