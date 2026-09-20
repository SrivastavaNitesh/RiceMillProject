using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RiceMillProject.BAL;
using RiceMillProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;

namespace RiceMillProject.Controllers
{
    [Authorize(Policy = "GatemanAccess")]
    public class VehicleController : Controller
    {
        private readonly VehicleBAL _vehicleBal;
        private readonly string _connectionString;

        public VehicleController(IConfiguration configuration)
        {
            _vehicleBal = new VehicleBAL(configuration);
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public IActionResult Index()
        {
            var vehicles = _vehicleBal.GetAllVehicles();
            return View(vehicles);
        }

        [HttpGet]
        public IActionResult Create()
        {
            PopulateLookups();
            return View(new Vehicle { IsActive = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Vehicle vehicle)
        {
            if (!ModelState.IsValid)
            {
                PopulateLookups(vehicle.PartyId, vehicle.DriverId);
                return View(vehicle);
            }

            try
            {
                vehicle.VehicleNumber = vehicle.VehicleNumber.Trim().ToUpperInvariant();
                int vehicleId = _vehicleBal.AddVehicleWithMapping(vehicle);
                if (vehicleId <= 0) throw new InvalidOperationException("Vehicle could not be saved.");
                TempData["SuccessMessage"] = "Vehicle registered and linked with the selected Party and Driver.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Vehicle could not be saved: " + ex.Message);
                PopulateLookups(vehicle.PartyId, vehicle.DriverId);
                return View(vehicle);
            }
        }

        private void PopulateLookups(int selectedPartyId = 0, int selectedDriverId = 0)
        {
            var parties = new List<SelectListItem>();
            var drivers = new List<SelectListItem>();

            using var con = new SqlConnection(_connectionString);
            con.Open();

            const string partyQuery = @"
                SELECT DISTINCT p.PersonId, p.PersonName
                FROM p02_Person p
                INNER JOIN P11_PersonDesignatation pd ON pd.P02_PersonId = p.PersonId AND pd.IsActive = 1
                INNER JOIN o12_designatation d ON d.DesignationId = pd.O12_DesignationId AND d.IsActive = 1
                INNER JOIN o10_post post ON post.PostId = d.O10_Postid AND post.IsActive = 1
                WHERE post.PostId = 8 AND p.IsActive = 1
                UNION
                SELECT p.PersonId, p.PersonName
                FROM p02_Person p
                WHERE p.IsActive = 1 AND LOWER(LTRIM(RTRIM(p.PersonType))) IN ('party', 'supplier')
                ORDER BY PersonName;";
            using (var cmd = new SqlCommand(partyQuery, con))
            using (var reader = cmd.ExecuteReader())
                while (reader.Read())
                    parties.Add(new SelectListItem
                    {
                        Value = reader["PersonId"].ToString(),
                        Text = reader["PersonName"].ToString(),
                        Selected = Convert.ToInt32(reader["PersonId"]) == selectedPartyId
                    });

            const string driverQuery = @"
                SELECT DISTINCT p.PersonId, p.PersonName
                FROM p02_Person p
                INNER JOIN P11_PersonDesignatation pd ON pd.P02_PersonId = p.PersonId AND pd.IsActive = 1
                INNER JOIN o12_designatation d ON d.DesignationId = pd.O12_DesignationId AND d.IsActive = 1
                INNER JOIN o10_post post ON post.PostId = d.O10_Postid AND post.IsActive = 1
                WHERE post.PostId = 9 AND p.IsActive = 1
                UNION
                SELECT p.PersonId, p.PersonName
                FROM p02_Person p
                WHERE p.IsActive = 1 AND LOWER(LTRIM(RTRIM(p.PersonType))) = 'driver'
                ORDER BY PersonName;";
            using (var cmd = new SqlCommand(driverQuery, con))
            using (var reader = cmd.ExecuteReader())
                while (reader.Read())
                    drivers.Add(new SelectListItem
                    {
                        Value = reader["PersonId"].ToString(),
                        Text = reader["PersonName"].ToString(),
                        Selected = Convert.ToInt32(reader["PersonId"]) == selectedDriverId
                    });

            ViewBag.Parties = parties;
            ViewBag.Drivers = drivers;
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
            // Using ADO.NET directly here for simplicity since it's a specific read
            using (var con = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
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
