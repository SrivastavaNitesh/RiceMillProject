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

        public IActionResult Index()
        {
            try
            {
                PopulateDropdowns();
                var entries = _inwardBal.GetAllInwardEntries();
                ViewBag.NewInward = new InwardHeader
                {
                    InwardDate = DateTime.Now,
                    InwardTime = DateTime.Now.ToString("hh:mm tt"),
                    GateInDateTime = DateTime.Now,
                    GateManName = User?.Identity?.Name ?? "GateMan1 (System)"
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
                if (ModelState.IsValid)
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
                var parties = _personBal.GetAllPersons();

                ViewBag.Gates = new SelectList(gates, "GateId", "GateName");
                ViewBag.InwardTypes = new SelectList(inwardTypes, "InwardTypeId", "InwardTypeName");
                ViewBag.VehicleTypes = new SelectList(vehicleTypes, "VehicleTypeId", "VehicleTypeName");
                ViewBag.Parties = new SelectList(parties, "PersonId", "PersonName");
            }
            catch (Exception)
            {
                ViewBag.Gates = Enumerable.Empty<SelectListItem>();
                ViewBag.InwardTypes = Enumerable.Empty<SelectListItem>();
                ViewBag.VehicleTypes = Enumerable.Empty<SelectListItem>();
                ViewBag.Parties = Enumerable.Empty<SelectListItem>();
            }
        }
    }
}
