using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
            PopulateOffices();
            return View(new LocationMaster { IsActive = true, CapacityUnit = "MT" });
        }

        [HttpPost]
        public IActionResult Create(LocationMaster model)
        {
            if (model.OfficeId <= 0 && ModelState[nameof(model.OfficeId)]?.Errors.Count == 0)
                ModelState.AddModelError(nameof(model.OfficeId), "Company selection is required");

            if (string.IsNullOrWhiteSpace(model.LocationName) && ModelState[nameof(model.LocationName)]?.Errors.Count == 0)
                ModelState.AddModelError(nameof(model.LocationName), "Location Name is required");

            if (string.IsNullOrWhiteSpace(model.LocationType) && ModelState[nameof(model.LocationType)]?.Errors.Count == 0)
                ModelState.AddModelError(nameof(model.LocationType), "Location Type is required");

            if (ModelState.IsValid)
            {
                try
                {
                    _locationBal.SaveLocation(model);
                    TempData["SuccessMessage"] = "Location saved successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                    TempData["ErrorMessage"] = "Location could not be saved: " + ex.Message;
                }
            }
            TempData["ErrorMessage"] ??= "Please select Company, Location Name and Location Type.";
            PopulateOffices(model.OfficeId);
            return View(model);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var model = _locationBal.GetLocationById(id);
            if (model == null) return NotFound();
            PopulateOffices(model.OfficeId);
            return View("Create", model);
        }

        private void PopulateOffices(int? selectedOfficeId = null)
        {
            ViewBag.Offices = new SelectList(
                _locationBal.GetActiveOffices(), "OfficeId", "OfficeName", selectedOfficeId);
            ViewBag.LocationTypes = _locationBal.GetActiveLocationTypes();
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
