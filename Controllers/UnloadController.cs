using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using RiceMillProject.BAL;
using RiceMillProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Data;

namespace RiceMillProject.Controllers
{
    [Authorize(Policy = "SupervisorAccess")]
    public partial class UnloadController : Controller
    {
        private readonly RiceMillProject.DAL.RecordEditDAL _edits;
        private readonly UnloadTransactionBAL _unloadBal;
        private readonly GateEntryBAL _gateBal;
        private readonly PersonBAL _personBal;
        private readonly ItemBAL _itemBal;
        private readonly BagTypeBAL _bagBal;
        private readonly OfficeBAL _officeBal;
        private readonly LabWorkflowBAL _labWorkflow;
        private readonly LocationMasterBAL _locationBal;
        private readonly SupervisorDashboardBAL _supervisorDashboardBal;

        public UnloadController(IConfiguration configuration)
        {
            _edits = new RiceMillProject.DAL.RecordEditDAL(configuration);
            _unloadBal = new UnloadTransactionBAL(configuration);
            _gateBal = new GateEntryBAL(configuration);
            _personBal = new PersonBAL(configuration);
            _itemBal = new ItemBAL(configuration);
            _bagBal = new BagTypeBAL(configuration);
            _officeBal = new OfficeBAL(configuration);
            _labWorkflow = new LabWorkflowBAL(configuration);
            _locationBal = new LocationMasterBAL(configuration);
            _supervisorDashboardBal = new SupervisorDashboardBAL(configuration);
        }

        // ============================================================
        // INDEX
        // ============================================================

        [HttpGet]
        public IActionResult LocationReport(DateTime? fromDate, DateTime? toDate, int? locationId, string? status, string? search)
        {
            if (!ModelState.IsValid || (search?.Length ?? 0) > 200) return BadRequest("Invalid report filters.");
            var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time")).Date;
            var from = fromDate?.Date ?? new DateTime(today.Year, today.Month, 1);
            var to = toDate?.Date ?? new DateTime(today.Year, today.Month, 1).AddMonths(1).AddDays(-1);
            if (from > to || to == DateTime.MaxValue.Date) return BadRequest("Select a valid date range.");
            if (GetCurrentPersonId() <= 0 || GetCurrentOfficeId() <= 0) return Forbid();
            if (status is not (null or "" or "Pending" or "Completed")) return BadRequest("Invalid status.");
            var report = new RiceMillProject.DAL.SupervisorReportDAL(HttpContext.RequestServices.GetRequiredService<IConfiguration>())
                .Load(GetCurrentPersonId(), GetCurrentOfficeId(), from, to, locationId, status, search);
            if (locationId.HasValue && !report.Locations.AsEnumerable().Any(l => l.Field<int>("LocationId") == locationId.Value)) return BadRequest("Location is not available for this company.");
            return View(report);
        }

        public IActionResult Dashboard()
        {
            try
            {
                return View(_supervisorDashboardBal.GetDashboard(GetCurrentPersonId(), GetCurrentOfficeId()));
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Dashboard data could not be loaded: " + ex.Message;
                return View(new SupervisorDashboard());
            }
        }

        public IActionResult Index()
        {
            var gateEntries = _gateBal.GetAllGateEntries();
            var unloads = _unloadBal.GetAllUnloading();

            var assignedRsts = unloads
                .Select(u => u.RSTNumber)
                .Distinct()
                .ToList();

            var pendingAssignments = gateEntries
                .Where(e =>
                    (e.Status is "WaitingForSupervisor" or "Entered")
                    && !assignedRsts.Contains(e.RSTNumber))
                .ToList();

            var pendingVerifications = unloads
                .Where(u =>
                    u.Status == "Unloaded"
                    || u.Status == "Assigned")
                .ToList();

            ViewBag.PendingAssignments = pendingAssignments;
            ViewBag.PendingVerifications = pendingVerifications;
            return View(unloads);
        }

        // ============================================================
        // ASSIGN - GET
        // ============================================================

        [HttpGet]
        public IActionResult Assign(string rstNumber)
        {
            var gateEntry = _gateBal
                .GetAllGateEntries()
                .FirstOrDefault(e => e.RSTNumber == rstNumber);

            if (gateEntry == null)
            {
                return NotFound();
            }

            int supervisorId = GetCurrentPersonId();

            if (supervisorId <= 0)
            {
                return Forbid();
            }

            /*
             * IMPORTANT:
             * OfficeId claim se nahi.
             * Logged-in PersonId se DB se OfficeId niklega.
             */
            int officeId = GetCurrentOfficeId();

            ViewBag.RSTNumber = rstNumber;

            if (officeId <= 0)
            {
                PopulateAssignLookups(
                    0,
                    methId: 0,
                    locationId: null
                );

                ModelState.AddModelError(
                    string.Empty,
                    "Supervisor company mapping was not found."
                );

                return View(new UnloadTransaction
                {
                    RSTNumber = rstNumber,
                    SupervisorId = supervisorId,
                    ItemId = gateEntry.ItemId
                });
            }

            PopulateAssignLookups(
                officeId,
                methId: 0,
                locationId: null
            );

            var model = new UnloadTransaction
            {
                RSTNumber = rstNumber,
                SupervisorId = supervisorId,
                ItemId = gateEntry.ItemId
            };

            return View(model);
        }

        // ============================================================
        // ASSIGN - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Assign(UnloadTransaction unload)
        {
            int supervisorId = GetCurrentPersonId();
            int officeId = GetCurrentOfficeId();

            // Supervisor logged-in user se hi lenge
            unload.SupervisorId = supervisorId;

            // ============================
            // VALIDATION
            // ============================

            if (supervisorId <= 0)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Supervisor login was not found."
                );
            }

            if (officeId <= 0)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Supervisor company mapping was not found."
                );
            }

            if (string.IsNullOrWhiteSpace(unload.RSTNumber))
            {
                ModelState.AddModelError(
                    nameof(unload.RSTNumber),
                    "RST Number is required."
                );
            }

            if (unload.MethId <= 0)
            {
                ModelState.AddModelError(
                    nameof(unload.MethId),
                    "Please select Meth."
                );
            }

            var selectedLocationIds = (unload.SelectedLocationIds ?? Array.Empty<int>())
                .Where(x => x > 0).Distinct().ToArray();
            if (selectedLocationIds.Length == 0 && unload.LocationId.GetValueOrDefault() > 0)
                selectedLocationIds = new[] { unload.LocationId.GetValueOrDefault() };
            if (selectedLocationIds.Length == 0)
            {
                ModelState.AddModelError(
                    nameof(unload.SelectedLocationIds),
                    "Please select at least one unloading location."
                );
            }

            // IMPORTANT:
            // UnloadTransaction ke doosre fields ki validation ignore karni hai
            // kyunki Assign screen par unki zarurat nahi hai.

            if (supervisorId > 0 &&
                officeId > 0 &&
                !string.IsNullOrWhiteSpace(unload.RSTNumber) &&
                unload.MethId > 0 &&
                selectedLocationIds.Length > 0)
            {
                try
                {
                    int unloadId = _unloadBal.AssignUnloading(
                        unload.RSTNumber,
                        supervisorId,
                        unload.MethId,
                        unload.ItemId,
                        selectedLocationIds[0],
                        selectedLocationIds, officeId
                    );

                    if (unloadId > 0)
                    {
                        TempData["SuccessMessage"] =
                            $"RST {unload.RSTNumber} assigned successfully.";

                        return RedirectToAction(
                            nameof(PrintChallan),
                            new
                            {
                                id = unloadId
                            }
                        );
                    }

                    ModelState.AddModelError(
                        string.Empty,
                        "Unloading assignment could not be created."
                    );
                }
                catch (Microsoft.Data.SqlClient.SqlException ex)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        ex.Message
                    );
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        ex.Message
                    );
                }
            }

            // Error aaye to dropdown dobara load honge
            PopulateAssignLookups(
                officeId,
                unload.MethId,
                unload.LocationId
            );

            return View(unload);
        }
        // ============================================================
        // COMPLETE - GET
        // ============================================================

        [HttpGet]
        public IActionResult Complete(int id)
        {
            var unload = _unloadBal
                .GetAllUnloading()
                .FirstOrDefault(u => u.UnloadId == id);

            if (unload == null)
            {
                return NotFound();
            }

            int currentPersonId = GetCurrentPersonId();

            if (!User.IsAdminUser() &&
                unload.SupervisorId != currentPersonId)
            {
                return Forbid();
            }

            PopulateCompletionLookups(unload);

            return View(unload);
        }

        // ============================================================
        // COMPLETE - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Complete(
            UnloadTransaction unload,
            int[] SelectedWorkers,
            string Shift,
            int ActualLocationId)
        {
            var saved = _unloadBal
                .GetAllUnloading()
                .FirstOrDefault(u => u.UnloadId == unload.UnloadId);

            if (saved == null)
            {
                return NotFound();
            }

            int currentPersonId = GetCurrentPersonId();

            if (!User.IsAdminUser() &&
                saved.SupervisorId != currentPersonId)
            {
                return Forbid();
            }

            unload.RSTNumber = saved.RSTNumber;
            unload.SupervisorId = saved.SupervisorId;
            unload.MethId = saved.MethId;
            unload.ItemId = saved.ItemId;

            unload.SelectedItemIds =
                saved.ItemId.GetValueOrDefault() > 0
                    ? [saved.ItemId.GetValueOrDefault()]
                    : [];

            if (unload.BagTypeId.GetValueOrDefault() <= 0 ||
                unload.NumberOfBags.GetValueOrDefault() <= 0 ||
                ActualLocationId <= 0)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Select bag type, unloading location and positive bag count."
                );
            }

            if (unload.SelectedItemIds.Length == 0)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "This RST has no material/variety. Return it to Weightman for correction."
                );
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _unloadBal.CompleteWithItems(
                        unload,
                        ActualLocationId,
                        Shift,
                        SelectedWorkers ?? []
                    );

                    TempData["SuccessMessage"] =
                        $"Unloading report completed for RST {unload.RSTNumber}. It is now waiting for Tare Weight.";

                    return RedirectToAction(
                        nameof(PrintChallan),
                        new
                        {
                            id = unload.UnloadId
                        }
                    );
                }
                catch (Microsoft.Data.SqlClient.SqlException ex)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        ex.Number == 50001
                            ? ex.Message
                            : "Unloading report could not be saved."
                    );
                }
            }

            PopulateCompletionLookups(
                unload,
                ActualLocationId,
                SelectedWorkers,
                Shift
            );

            return View(unload);
        }

        // ============================================================
        // VERIFY - GET
        // ============================================================

        [HttpGet]
        public IActionResult Verify(int id)
        {
            var unload = _unloadBal
                .GetAllUnloading()
                .FirstOrDefault(u => u.UnloadId == id);

            if (unload == null)
            {
                return NotFound();
            }

            return View(unload);
        }

        // ============================================================
        // VERIFY - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Verify(
            int UnloadId,
            decimal TotalBagDeductionGrams)
        {
            _unloadBal.VerifyUnloading(
                UnloadId,
                TotalBagDeductionGrams
            );

            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // PRINT CHALLAN
        // ============================================================

        [HttpGet]
        public IActionResult PrintChallan(int id)
        {
            var unload = _unloadBal
                .GetAllUnloading()
                .FirstOrDefault(u => u.UnloadId == id);

            if (unload == null)
            {
                return NotFound();
            }

            return View(unload);
        }

        // ============================================================
        // ASSIGN LOOKUPS
        // ============================================================

        private void PopulateAssignLookups(
            int officeId,
            int methId = 0,
            int? locationId = null)
        {
            var allPersons = _personBal.GetAllPersons();

            /*
             * Meth list currently PersonType se aa rahi hai.
             * SQL SP bhi final validation karega ki selected Meth
             * Supervisor ki company ka hi ho.
             */
            ViewBag.Meths = new SelectList(
                allPersons.Where(p =>
                    p.PersonType == "Meth"),
                "PersonId",
                "PersonName",
                methId
            );

            if (officeId > 0)
            {
                ViewBag.Locations = new SelectList(
                    _locationBal.GetCompanyUnloadingLocations(officeId),
                    "LocationId",
                    "LocationName",
                    locationId
                );
            }
            else
            {
                ViewBag.Locations = new SelectList(
                    new List<LocationMaster>(),
                    "LocationId",
                    "LocationName"
                );
            }
        }

        // ============================================================
        // COMPLETE LOOKUPS
        // ============================================================

        private void PopulateCompletionLookups(
            UnloadTransaction unload,
            int selectedLocationId = 0,
            int[]? selectedWorkers = null,
            string? shift = null)
        {
            var people = _unloadBal.GetUnloadingPeople();

            ViewBag.GateMen = new SelectList(
                people.Where(p =>
                    p.PersonType == "Gate Man"),
                "PersonId",
                "PersonName",
                unload.GateManId
            );

            ViewBag.Workers = people
                .Where(p =>
                    p.PersonType == "Worker")
                .ToList();

            ViewBag.BagTypes = new SelectList(
                _bagBal.GetAllBagTypes(),
                "BagTypeId",
                "BagTypeName",
                unload.BagTypeId
            );

            int officeId = GetCurrentOfficeId();

            if (officeId > 0)
            {
                ViewBag.Locations = new SelectList(
                    _officeBal.GetOfficeLocations(officeId),
                    "LocationId",
                    "LocationName",
                    selectedLocationId
                );
            }
            else
            {
                ViewBag.Locations = new SelectList(
                    new List<OfficeLocation>(),
                    "LocationId",
                    "LocationName"
                );
            }

            ViewBag.SelectedWorkers =
                selectedWorkers ?? [];

            ViewBag.SelectedShift =
                shift ?? "Day";
        }

        // ============================================================
        // CURRENT LOGIN PERSON
        // ============================================================

        private int GetCurrentPersonId()
        {
            if (User.IsAdminUser()) return HttpContext.EffectivePersonId();
            var personClaim = User.Claims
                .FirstOrDefault(c =>
                    c.Type == "PersonId")
                ?.Value;

            if (!string.IsNullOrWhiteSpace(personClaim) &&
                int.TryParse(
                    personClaim,
                    out int personId))
            {
                return personId;
            }

            return 0;
        }

        // ============================================================
        // CURRENT LOGIN OFFICE / COMPANY
        // ============================================================

        private int GetCurrentOfficeId()
        {
            /*
             * OfficeId CLAIM SE NAHI LENA.
             *
             * Current logged-in PersonId se
             * sa05_user table se OfficeId niklega.
             */

            int personId = GetCurrentPersonId();

            if (personId > 0)
            {
                var mappedOffice = _officeBal.GetOfficeIdByPersonId(personId);
                if (mappedOffice > 0) return mappedOffice;
            }

            // Login creates CompanyId from sa05_user.OfficeId; use it as a safe
            // fallback when the person-to-company mapping is not populated yet.
            var companyClaim = User.Claims.FirstOrDefault(c => c.Type == "CompanyId")?.Value;
            if (int.TryParse(companyClaim, out var companyId) && companyId > 0) return companyId;
            var username = User.Identity?.Name;
            return string.IsNullOrWhiteSpace(username) ? 0 : _officeBal.GetOfficeIdByUsername(username);
        }

        [HttpGet]
        public IActionResult SupervisorAction(int id, bool edit = false, bool viewOnly = false)
        {
            try
            {
                int supervisorId = GetCurrentPersonId();

                if (supervisorId <= 0)
                {
                    return Forbid();
                }

                var unload = _unloadBal
                    .GetAllUnloading()
                    .FirstOrDefault(x => x.UnloadId == id);

                if (unload == null)
                {
                    return NotFound();
                }

                if (!User.IsAdminUser() && unload.SupervisorId != supervisorId) return Forbid();

                var model = new SupervisorUnloadActionViewModel
                {
                    UnloadId = unload.UnloadId,

                    RSTNumber = unload.RSTNumber ?? "",

                    AssignedDateTime = unload.UnloadTime,

                    VehicleNumber = unload.VehicleNumber ?? "",

                    PartyName = unload.PartyName ?? "",

                    CompanyName = unload.OfficeName ?? "",

                    ItemName = unload.ItemName ?? "",

                    LocationName = unload.LocationName ?? "",

                    AssignedLocationIds = _unloadBal.GetAssignedLocationIds(unload.UnloadId),

                    MethName = unload.MethName ?? "",

                    MethTotalBags = unload.NumberOfBags ?? 0,
                    IsWorkerWorkCompleted = unload.IsWorkerWorkCompleted,
                    IsFieldDetailsCompleted = unload.IsSupervisorActionCompleted
                };


                // =====================================================
                // ITEM CATEGORY - DATABASE
                // =====================================================

                var categoryTable =
                    _itemBal.GetActiveItemCategories();

                ViewBag.ItemCategories =
                    categoryTable.AsEnumerable().Select(row => new SelectListItem
                    { Value = Convert.ToString(row["CategoryId"]), Text = Convert.ToString(row["CategoryName"]) }).ToList();


                // =====================================================
                // BAG TYPE - DATABASE
                // =====================================================

                var bagTypeTable =
                    _bagBal.GetActiveBagTypes();

                ViewBag.BagTypes = bagTypeTable.AsEnumerable()
                    .Select(row => new SelectListItem
                    {
                        Value = row["BagTypeId"]?.ToString() ?? "",
                        Text = row["BagTypeName"]?.ToString() ?? ""
                    })
                    .Where(x => !string.IsNullOrWhiteSpace(x.Value))
                    .ToList();

                var assignedLocationIds = model.AssignedLocationIds.ToHashSet();
                ViewBag.CompanyLocations = _locationBal.GetCompanyUnloadingLocations(GetCurrentOfficeId())
                    .Where(x => assignedLocationIds.Contains(x.LocationId)).ToList();


                if (edit || viewOnly) {
                    var correction = _edits.Load(ActorId, "Supervisor", id.ToString());
                    if (!viewOnly && !correction.Access.CanEdit) return StatusCode(403, correction.Access.LockedReason);
                    PrepareFieldCorrection(model, correction);
                    ViewBag.ReadOnly=viewOnly;
                }
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "Unable to load Supervisor action page: "
                    + ex.Message;

                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SupervisorAction(
    SupervisorUnloadActionViewModel model)
        {
            if (_edits.Exists("Supervisor", model.UnloadId.ToString())) return SaveFieldCorrection(model);
            try
            {
                int supervisorId =
                    GetCurrentPersonId();

                if (supervisorId <= 0)
                {
                    return Forbid();
                }


                // =====================================================
                // UNLOAD CHECK
                // =====================================================

                var unload = _unloadBal
                    .GetAllUnloading()
                    .FirstOrDefault(
                        x => x.UnloadId == model.UnloadId
                    );

                if (unload == null)
                {
                    TempData["ErrorMessage"] =
                        "Unloading record was not found.";

                    return RedirectToAction("Index");
                }


                // =====================================================
                // ONLY ASSIGNED SUPERVISOR CAN SAVE
                // =====================================================

                if (unload.SupervisorId != supervisorId)
                {
                    return Forbid();
                }


                // =====================================================
                // CLEAN CATEGORY ROWS
                // =====================================================

                var rows =
                    model.CategoryRows?
                        .Where(x =>
                            x.CategoryId > 0
                            &&
                            x.ItemId > 0
                            &&
                            x.LocationId > 0
                            &&
                            x.BagTypeId > 0
                            &&
                            x.BagCount > 0
                        )
                        .ToList()
                    ??
                    new List<SupervisorUnloadCategoryRow>();


                if (rows.Count == 0)
                {
                    TempData["ErrorMessage"] =
                        "Please enter at least one category/item bag detail.";

                    return RedirectToAction(
                        nameof(SupervisorAction),
                        new
                        {
                            id = model.UnloadId
                        }
                    );
                }


                // =====================================================
                // DUPLICATE COMBINATION CHECK
                // =====================================================

                bool duplicateExists =
                    rows
                        .GroupBy(x => new
                        {
                            x.CategoryId,
                            x.ItemId,
                            x.LocationId,
                            x.BagTypeId
                        })
                        .Any(g => g.Count() > 1);


                if (duplicateExists)
                {
                    TempData["ErrorMessage"] =
                        "Same Category, Item, Location and Bag Type cannot be entered twice.";

                    return RedirectToAction(
                        nameof(SupervisorAction),
                        new
                        {
                            id = model.UnloadId
                        }
                    );
                }


                // =====================================================
                // SUPERVISOR TOTAL
                // =====================================================

                var locationRows = (model.LocationRows ?? new List<SupervisorUnloadLocationRow>())
                    .SelectMany(x => (x.BagRows ?? new List<SupervisorUnloadLocationBagRow>())
                        .Select(b => new SupervisorUnloadLocationBagRow { LocationId = x.LocationId, BagTypeId = b.BagTypeId, BagCount = b.BagCount }))
                    .Where(x => x.LocationId > 0 && x.BagTypeId > 0 && x.BagCount > 0)
                    .ToList();
                if (locationRows.Count == 0)
                {
                    TempData["ErrorMessage"] = "Please enter at least one positive location bag detail.";
                    return RedirectToAction(nameof(SupervisorAction), new { id = model.UnloadId });
                }

                int legacyStackTotal = model.StackPP + model.StackJute + model.HaudiPP + model.HaudiJute;
                int locationTotal = locationRows.Sum(x => x.BagCount) + legacyStackTotal;
                int materialTotal = rows.Sum(x => x.BagCount);
                if (locationTotal != materialTotal)
                {
                    TempData["ErrorMessage"] = $"Location Total ({locationTotal}) must equal Material Total ({materialTotal}).";
                    return RedirectToAction(nameof(SupervisorAction), new { id = model.UnloadId });
                }


                // =====================================================
                // SAVE
                // =====================================================

                int supervisorActionId =
                    _unloadBal.SaveSupervisorUnloadAction(
                        model.UnloadId,
                        supervisorId,
                        model.StackPP,
                        model.StackJute,
                        model.HaudiPP,
                        model.HaudiJute,
                        rows,
                        locationRows
                    );


                if (supervisorActionId <= 0)
                {
                    throw new InvalidOperationException(
                        "Supervisor unloading details could not be saved."
                    );
                }


                // =====================================================
                // SUCCESS
                // =====================================================

                TempData["SuccessMessage"] = $"RST {unload.RSTNumber} Supervisor field details saved successfully.";

                return RedirectToAction("Index");
            }
            catch (SqlException ex)
            {
                TempData["ErrorMessage"] =
                    ex.Message;

                return RedirectToAction(
                    nameof(SupervisorAction),
                    new
                    {
                        id = model.UnloadId
                    }
                );
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "Unable to save Supervisor details: "
                    + ex.Message;

                return RedirectToAction(
                    nameof(SupervisorAction),
                    new
                    {
                        id = model.UnloadId
                    }
                );
            }
        }

        [HttpGet]
        public IActionResult MethWorkRegister()
        {
            try
            {
                int supervisorId =
                    GetCurrentPersonId();

                if (supervisorId <= 0)
                {
                    return Forbid();
                }

                var records =
                    _unloadBal
                        .GetSupervisorMethWorkRegister(
                            supervisorId
                        );

                ViewBag.FinalWeightTimes=new RiceMillProject.DAL.SupervisorReportDAL(HttpContext.RequestServices.GetRequiredService<IConfiguration>()).FinalWeightTimes(supervisorId,GetCurrentOfficeId());
                ViewBag.ItemCategoriesByUnload=new RiceMillProject.DAL.SupervisorReportDAL(HttpContext.RequestServices.GetRequiredService<IConfiguration>()).CategoryNames(supervisorId,GetCurrentOfficeId());
                return View(records.OrderByDescending(r => r.UnloadTime ?? DateTime.MinValue).ThenByDescending(r => r.UnloadId).ToList());
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "Unable to load Labour Mates register: "
                    + ex.Message;

                return View(
                    new List<UnloadTransaction>()
                );
            }
        }


        [HttpGet]
        public IActionResult GetItemsByCategory(int categoryId)
        {
            try
            {
                if (categoryId <= 0)
                {
                    return Json(new
                    {
                        success = false,
                        items = Array.Empty<object>()
                    });
                }

                var itemTable =
                    _itemBal.GetItemsByCategory(categoryId);

                var items = itemTable
                    .AsEnumerable()
                    .Select(row => new
                    {
                        itemId =
                            row.Field<int>("ItemId"),

                        itemName =
                            row.Field<string>("ItemName") ?? ""
                    })
                    .ToList();

                return Json(new
                {
                    success = true,
                    items = items
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message,
                    items = Array.Empty<object>()
                });
            }
        }
        [HttpGet]
        public IActionResult SupervisorMethWork(int id, bool edit = false, bool viewOnly = false)
        {
            try
            {
                int supervisorId = GetCurrentPersonId();

                if (supervisorId <= 0)
                {
                    return Forbid();
                }

                // =====================================================
                // UNLOAD / RST FIND
                // =====================================================

                var unload = _unloadBal
                    .GetAllUnloading()
                    .FirstOrDefault(x => x.UnloadId == id);

                if (unload == null)
                {
                    TempData["ErrorMessage"] =
                        "Unloading record was not found.";

                    return RedirectToAction(
                        nameof(MethWorkRegister)
                    );
                }


                // =====================================================
                // ONLY ASSIGNED SUPERVISOR CAN ENTER WORK
                // =====================================================

                if (unload.SupervisorId != supervisorId)
                {
                    return Forbid();
                }


                // =====================================================
                // METH MUST BE ASSIGNED
                // =====================================================

                if (unload.MethId <= 0)
                {
                    TempData["ErrorMessage"] =
                        "Meth is not assigned for this RST.";

                    return RedirectToAction(
                        nameof(MethWorkRegister)
                    );
                }


                // =====================================================
                // LOCATION MUST BE ASSIGNED
                // =====================================================

                if (unload.LocationId <= 0)
                {
                    TempData["ErrorMessage"] =
                        "Unloading location is not assigned for this RST.";

                    return RedirectToAction(
                        nameof(MethWorkRegister)
                    );
                }


                // =====================================================
                // WORK ALREADY ENTERED
                // =====================================================

                if (!edit && !viewOnly && string.Equals(
                        unload.Status,
                        "Unloaded",
                        StringComparison.OrdinalIgnoreCase))
                {
                    TempData["ErrorMessage"] =
                        "Worker-wise unloading work has already been entered.";

                    return RedirectToAction(
                        nameof(MethWorkRegister)
                    );
                }


                // =====================================================
                // GET WORKERS UNDER ASSIGNED METH
                // =====================================================

                var workers =
                    _unloadBal
                        .GetWorkersByMeth(
                            unload.MethId
                        );


                unload.WorkerRows =
                    workers
                        .Select(w =>
                            new WorkerAllocation
                            {
                                UnloadId = unload.UnloadId,

                                WorkerId = w.PersonId,

                                WorkerName =
                                    w.PersonName ?? ""
                            }
                        )
                        .ToList();


                // =====================================================
                // BAG TYPES
                // Existing project method use kar rahe hain
                // =====================================================

                ViewBag.BagTypes =
                    new SelectList(
                        _bagBal.GetAllBagTypes(),
                        "BagTypeId",
                        "BagTypeName"
                    );


                if (edit || viewOnly) {
                    var correction = _edits.Load(ActorId, "Labour", id.ToString());
                    if (!viewOnly && !correction.Access.CanEdit) return StatusCode(403, correction.Access.LockedReason);
                    unload.EditVersion=correction.Version; unload.EditExpiresAt=correction.Access.ExpiresAt;
                    foreach(var saved in correction.Details) { var workerId=N(saved,"WorkerId"); if(!unload.WorkerRows.Any(w=>w.WorkerId==workerId)) unload.WorkerRows.Add(new WorkerAllocation{WorkerId=workerId,WorkerName="Worker "+workerId}); }
                    ViewBag.SavedWorkerDetails=correction.Details;
                    ViewBag.ReadOnly=viewOnly;
                    if(viewOnly){var ids=correction.Details.Select(d=>N(d,"WorkerId")).ToHashSet();unload.WorkerRows=unload.WorkerRows.Where(w=>ids.Contains(w.WorkerId)).ToList();}
                }
                return View(unload);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "Unable to load labour work entry page: "
                    + ex.Message;

                return RedirectToAction(
                    nameof(MethWorkRegister)
                );
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SupervisorMethWork(
    UnloadTransaction model)
        {
            if (_edits.Exists("Labour", model.UnloadId.ToString())) return SaveWorkerCorrection(model);
            try
            {
                int supervisorId =
                    GetCurrentPersonId();

                if (supervisorId <= 0)
                {
                    return Forbid();
                }


                // =====================================================
                // UNLOAD VERIFY
                // =====================================================

                var unload =
                    _unloadBal
                        .GetAllUnloading()
                        .FirstOrDefault(
                            x =>
                                x.UnloadId
                                ==
                                model.UnloadId
                        );


                if (unload == null)
                {
                    TempData["ErrorMessage"] =
                        "Unloading record was not found.";

                    return RedirectToAction(
                        nameof(MethWorkRegister)
                    );
                }


                // =====================================================
                // ASSIGNED SUPERVISOR VERIFY
                // =====================================================

                if (unload.SupervisorId != supervisorId)
                {
                    return Forbid();
                }


                // =====================================================
                // ALREADY COMPLETED
                // =====================================================

                if (
                    string.Equals(
                        unload.Status,
                        "Unloaded",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    TempData["ErrorMessage"] =
                        "Labour work details have already been entered.";

                    return RedirectToAction(
                        nameof(MethWorkRegister)
                    );
                }


                // =====================================================
                // METH VERIFY
                // =====================================================

                if (unload.MethId <= 0)
                {
                    TempData["ErrorMessage"] =
                        "Meth is not assigned for this RST.";

                    return RedirectToAction(
                        nameof(MethWorkRegister)
                    );
                }


                // =====================================================
                // GET VALID WORKERS UNDER ASSIGNED METH
                // =====================================================

                var methWorkers =
                    _unloadBal
                        .GetWorkersByMeth(
                            unload.MethId
                        );


                var validWorkerIds =
                    methWorkers
                        .Select(
                            x => x.PersonId
                        )
                        .ToHashSet();


                // =====================================================
                // GET POSTED WORK ROWS
                // Ignore completely blank rows
                // =====================================================

                var rows =
                    model.WorkerRows?
                        .Where(
                            x =>
                                x.WorkerId > 0
                                &&
                                (
                                    !string.IsNullOrWhiteSpace(
                                        x.WorkType
                                    )
                                    ||
                                    x.BagTypeId.GetValueOrDefault() > 0
                                    ||
                                    x.BagCount > 0
                                    ||
                                    x.PerBagCharge > 0
                                )
                        )
                        .ToList()
                    ??
                    new List<WorkerAllocation>();


                if (rows.Count == 0)
                {
                    TempData["ErrorMessage"] =
                        "Please enter work details for at least one worker.";

                    return RedirectToAction(
                        nameof(SupervisorMethWork),
                        new
                        {
                            id = model.UnloadId
                        }
                    );
                }


                // =====================================================
                // SERVER SIDE VALIDATION
                // =====================================================

                foreach (var row in rows)
                {
                    if (
                        !validWorkerIds.Contains(
                            row.WorkerId
                        )
                    )
                    {
                        TempData["ErrorMessage"] =
                            "Invalid worker selected.";

                        return RedirectToAction(
                            nameof(SupervisorMethWork),
                            new
                            {
                                id = model.UnloadId
                            }
                        );
                    }


                    // Support old + new naming
                    string workType =
                        row.WorkType?
                            .Trim()
                        ?? "";


                    if (
                        workType.Equals(
                            "Load",
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        workType =
                            "Loading";
                    }
                    else if (
                        workType.Equals(
                            "Unload",
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        workType =
                            "Unloading";
                    }


                    if (
                        workType != "Loading"
                        &&
                        workType != "Unloading"
                    )
                    {
                        TempData["ErrorMessage"] =
                            $"Please select Loading or Unloading for {row.WorkerName}.";

                        return RedirectToAction(
                            nameof(SupervisorMethWork),
                            new
                            {
                                id = model.UnloadId
                            }
                        );
                    }


                    row.WorkType =
                        workType;


                    if (
                        row.BagTypeId.GetValueOrDefault()
                        <= 0
                    )
                    {
                        TempData["ErrorMessage"] =
                            $"Please select Bag Type for {row.WorkerName}.";

                        return RedirectToAction(
                            nameof(SupervisorMethWork),
                            new
                            {
                                id = model.UnloadId
                            }
                        );
                    }


                    if (row.BagCount <= 0)
                    {
                        TempData["ErrorMessage"] =
                            $"Bag Count must be greater than zero for {row.WorkerName}.";

                        return RedirectToAction(
                            nameof(SupervisorMethWork),
                            new
                            {
                                id = model.UnloadId
                            }
                        );
                    }


                    if (row.PerBagCharge < 0)
                    {
                        TempData["ErrorMessage"] =
                            $"Invalid Per Bag Charge for {row.WorkerName}.";

                        return RedirectToAction(
                            nameof(SupervisorMethWork),
                            new
                            {
                                id = model.UnloadId
                            }
                        );
                    }
                }

                // When Supervisor Field Details already contain the bag total,
                // worker allocations must reconcile to that same total.
                var workerBagTotal = rows.Sum(x => x.BagCount);
                var supervisorBagTotal = unload.NumberOfBags.GetValueOrDefault();
                if (supervisorBagTotal > 0 && workerBagTotal != supervisorBagTotal)
                {
                    TempData["ErrorMessage"] = $"Worker bag total ({workerBagTotal}) must match Supervisor bag total ({supervisorBagTotal}) for RST {unload.RSTNumber}.";
                    return RedirectToAction(nameof(SupervisorMethWork), new { id = model.UnloadId });
                }


                // =====================================================
                // DUPLICATE ROW CHECK
                // Same Worker + Work + Bag Type
                // =====================================================

                bool duplicateExists =
                    rows
                        .GroupBy(
                            x => new
                            {
                                x.WorkerId,

                                WorkType =
                                    x.WorkType
                                        .ToLower(),

                                BagTypeId =
                                    x.BagTypeId
                                        .GetValueOrDefault()
                            }
                        )
                        .Any(
                            g => g.Count() > 1
                        );


                if (duplicateExists)
                {
                    TempData["ErrorMessage"] =
                        "Same Worker, Work Type and Bag Type cannot be entered twice.";

                    return RedirectToAction(
                        nameof(SupervisorMethWork),
                        new
                        {
                            id = model.UnloadId
                        }
                    );
                }


                _unloadBal.CompleteSupervisorWorkerWork(
                    model.UnloadId,
                    supervisorId,
                    rows
                );


                // =====================================================
                // SUCCESS
                // =====================================================

                TempData["SuccessMessage"] =
                    $"Labour work details saved successfully for RST {unload.RSTNumber}.";


                return RedirectToAction(
                    nameof(MethWorkRegister)
                );
            }
            catch (SqlException ex)
            {
                TempData["ErrorMessage"] =
                    ex.Message;

                return RedirectToAction(
                    nameof(SupervisorMethWork),
                    new
                    {
                        id = model.UnloadId
                    }
                );
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "Unable to save labour work details: "
                    + ex.Message;

                return RedirectToAction(
                    nameof(SupervisorMethWork),
                    new
                    {
                        id = model.UnloadId
                    }
                );
            }
        }
        [HttpGet]
        public IActionResult GetItemCategories()
        {
            try
            {
                var dt = _itemBal.GetActiveItemCategories();

                var categories = dt
                    .AsEnumerable()
                    .Select(row => new
                    {
                        categoryId = Convert.ToInt32(
                            row["CategoryId"]
                        ),

                        categoryName =
                            row["CategoryName"]?.ToString()
                            ?? ""
                    })
                    .ToList();

                return Json(new
                {
                    success = true,
                    categories = categories
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message,
                    categories = Array.Empty<object>()
                });
            }
        }


        [HttpGet]
        public IActionResult GetBagTypes()
        {
            try
            {
                var data =
                    _bagBal.GetAllBagTypes();

                var bagTypes =
                    data.Select(x => new
                    {
                        bagTypeId = x.BagTypeId,
                        bagTypeName = x.BagTypeName
                    })
                    .ToList();

                return Json(new
                {
                    success = true,
                    bagTypes = bagTypes
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message,
                    bagTypes = Array.Empty<object>()
                });
            }
        }
    }
}
