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
        private readonly PostBAL _postBal;

        public OfficeLocationMappingController(IConfiguration configuration)
        {
            _locationBal = new LocationMasterBAL(configuration);
            _postBal = new PostBAL(configuration);
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
                _locationBal.SaveOfficeLocationMapping(model);
                TempData["SuccessMessage"] = "Office-Location mapping saved successfully!";
                return RedirectToAction(nameof(Index));
            }
            PopulateDropdowns();
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

        private void PopulateDropdowns()
        {
            var offices = _postBal.GetAllPosts();
            var locations = _locationBal.GetAllLocations();

            ViewBag.Offices = new SelectList(offices, "PostId", "PostName");
            ViewBag.Locations = new SelectList(locations, "LocationId", "LocationName");
        }
    }
}
