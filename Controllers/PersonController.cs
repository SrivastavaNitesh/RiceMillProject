using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RiceMillProject.BAL;
using RiceMillProject.Models;
using System.Data;

namespace RiceMillProject.Controllers
{
    [Authorize]
    public class PersonController : Controller
    {
        private readonly PersonBAL _personBal;
        private readonly OfficeBAL _officeBal;
        private readonly BusinessLayer _BusLayer;
        public PersonController(IConfiguration configuration)
        {
            _personBal = new PersonBAL(configuration);
            _officeBal = new OfficeBAL(configuration);
            _BusLayer = new BusinessLayer(configuration);
        }

        public IActionResult Index()
        {
            string Mode = "oth";
            var persons = _personBal.GetEmployeeList(Mode);
            return View(persons);
        }

        public IActionResult EmployeeList(int? officeId)
        {
            var offices = Allclass.CreateDropdown(_BusLayer.GetAllMainoffice());
            offices[0].Text = "All Companies";
            offices[0].Value = "";
            ViewBag.Offices = offices;
            ViewBag.SelectedOfficeId = officeId;
            string Mode = "emp";
            var persons = _personBal.GetEmployeeList(Mode, officeId);
            return View(persons);
        }

        [HttpGet]
        public IActionResult Create()
        {
            DataTable dt = _BusLayer.GetPost();
            ViewBag.Post = Allclass.CreateDropdown(dt);
            dt = _BusLayer.GetAllMainoffice();
            ViewBag.Offices = Allclass.CreateDropdown(dt);
            dt = _BusLayer.GetAllMeth();
            ViewBag.Meth = Allclass.CreateDropdown(dt);
            dt = _BusLayer.ManageDepartmentMaster("GETALL");
            ViewBag.Departments = Allclass.CreateDropdown(dt);
            return View(new Person());
        }

        [HttpPost]
        public IActionResult Create(Person person)
        {
            person.ShareCompany = person.PersonType == "8" && person.ShareCompany;

            if (!person.ShareCompany && !_personBal.IsMobileNumberUnique(person.MobileNumber))
            {
                ModelState.AddModelError("MobileNumber", "This mobile number is already registered.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    int newId = _personBal.AddPerson(person);
                    if (newId > 0)
                    {
                        TempData["SuccessMessage"] = person.ShareCompany
                            ? $"Existing person '{person.PersonName}' assigned to the selected company successfully!"
                            : $"{GetPersonRoleName(person.PersonType)} '{person.PersonName}' saved successfully!";
                        return RedirectToAction("Index");
                    }

                    ModelState.AddModelError(string.Empty, "Database did not return the new person ID.");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
            }

            var errors = ModelState.Values
                .SelectMany(value => value.Errors)
                .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                    ? error.Exception?.Message
                    : error.ErrorMessage)
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .Distinct()
                .ToList();
            TempData["ErrorMessage"] = errors.Count > 0
                ? string.Join(" ", errors)
                : "Person could not be saved. Please verify the entered details.";

            DataTable dt = _BusLayer.GetPost();
            ViewBag.Post = Allclass.CreateDropdown(dt);
            dt = _BusLayer.GetAllMainoffice();
            ViewBag.Offices = Allclass.CreateDropdown(dt);
            dt = _BusLayer.GetAllMeth();
            ViewBag.Meth = Allclass.CreateDropdown(dt);
            dt = _BusLayer.ManageDepartmentMaster("GETALL");
            ViewBag.Departments = Allclass.CreateDropdown(dt);
            return View(person);
        }

        [HttpGet]
        public IActionResult GetWorkersByMeth(int methPersonId)
        {
            DataTable dt = _BusLayer.GetWorkersByMeth(methPersonId);
            var workers = Allclass.CreateDropdown(dt);
            return Json(workers);
        }

        [HttpGet]
        public IActionResult Employee()
        {   
            DataTable dt = _BusLayer.GetEmployeePost();
            ViewBag.Post = Allclass.CreateDropdown(dt);
            dt = _BusLayer.GetAllMainoffice();
            ViewBag.Offices = Allclass.CreateDropdown(dt);
            dt = _BusLayer.ManageDepartmentMaster("GETALL");
            ViewBag.Departments = Allclass.CreateDropdown(dt);
            return View(new Person
            {
                IsActive = true,
                OfficeId = User.CompanyId()
            });
        }

        [HttpPost]
        public IActionResult Employee(Person person)
        {
            person.ShareCompany = person.PersonType == "8" && person.ShareCompany;

            if (person.OfficeId <= 0)
                ModelState.AddModelError(nameof(person.OfficeId), "Company selection is required.");

            if (person.PersonType == "1" && (!person.GateId.HasValue || person.GateId <= 0))
                ModelState.AddModelError(nameof(person.GateId), "Gate selection is required for Gateman.");

            if (person.PersonType == "7" && person.MethdesignatationId.GetValueOrDefault() <= 0)
                ModelState.AddModelError(nameof(person.MethdesignatationId), "Meth selection is required for Worker.");

            if (!person.ShareCompany && !_personBal.IsMobileNumberUnique(person.MobileNumber))
            {
                ModelState.AddModelError("MobileNumber", "This mobile number is already registered.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    int newId = _personBal.AddPerson(person);
                    if (newId > 0)
                    {
                        TempData["SuccessMessage"] = person.ShareCompany
                            ? $"Existing person '{person.PersonName}' assigned to the selected company successfully!"
                            : $"Employee '{person.PersonName}' registered successfully!";
                        return RedirectToAction("EmployeeList");
                    }

                    ModelState.AddModelError(string.Empty, "Database did not return the new employee ID.");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
            }

            var errors = ModelState.Values
                .SelectMany(value => value.Errors)
                .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                    ? error.Exception?.Message
                    : error.ErrorMessage)
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .Distinct()
                .ToList();
            TempData["ErrorMessage"] = errors.Count > 0
                ? string.Join(" ", errors)
                : "Employee could not be saved. Please verify the entered details.";

            DataTable dt = _BusLayer.GetEmployeePost();
            ViewBag.Post = Allclass.CreateDropdown(dt);
            dt = _BusLayer.GetAllMainoffice();
            ViewBag.Offices = Allclass.CreateDropdown(dt);
            dt = _BusLayer.ManageDepartmentMaster("GETALL");
            ViewBag.Departments = Allclass.CreateDropdown(dt);
            return View(person);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult QuickAddCompany(string companyName, string location)
        {
            if (!User.IsAdminUser()) return Forbid();

            try
            {
                companyName = (companyName ?? string.Empty).Trim();
                location = (location ?? string.Empty).Trim();
                if (companyName.Length is < 2 or > 150)
                    throw new InvalidOperationException("Company name must be 2 to 150 characters.");
                if (location.Length is < 2 or > 250)
                    throw new InvalidOperationException("Company location is required.");

                var existing = _officeBal.GetAllOffices().FirstOrDefault(office =>
                    office.IsActive && string.Equals(
                        office.OfficeName?.Trim(), companyName,
                        StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                    return Json(new { success = true, officeId = existing.OfficeId, officeName = existing.OfficeName, existing = true });

                int officeId = _officeBal.AddOffice(new Office
                {
                    OfficeName = companyName,
                    Location = location,
                    OfficeTypeId = 1,
                    IsActive = true
                });
                if (officeId <= 0) throw new InvalidOperationException("Company could not be saved.");

                return Json(new { success = true, officeId, officeName = companyName, existing = false });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private string GetPersonRoleName(string? personType)
        {
            if (!int.TryParse(personType, out int postId)) return "Person";
            DataTable posts = _BusLayer.GetPost();
            DataRow? row = posts.AsEnumerable().FirstOrDefault(item =>
                item.Table.Columns.Contains("PostId") && Convert.ToInt32(item["PostId"]) == postId);
            return row != null && row.Table.Columns.Contains("PostName")
                ? row["PostName"]?.ToString() ?? "Person"
                : "Person";
        }

        [HttpGet]
        public IActionResult GetGatesByCompany(int officeId)
        {
            DataTable dt = _BusLayer.GetGatesByOffice(officeId);
            return Json(dt.AsEnumerable().Select(row => new
            {
                value = Convert.ToInt32(row["Value"]),
                text = row["Text"]?.ToString() ?? string.Empty
            }));
        }

        [HttpGet]
        public IActionResult GetMethByCompany(int officeId)
        {
            DataTable dt = _BusLayer.GetMethByOffice(officeId);
            return Json(dt.AsEnumerable().Select(row => new
            {
                value = Convert.ToInt32(row["Value"]),
                text = row["Text"]?.ToString() ?? string.Empty
            }));
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var person = _personBal.GetAllPersons().Find(p => p.PersonId == id);
            if (person == null)
            {
                return NotFound();
            }
            DataTable dt = _BusLayer.GetAllMainoffice();
            ViewBag.Offices = Allclass.CreateDropdown(dt);
            dt = _BusLayer.ManageDepartmentMaster("GETALL");
            ViewBag.Departments = Allclass.CreateDropdown(dt);
            return View(person);
        }

        [HttpPost]
        public IActionResult Edit(Person person)
        {
            if (!_personBal.IsMobileNumberUnique(person.MobileNumber, person.PersonId))
            {
                ModelState.AddModelError("MobileNumber", "This mobile number is already registered to another person.");
            }

            if (ModelState.IsValid)
            {
                _personBal.UpdatePerson(person);
                return RedirectToAction("Index");
            }
            DataTable dt = _BusLayer.GetAllMainoffice();
            ViewBag.Offices = Allclass.CreateDropdown(dt);
            dt = _BusLayer.ManageDepartmentMaster("GETALL");
            ViewBag.Departments = Allclass.CreateDropdown(dt);
            return View(person);
        }
        
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var person = _personBal.GetAllPersons().Find(p => p.PersonId == id);
            if (person != null)
            {
                person.IsActive = false;
                _personBal.UpdatePerson(person);
            }
            return RedirectToAction("Index");
        }
    }
}
