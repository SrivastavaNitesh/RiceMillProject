using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using RiceMillProject.BAL;
using RiceMillProject.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;

namespace RiceMillProject.Controllers
{
    [Authorize(Policy = "WeightmanAccess")]
    public class GateEntryController : Controller
    {
        private readonly GateEntryBAL _gateBal;
        private readonly VehicleBAL _vehicleBal;
        private readonly PersonBAL _personBal;
        private readonly OfficeBAL _officeBal;
        private readonly ItemBAL _itemBal;
        private readonly BusinessLayer _busLayer;

        public GateEntryController(IConfiguration configuration)
        {
            _gateBal = new GateEntryBAL(configuration);
            _vehicleBal = new VehicleBAL(configuration);
            _personBal = new PersonBAL(configuration);
            _officeBal = new OfficeBAL(configuration);
            _itemBal = new ItemBAL(configuration);
            _busLayer = new BusinessLayer(configuration);
        }


        // =========================================================
        // INDEX
        // =========================================================

        public IActionResult Index()
        {
            try
            {
                var entries =
                    _gateBal.GetAllGateEntries();

                return View(entries);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "An error occurred while loading entries: "
                    + ex.Message;

                return View(
                    new List<GateEntry>()
                );
            }
        }


        // =========================================================
        // DASHBOARD
        // =========================================================

        [HttpGet]
        public IActionResult Dashboard()
        {
            try
            {
                var entries =
                    _gateBal.GetAllGateEntries();

                ViewBag.TotalEntries =
                    entries.Count;

                ViewBag.TodayEntries =
                    entries.Count(
                        e =>
                            e.GateEntryTime.Date
                            ==
                            DateTime.Today
                    );

                ViewBag.PendingTare =
                    entries.Count(
                        e =>
                            !e.TareWeight.HasValue
                    );

                ViewBag.CompletedEntries =
                    entries.Count(
                        e =>
                            e.NetWeight.HasValue
                    );

                return View(entries);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage =
                    "An error occurred while loading Weightman dashboard: "
                    + ex.Message;

                ViewBag.TotalEntries = 0;
                ViewBag.TodayEntries = 0;
                ViewBag.PendingTare = 0;
                ViewBag.CompletedEntries = 0;

                return View(
                    new List<GateEntry>()
                );
            }
        }


        // =========================================================
        // CREATE - GET
        // =========================================================

        [HttpGet]
        public IActionResult Create(
            int? inwardId = null)
        {
            try
            {
                var entry =
                    new GateEntry();

                if (inwardId.HasValue)
                {
                    var details =
                        _gateBal.GetInwardDetailsById(
                            inwardId.Value
                        );

                    if (details.Rows.Count == 0)
                    {
                        TempData["ErrorMessage"] =
                            "Selected inward entry is unavailable or closed for new RST entries.";

                        return RedirectToAction(
                            "Report",
                            "Gateman"
                        );
                    }

                    var row =
                        details.Rows[0];

                    entry.InwardNo =
                        row["InwardNo"]
                            ?.ToString()
                        ?? string.Empty;

                    entry.PartyId =
                        row["PartyId"]
                            != DBNull.Value
                            ? Convert.ToInt32(
                                row["PartyId"]
                            )
                            : 0;

                    entry.VehicleId =
                        row["VehicleId"]
                            != DBNull.Value
                            ? Convert.ToInt32(
                                row["VehicleId"]
                            )
                            : 0;

                    entry.DriverId =
                        row["DriverId"]
                            != DBNull.Value
                            ? Convert.ToInt32(
                                row["DriverId"]
                            )
                            : 0;

                    ViewBag.LockInward =
                        true;

                    ViewBag.InwardId =
                        inwardId.Value;

                    ViewBag.PartyName =
                        row["PartyName"]
                            ?.ToString()
                        ?? "-";

                    ViewBag.VehicleNo =
                        row["VehicleNo"]
                            ?.ToString()
                        ?? "-";

                    ViewBag.DriverName =
                        row["DriverName"]
                            ?.ToString()
                        ?? "-";

                    ViewBag.DriverMobile =
                        row["DriverMobile"]
                            ?.ToString()
                        ?? "-";

                    ViewBag.TotalBags =
                        row["ApproxNoOfBags"]
                            != DBNull.Value
                            ? Convert.ToInt32(
                                row["ApproxNoOfBags"]
                            )
                            : 0;
                }

                PopulateDropdowns(entry);

                return View(entry);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "An error occurred while loading the page: "
                    + ex.Message;

                return RedirectToAction(
                    "Index"
                );
            }
        }


        // =========================================================
        // CREATE - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(
            GateEntry entry)
        {
            try
            {
                entry.CreatedBy =
                    Convert.ToInt32(
                        User.FindFirst(
                            ClaimTypes.NameIdentifier
                        )?.Value
                        ?? "0"
                    );

                if (
                    !string.IsNullOrWhiteSpace(
                        entry.InwardNo
                    )
                )
                {
                    var inward =
                        _gateBal.GetInwardDetails(
                            entry.InwardNo
                        );

                    if (inward.Rows.Count == 0)
                    {
                        throw new InvalidOperationException(
                            "The selected inward entry could not be verified."
                        );
                    }

                    var row =
                        inward.Rows[0];

                    entry.PartyId =
                        row["PartyId"]
                            != DBNull.Value
                            ? Convert.ToInt32(
                                row["PartyId"]
                            )
                            : 0;

                    entry.VehicleId =
                        row["VehicleId"]
                            != DBNull.Value
                            ? Convert.ToInt32(
                                row["VehicleId"]
                            )
                            : 0;

                    entry.DriverId =
                        row["DriverId"]
                            != DBNull.Value
                            ? Convert.ToInt32(
                                row["DriverId"]
                            )
                            : 0;
                }

                if (ModelState.IsValid)
                {
                    string generatedRST =
                        _gateBal.CreateGateEntry(
                            entry
                        );

                    if (
                        string.IsNullOrWhiteSpace(
                            generatedRST
                        )
                    )
                    {
                        throw new InvalidOperationException(
                            "RST could not be generated. Please reload and try again."
                        );
                    }

                    TempData["SuccessMessage"] =
                        $"RST Generated Successfully! RST Number: {generatedRST}";

                    return RedirectToAction(
                        "Dashboard"
                    );
                }

                PopulateDropdowns(entry);

                return View(entry);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Entry could not be saved: "
                    + ex.Message
                );

                PopulateDropdowns(entry);

                return View(entry);
            }
        }


        // =========================================================
        // POPULATE DROPDOWNS
        // =========================================================

        private void PopulateDropdowns(
            GateEntry entry)
        {
            try
            {
                DataTable dtInward =
                    _busLayer
                        .GetActiveInwardEntriesForDropdown();

                ViewBag.InwardEntries =
                    Allclass.CreateDropdown(
                        dtInward
                    );


                ViewBag.Vehicles =
                    new SelectList(
                        _vehicleBal
                            .GetAllVehicles(),
                        "VehicleId",
                        "VehicleNumber",
                        entry.VehicleId
                    );


                var parties =
                    _personBal
                        .GetActivePersonsByPost(
                            8
                        );

                ViewBag.Parties =
                    new SelectList(
                        parties,
                        "PersonId",
                        "PersonName",
                        entry.PartyId
                    );


                var driversList =
                    _gateBal
                        .GetDriversWithMobile();

                ViewBag.Drivers =
                    new SelectList(
                        driversList,
                        "DriverId",
                        "DriverNameMobile",
                        entry.DriverId
                    );


                DataTable dtOffice =
                    _busLayer
                        .GetAllMainoffice();

                ViewBag.Offices =
                    Allclass.CreateDropdown(
                        dtOffice
                    );


                ViewBag.Items =
                    new SelectList(
                        _itemBal
                            .GetAllItems()
                            .Where(
                                i => i.IsActive
                            ),
                        "ItemId",
                        "ItemName",
                        entry.ItemId
                    );
            }
            catch (Exception)
            {
                ViewBag.InwardEntries =
                    Enumerable
                        .Empty<SelectListItem>();

                ViewBag.Vehicles =
                    Enumerable
                        .Empty<SelectListItem>();

                ViewBag.Parties =
                    Enumerable
                        .Empty<SelectListItem>();

                ViewBag.Drivers =
                    Enumerable
                        .Empty<SelectListItem>();

                ViewBag.Offices =
                    Enumerable
                        .Empty<SelectListItem>();

                ViewBag.Items =
                    Enumerable
                        .Empty<SelectListItem>();
            }


            ViewBag.PendingInwards =
                new List<PendingRstInward>();

            ViewBag.Offices =
                Enumerable
                    .Empty<SelectListItem>();


            try
            {
                ViewBag.PendingInwards =
                    _gateBal
                        .GetPendingRstInwards();
            }
            catch (Exception)
            {
                ModelState.AddModelError(
                    "",
                    "Pending inwards could not be loaded. Check the database connection and GateEntry_Inward_Workflow.sql migration."
                );
            }


            try
            {
                ViewBag.Offices =
                    new SelectList(
                        _officeBal
                            .GetAllOffices()
                            .Where(
                                x => x.IsActive
                            ),
                        "OfficeId",
                        "OfficeName",
                        entry.TargetOfficeId
                    );
            }
            catch (Exception)
            {
                ModelState.AddModelError(
                    "",
                    "Company names could not be loaded."
                );
            }
        }


        // =========================================================
        // OUTBOUND - GET
        // =========================================================

        [HttpGet]
        public IActionResult Outbound(
            string id)
        {
            try
            {
                var entry =
                    _gateBal
                        .GetAllGateEntries()
                        .Find(
                            e =>
                                e.RSTNumber
                                ==
                                id
                        );

                if (entry == null)
                {
                    return NotFound();
                }

                return View(entry);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "An error occurred: "
                    + ex.Message;

                return RedirectToAction(
                    "Index"
                );
            }
        }


        // =========================================================
        // OUTBOUND - POST
        // =========================================================

        [HttpPost]
        public IActionResult Outbound(
            string rstNumber,
            decimal tareWeight)
        {
            try
            {
                _gateBal.CompleteGateExit(
                    rstNumber,
                    tareWeight
                );

                return RedirectToAction(
                    "Index"
                );
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "An error occurred: "
                    + ex.Message;

                return RedirectToAction(
                    "Index"
                );
            }
        }


        // =========================================================
        // PRINT
        // =========================================================

        [HttpGet]
        public IActionResult Print(
            string id)
        {
            try
            {
                var entry =
                    _gateBal
                        .GetAllGateEntries()
                        .Find(
                            e =>
                                e.RSTNumber
                                ==
                                id
                        );

                if (entry == null)
                {
                    return NotFound();
                }

                return View(entry);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "An error occurred: "
                    + ex.Message;

                return RedirectToAction(
                    "Index"
                );
            }
        }


        // =========================================================
        // GET INWARD DETAILS
        // =========================================================

        [HttpGet]
        public IActionResult GetInwardDetails(
            string inwardNo)
        {
            try
            {
                var dt =
                    _gateBal.GetInwardDetails(
                        inwardNo
                    );

                if (
                    dt != null
                    &&
                    dt.Rows.Count > 0
                )
                {
                    var row =
                        dt.Rows[0];

                    return Json(
                        new
                        {
                            success = true,

                            inwardNo =
                                row["InwardNo"]
                                    ?.ToString(),

                            partyId =
                                row["PartyId"]
                                    != DBNull.Value
                                    ? Convert.ToInt32(
                                        row["PartyId"]
                                    )
                                    : (int?)null,

                            partyName =
                                row["PartyName"]
                                    ?.ToString(),

                            vehicleId =
                                row["VehicleId"]
                                    != DBNull.Value
                                    ? Convert.ToInt32(
                                        row["VehicleId"]
                                    )
                                    : (int?)null,

                            vehicleNo =
                                row["VehicleNo"]
                                    ?.ToString(),

                            driverId =
                                row["DriverId"]
                                    != DBNull.Value
                                    ? Convert.ToInt32(
                                        row["DriverId"]
                                    )
                                    : (int?)null,

                            driverName =
                                row["DriverName"]
                                    ?.ToString(),

                            driverMobile =
                                row["DriverMobile"]
                                    ?.ToString()
                        }
                    );
                }

                return Json(
                    new
                    {
                        success = false,
                        message =
                            "Inward details not found"
                    }
                );
            }
            catch (Exception ex)
            {
                return Json(
                    new
                    {
                        success = false,
                        message =
                            ex.Message
                    }
                );
            }
        }


        // =========================================================
        // PARTY LINKAGE
        // =========================================================

        [HttpGet]
        public IActionResult GetLinkageByParty(
            int partyId)
        {
            try
            {
                var data =
                    _gateBal
                        .GetLinkageByParty(
                            partyId
                        );

                return Json(
                    new
                    {
                        success = true,
                        vehicleId =
                            data.vehicleId,
                        driverId =
                            data.driverId
                    }
                );
            }
            catch (Exception ex)
            {
                return Json(
                    new
                    {
                        success = false,
                        message =
                            ex.Message
                    }
                );
            }
        }


        // =========================================================
        // PARTY OPTIONS
        // =========================================================

        [HttpGet]
        public IActionResult GetPartyOptions(
            int partyId)
        {
            try
            {
                var vehicles =
                    _gateBal
                        .GetVehiclesByParty(
                            partyId
                        );

                var drivers =
                    _gateBal
                        .GetDriversByParty(
                            partyId
                        );


                int defaultVehicleId = 0;
                int defaultDriverId = 0;


                foreach (dynamic v in vehicles)
                {
                    if (v.IsLinked)
                    {
                        defaultVehicleId =
                            v.VehicleId;

                        break;
                    }
                }


                foreach (dynamic d in drivers)
                {
                    if (d.IsLinked)
                    {
                        defaultDriverId =
                            d.DriverId;

                        break;
                    }
                }


                return Json(
                    new
                    {
                        success = true,

                        vehicles =
                            vehicles,

                        drivers =
                            drivers,

                        defaultVehicleId =
                            defaultVehicleId,

                        defaultDriverId =
                            defaultDriverId
                    }
                );
            }
            catch (Exception ex)
            {
                return Json(
                    new
                    {
                        success = false,
                        message =
                            ex.Message
                    }
                );
            }
        }
    }
}