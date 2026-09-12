using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RiceMillProject.BAL;
using RiceMillProject.Models;

namespace RiceMillProject.Controllers
{
    [Authorize(Roles = "Admin, Gate Man")]
    public class LocationController : Controller
    {
        private readonly LocationMasterBAL _locationBal;

        public LocationController(IConfiguration configuration)
        {
            _locationBal = new LocationMasterBAL(configuration);
        }

        public IActionResult Index()
        {
            var locations = _locationBal.GetAllLocations();
            return View(locations);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new LocationMaster { IsActive = true, CapacityUnit = "MT" });
        }

        [HttpPost]
        public IActionResult Create(LocationMaster model)
        {
            if (ModelState.IsValid)
            {
                _locationBal.SaveLocation(model);
                TempData["SuccessMessage"] = "Location master saved successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var model = _locationBal.GetLocationById(id);
            if (model == null) return NotFound();
            return View("Create", model);
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            _locationBal.DeleteLocation(id);
            TempData["SuccessMessage"] = "Location deactivated successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}
