using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RiceMillProject.BAL;
using RiceMillProject.Models;

namespace RiceMillProject.Controllers
{
    [Authorize(Roles = "Admin, Gate Man")]
    public class OfficeController : Controller
    {
        private readonly OfficeBAL _officeBal;

        public OfficeController(IConfiguration configuration)
        {
            _officeBal = new OfficeBAL(configuration);
        }

        public IActionResult Index()
        {
            var offices = _officeBal.GetAllOffices();
            return View(offices);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new Office { IsActive = true });
        }

        [HttpPost]
        public IActionResult Create(Office office)
        {
            if (ModelState.IsValid)
            {
                _officeBal.AddOffice(office);
                return RedirectToAction("Index");
            }
            return View(office);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            // For now, getting all and finding. Ideally, we would have a GetOfficeById SP.
            var office = _officeBal.GetAllOffices().Find(o => o.OfficeId == id);
            if (office == null)
            {
                return NotFound();
            }
            return View(office);
        }

        [HttpPost]
        public IActionResult Edit(Office office)
        {
            if (ModelState.IsValid)
            {
                _officeBal.UpdateOffice(office);
                return RedirectToAction("Index");
            }
            return View(office);
        }
        
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var office = _officeBal.GetAllOffices().Find(o => o.OfficeId == id);
            if (office != null)
            {
                office.IsActive = false; // Soft delete
                _officeBal.UpdateOffice(office);
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult GetLocations(int officeId)
        {
            var locations = new List<object>();
            string _connStr = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build().GetConnectionString("DefaultConnection") ?? "";
            
            using (Microsoft.Data.SqlClient.SqlConnection con = new Microsoft.Data.SqlClient.SqlConnection(_connStr))
            {
                using (Microsoft.Data.SqlClient.SqlCommand cmd = new Microsoft.Data.SqlClient.SqlCommand("sp_GetOfficeLocations", con))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@OfficeId", officeId);
                    con.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            locations.Add(new { 
                                LocationId = reader["LocationId"], 
                                LocationName = reader["LocationName"] 
                            });
                        }
                    }
                }
            }
            return Json(locations);
        }
    }
}
