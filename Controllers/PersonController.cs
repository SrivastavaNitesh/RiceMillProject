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
        private readonly BusinessLayer _BusLayer;
        public PersonController(IConfiguration configuration)
        {
            _personBal = new PersonBAL(configuration);
            _BusLayer = new BusinessLayer(configuration);
        }

        public IActionResult Index()
        {
            string Mode = "oth";
            var persons = _personBal.GetEmployeeList(Mode);
            return View(persons);
        }

        public IActionResult EmployeeList()
        {
            string Mode = "emp";
            var persons = _personBal.GetEmployeeList(Mode);
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
            if (!_personBal.IsMobileNumberUnique(person.MobileNumber))
            {
                ModelState.AddModelError("MobileNumber", "This mobile number is already registered.");
            }

            if (ModelState.IsValid)
            {
                _personBal.AddPerson(person);
                return RedirectToAction("Index");
            }

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
            return View(new Person { IsActive = true });
        }

        [HttpPost]
        public IActionResult Employee(Person person)
        {
            if (person.OfficeId <= 0)
                ModelState.AddModelError(nameof(person.OfficeId), "Company selection is required.");

            if (person.PersonType == "1" && (!person.GateId.HasValue || person.GateId <= 0))
                ModelState.AddModelError(nameof(person.GateId), "Gate selection is required for Gateman.");

            if (person.PersonType == "7" && person.MethdesignatationId.GetValueOrDefault() <= 0)
                ModelState.AddModelError(nameof(person.MethdesignatationId), "Meth selection is required for Worker.");

            if (!_personBal.IsMobileNumberUnique(person.MobileNumber))
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
                        TempData["SuccessMessage"] = $"Employee '{person.PersonName}' registered successfully!";
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
