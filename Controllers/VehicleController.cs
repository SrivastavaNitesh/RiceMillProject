using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RiceMillProject.BAL;
using RiceMillProject.Models;
using Microsoft.AspNetCore.Authorization;

namespace RiceMillProject.Controllers
{
    [Authorize(Roles = "Admin, Gate Man")]
    public class VehicleController : Controller
    {
        private readonly VehicleBAL _vehicleBal;

        public VehicleController(IConfiguration configuration)
        {
            _vehicleBal = new VehicleBAL(configuration);
        }

        public IActionResult Index()
        {
            var vehicles = _vehicleBal.GetAllVehicles();
            return View(vehicles);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var _personBal = new PersonBAL(new ConfigurationBuilder().AddJsonFile("appsettings.json").Build());
            var persons = _personBal.GetAllPersons();
            
            ViewBag.Parties = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(persons.FindAll(p => p.PersonType == "Party" || p.PersonType == "Kisan" || p.PersonType == "Center"), "PersonId", "PersonName");
            ViewBag.Drivers = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(persons.FindAll(p => p.PersonType == "Driver"), "PersonId", "PersonName");
            
            return View(new Vehicle { IsActive = true });
        }

        [HttpPost]
        public IActionResult Create(Vehicle vehicle, int? PartyId, int? DriverId)
        {
            if (ModelState.IsValid)
            {
                int vehicleId = _vehicleBal.AddVehicle(vehicle);
                
                // Add mapping if party or driver is selected
                if(vehicleId > 0 && PartyId.HasValue && DriverId.HasValue)
                {
                    var _dal = new RiceMillProject.DAL.VehicleDAL(new ConfigurationBuilder().AddJsonFile("appsettings.json").Build());
                    _dal.InsertVehicleMapping(vehicleId, PartyId.Value, DriverId.Value);
                }
                
                return RedirectToAction("Index");
            }
            return View(vehicle);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var vehicle = _vehicleBal.GetAllVehicles().Find(v => v.VehicleId == id);
            if (vehicle == null)
            {
                return NotFound();
            }
            return View(vehicle);
        }

        [HttpPost]
        public IActionResult Edit(Vehicle vehicle)
        {
            if (ModelState.IsValid)
            {
                _vehicleBal.UpdateVehicle(vehicle);
                return RedirectToAction("Index");
            }
            return View(vehicle);
        }
        
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var vehicle = _vehicleBal.GetAllVehicles().Find(v => v.VehicleId == id);
            if (vehicle != null)
            {
                vehicle.IsActive = false;
                _vehicleBal.UpdateVehicle(vehicle);
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult GetVehicleMapping(int vehicleId)
        {
            var _dal = new RiceMillProject.DAL.VehicleDAL(new ConfigurationBuilder().AddJsonFile("appsettings.json").Build());
            
            // Using ADO.NET directly here for simplicity since it's a specific read
            string constr = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build().GetConnectionString("DefaultConnection") ?? "";
            using (var con = new Microsoft.Data.SqlClient.SqlConnection(constr))
            {
                using (var cmd = new Microsoft.Data.SqlClient.SqlCommand("sp_GetPartiesAndDriversByVehicle", con))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@VehicleId", vehicleId);
                    
                    con.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return Json(new {
                                partyId = reader["PartyId"],
                                partyName = reader["PartyName"]?.ToString(),
                                driverId = reader["DriverId"],
                                driverName = reader["DriverName"]?.ToString(),
                                driverMobile = reader["DriverMobile"]?.ToString()
                            });
                        }
                    }
                }
            }
            return Json(null);
        }
    }
}
