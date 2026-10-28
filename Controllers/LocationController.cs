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

        public IActionResult Index(int? subCompanyId)
        {
            var locations = _locationBal.GetAllLocations();
            var subCompanies = locations
                .Where(item => item.SubCompanyId > 0)
                .GroupBy(item => new { item.SubCompanyId, item.SubCompanyName })
                .Select(group => new SubCompanyOption
                {
                    SubCompanyId = group.Key.SubCompanyId,
                    SubCompanyName = group.Key.SubCompanyName ?? "Unassigned Subcompany"
                })
                .OrderBy(item => item.SubCompanyName)
                .ToList();

            if (subCompanyId.GetValueOrDefault() > 0)
                locations = locations.Where(item => item.SubCompanyId == subCompanyId).ToList();

            ViewBag.SubCompanyFilter = new SelectList(subCompanies, "SubCompanyId", "SubCompanyName", subCompanyId);
            ViewBag.SelectedSubCompanyId = subCompanyId;
            return View(locations);
        }

        [HttpGet]
        public IActionResult Create()
        {
            PopulateHierarchy();
            return View(new LocationMaster { IsActive = true, CapacityUnit = "MT" });
        }

        [HttpPost]
        public IActionResult Create(LocationMaster model)
        {
            if (model.CompanyId > 0)
                model.OfficeId = _locationBal.GetOfficeIdForCompany(model.CompanyId);
            ModelState.Remove(nameof(model.OfficeId));

            if (model.OfficeId <= 0)
                ModelState.AddModelError(nameof(model.CompanyId), "Valid main company selection is required");

            if (model.SubCompanyId > 0 && !_locationBal.IsSubCompanyInCompany(model.SubCompanyId, model.CompanyId))
                ModelState.AddModelError(nameof(model.SubCompanyId), "Selected subcompany does not belong to the main company");

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
            TempData["ErrorMessage"] ??= "Please select Main Company, Location Name and Location Type.";
            PopulateHierarchy(model.CompanyId, model.SubCompanyId);
            return View(model);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var model = _locationBal.GetLocationById(id);
            if (model == null) return NotFound();
            PopulateHierarchy(model.CompanyId, model.SubCompanyId);
            return View("Create", model);
        }

        [HttpGet]
        public IActionResult GetSubCompanies(int companyId)
        {
            return Json(_locationBal.GetActiveSubCompanies(companyId)
                .Select(item => new { id = item.SubCompanyId, name = item.SubCompanyName }));
        }

        private void PopulateHierarchy(int? selectedCompanyId = null, int? selectedSubCompanyId = null)
        {
            ViewBag.Companies = new SelectList(
                _locationBal.GetActiveCompanies(), "CompanyId", "CompanyName", selectedCompanyId);
            ViewBag.SubCompanies = new SelectList(
                selectedCompanyId.GetValueOrDefault() > 0
                    ? _locationBal.GetActiveSubCompanies(selectedCompanyId!.Value)
                    : new List<SubCompanyOption>(),
                "SubCompanyId", "SubCompanyName", selectedSubCompanyId);
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
