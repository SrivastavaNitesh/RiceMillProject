using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using RiceMillProject.BAL;
using RiceMillProject.Models;
using System.Data;
using System.Security.Claims;

namespace RiceMillProject.Controllers
{
    [Authorize]
    public class GatemanController : Controller
    {
        private readonly BusinessLayer _busLayer;
        private readonly GateInwardBAL _inwardBal;
        private readonly PersonBAL _personBal;

        public GatemanController(IConfiguration configuration)
        {
            _busLayer = new BusinessLayer(configuration);
            _inwardBal = new GateInwardBAL(configuration);
            _personBal = new PersonBAL(configuration);
        }

        [HttpGet]
        public IActionResult Dashboard()
        {
            try
            {
                int currentUserId = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var allEntries = _inwardBal.GetAllInwardEntries();
                
                DateTime today = DateTime.Today;
                int todayCount = allEntries.Count(e => e.InwardDate.Date == today || e.GateInDateTime.Date == today);
                int pendingRstCount = allEntries.Count(e => e.RSTRequired && !e.IsRSTGenerated);
                int withoutRstCount = allEntries.Count(e => !e.RSTRequired);
                int myEntriesCount = allEntries.Count;

                ViewBag.TodayCount = todayCount;
                ViewBag.PendingRstCount = pendingRstCount;
                ViewBag.WithoutRstCount = withoutRstCount;
                ViewBag.MyEntriesCount = myEntriesCount;

                var recentEntries = allEntries
                                    .OrderByDescending(e => e.InwardId)
                                    .Take(5)
                                    .ToList();

                return View(recentEntries);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "An error occurred while loading dashboard: " + ex.Message;
                ViewBag.TodayCount = 0;
                ViewBag.PendingRstCount = 0;
                ViewBag.WithoutRstCount = 0;
                ViewBag.MyEntriesCount = 0;
                return View(new List<InwardHeader>());
            }
        }

        [HttpGet]
        public IActionResult Report()
        {
            try
            {
                int currentUserId = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var allEntries = _inwardBal.GetAllInwardEntries();
                
                bool isAdmin = User.IsInRole("Admin") || (User.Identity?.Name ?? "").ToLower().Contains("admin") || currentUserId == 1;
                var myEntries = allEntries;

                return View(myEntries);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while loading report: " + ex.Message;
                return View(new List<InwardHeader>());
            }
        }

        public IActionResult Index()
        {
            try
            {
                PopulateDropdowns();
                var entries = _inwardBal.GetAllInwardEntries();
                int currentUserId = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                ViewBag.NewInward = new InwardHeader
                {
                    InwardDate = DateTime.Now,
                    InwardTime = DateTime.Now.ToString("hh:mm tt"),
                    GateInDateTime = DateTime.Now,
                    CreatedBy = currentUserId > 0 ? currentUserId : 2,
                    GateManName = User?.Identity?.Name ?? "Gateman"
                };
                return View(entries);
            }
            catch (Exception ex)
            {
                PopulateDropdowns(); // Ensure ViewBag is populated even if entries fail
                ViewBag.ErrorMessage = "An error occurred while loading the page: " + ex.Message;
                return View(new List<InwardHeader>());
            }
        }

        [HttpPost]
        public IActionResult CreateGateEntry(InwardHeader model)
        {
            try
            {
                int currentUserId = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                bool isAdmin = User.IsInRole("Admin") || (User.Identity?.Name ?? "").ToLower().Contains("admin") || currentUserId == 1;

                if (!isAdmin || model.CreatedBy <= 0)
                {
                    model.CreatedBy = currentUserId > 0 ? currentUserId : 2;
                }

                if (ModelState.IsValid || !string.IsNullOrWhiteSpace(model.VehicleNo))
                {
                    _inwardBal.SaveInwardEntry(model);
                    TempData["SuccessMessage"] = "Gate Inward Entry created successfully!";
                    return RedirectToAction(nameof(Index));
                }

                PopulateDropdowns();
                var entries = _inwardBal.GetAllInwardEntries();
                ViewBag.NewInward = model;
                return View("Index", entries);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while creating entry: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public IActionResult CheckDriverMobile(string mobileNumber)
        {
            try
            {
                bool isDuplicate = _inwardBal.IsDriverMobileDuplicate(mobileNumber);
                return Json(new { success = true, isDuplicate = isDuplicate });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetDriverMobileData(string driverName)
        {
            try
            {
                var driver = _personBal.GetAllPersons().FirstOrDefault(p => p.PersonName == driverName && p.PersonType == "Driver");
                if (driver != null)
                {
                    return Json(new { success = true, mobile = driver.MobileNumber });
                }
                return Json(new { success = false });
            }
            catch
            {
                return Json(new { success = false });
            }
        }

        [HttpPost]
        public IActionResult CreateInward(string vehicleNumber, string driverName, int? itemId, decimal? quantity, string? unit)
        {
            try
            {
                int gateManId = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var result = _busLayer.CreateGatemanInward(vehicleNumber, driverName, gateManId, itemId, quantity, unit);
                
                if (result != null && result.Rows.Count > 0)
                {
                    TempData["SuccessMessage"] = "Inward Entry created successfully. Generated Inward No: " + result.Rows[0][0]?.ToString();
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to create Inward Entry.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult GetChallanDetails(string challanNo)
        {
            try
            {
                var dt = _busLayer.GetChallanDetailsForOutward(challanNo);
                if (dt != null && dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    return Json(new
                    {
                        success = true,
                        challanNo = row["ChallanNo"].ToString(),
                        departmentName = row["DepartmentName"].ToString(),
                        generatorName = row["GeneratorName"].ToString(),
                        generatorPost = row["GeneratorPost"].ToString(),
                        itemName = row["ItemName"].ToString(),
                        quantity = row["Quantity"].ToString(),
                        unit = row["Unit"].ToString(),
                        statusName = row["StatusName"].ToString()
                    });
                }

                return Json(new { success = false, message = "Challan Not Found or Invalid." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult ApproveOutward(string challanNo)
        {
            try
            {
                int gateManId = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var result = _busLayer.ApproveOutwardChallan(challanNo, gateManId);

                if (result != null && result.Rows.Count > 0)
                {
                    TempData["SuccessMessage"] = "Outward Approved Successfully. Generated Outward No: " + result.Rows[0][0]?.ToString();
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to Approve Outward Entry.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        private void PopulateDropdowns()
        {
            try
            {
                int currentUserId = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                bool isAdmin = User.IsInRole("Admin") || (User.Identity?.Name ?? "").ToLower().Contains("admin") || currentUserId == 1;
                ViewBag.IsAdmin = isAdmin;

                var gatemen = _inwardBal.GetGatemenList();
                ViewBag.Gatemen = new SelectList(gatemen, "UserId", "GatemanName");

                var dtGates = _busLayer.ManageGateMaster("GETALL");
                var gates = new List<GateMaster>();
                if (dtGates != null && dtGates.Rows.Count > 0)
                {
                    foreach (DataRow row in dtGates.Rows)
                    {
                        gates.Add(new GateMaster
                        {
                            GateId = Convert.ToInt32(row["GateId"]),
                            GateName = row["GateName"].ToString() ?? ""
                        });
                    }
                }

                var inwardTypes = _inwardBal.GetInwardTypes();
                var vehicleTypes = _inwardBal.GetVehicleTypes();

                ViewBag.Gates = new SelectList(gates, "GateId", "GateName");
                ViewBag.InwardTypes = new SelectList(inwardTypes, "InwardTypeId", "InwardTypeName");
                ViewBag.VehicleTypes = new SelectList(vehicleTypes, "VehicleTypeId", "VehicleTypeName");
                
                var parties = _personBal.GetActivePersonsByPost(8);
                ViewBag.Parties = new SelectList(parties, "PersonId", "PersonName");

                // Fetch Drivers using INNER JOIN: p02 -> P11(IsActive=1) -> o12(IsActive=1) -> o10_post(PostId=9=Driver)
                // UNION fallback: PersonType = 'Driver' AND p02.IsActive=1
                var drivers = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();
                string constr = new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddJsonFile("appsettings.json").Build().GetConnectionString("DefaultConnection") ?? "";
                using(var con = new Microsoft.Data.SqlClient.SqlConnection(constr))
                {
                    string driverQuery = @"
                        SELECT DISTINCT p.PersonId, p.PersonName 
                        FROM p02_Person p 
                        INNER JOIN P11_PersonDesignatation pd ON p.PersonId = pd.P02_PersonId AND pd.IsActive = 1
                        INNER JOIN o12_designatation o12      ON pd.O12_DesignationId = o12.DesignationId AND o12.IsActive = 1
                        INNER JOIN o10_post op                ON o12.O10_Postid = op.PostId
                        WHERE op.PostId = 9 AND p.IsActive = 1
                        UNION
                        SELECT p.PersonId, p.PersonName
                        FROM p02_Person p
                        WHERE p.PersonType = 'Driver' AND p.IsActive = 1
                        ORDER BY PersonName";
                    
                    using(var cmd = new Microsoft.Data.SqlClient.SqlCommand(driverQuery, con))
                    {
                        con.Open();
                        using(var rdr = cmd.ExecuteReader())
                        {
                            while(rdr.Read())
                            {
                                drivers.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = rdr["PersonName"].ToString(), Text = rdr["PersonName"].ToString() });
                            }
                        }
                    }
                    
                    // Fetch Vehicles
                    var vehicles = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();
                    using(var cmd = new Microsoft.Data.SqlClient.SqlCommand("SELECT VehicleId, VehicleNumber FROM m_Vehicle WHERE IsActive=1", con))
                    {
                        using(var rdr = cmd.ExecuteReader())
                        {
                            while(rdr.Read())
                            {
                                vehicles.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = rdr["VehicleNumber"].ToString(), Text = rdr["VehicleNumber"].ToString() });
                            }
                        }
                    }
                    ViewBag.Vehicles = vehicles;
                }
                ViewBag.Drivers = drivers;

            }
            catch (Exception)
            {
                ViewBag.IsAdmin = true;
                ViewBag.Gatemen = Enumerable.Empty<SelectListItem>();
                ViewBag.Gates = Enumerable.Empty<SelectListItem>();
                ViewBag.InwardTypes = Enumerable.Empty<SelectListItem>();
                ViewBag.VehicleTypes = Enumerable.Empty<SelectListItem>();
                ViewBag.Companies = Enumerable.Empty<SelectListItem>();
                ViewBag.Drivers = Enumerable.Empty<SelectListItem>();
                ViewBag.Vehicles = Enumerable.Empty<SelectListItem>();
            }
        }
    }
}
