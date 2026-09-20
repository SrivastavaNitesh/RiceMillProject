using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using RiceMillProject.BAL;
using RiceMillProject.Models;

namespace RiceMillProject.Controllers
{
    [Authorize(Roles = "Admin, Gate Man")]
    public class OfficeLocationMappingController : Controller
    {
        private readonly LocationMasterBAL _locationBal;

        public OfficeLocationMappingController(IConfiguration configuration)
        {
            _locationBal = new LocationMasterBAL(configuration);
        }

        public IActionResult Index()
        {
            PopulateDropdowns();
            var mappings = _locationBal.GetAllOfficeLocationMappings();
            ViewBag.NewMapping = new OfficeLocationMapping { EffectiveFrom = DateTime.Now, IsActive = true, UnloadingAllowed = true };
            return View(mappings);
        }

        [HttpPost]
        public IActionResult Create(OfficeLocationMapping model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _locationBal.SaveOfficeLocationMapping(model);
                    TempData["SuccessMessage"] = "Company-Location mapping saved successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Mapping could not be saved: " + ex.Message;
                }
            }
            TempData["ErrorMessage"] ??= "Please select Company and Location.";
            PopulateDropdowns(model.OfficeId, model.LocationId);
            var mappings = _locationBal.GetAllOfficeLocationMappings();
            ViewBag.NewMapping = model;
            return View("Index", mappings);
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            _locationBal.DeleteOfficeLocationMapping(id);
            TempData["SuccessMessage"] = "Mapping deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        private void PopulateDropdowns(int? selectedOfficeId = null, int? selectedLocationId = null)
        {
            var offices = _locationBal.GetActiveOffices();
            var locations = _locationBal.GetAllLocations();

            ViewBag.Offices = new SelectList(offices, "OfficeId", "OfficeName", selectedOfficeId);
            ViewBag.Locations = new SelectList(locations, "LocationId", "LocationName", selectedLocationId);
        }
    }
}
