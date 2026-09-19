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
            if (!_personBal.IsMobileNumberUnique(person.MobileNumber))
            {
                ModelState.AddModelError("MobileNumber", "This mobile number is already registered.");
            }

            if (ModelState.IsValid)
            {
                int newId = _personBal.AddPerson(person);
                if (newId > 0)
                {
                    TempData["SuccessMessage"] = $"Gateman / Employee '{person.PersonName}' registered successfully!";
                    return RedirectToAction("EmployeeList");
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to register employee. Please try again.";
                }
            }
            DataTable dt = _BusLayer.GetEmployeePost();
            ViewBag.Post = Allclass.CreateDropdown(dt);
            dt = _BusLayer.GetAllMainoffice();
            ViewBag.Offices = Allclass.CreateDropdown(dt);
            dt = _BusLayer.ManageDepartmentMaster("GETALL");
            ViewBag.Departments = Allclass.CreateDropdown(dt);
            return View(person);
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
