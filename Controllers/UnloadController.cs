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
    public class UnloadController : Controller
    {
        private readonly UnloadTransactionBAL _unloadBal;
        private readonly GateEntryBAL _gateBal;
        private readonly PersonBAL _personBal;
        private readonly ItemBAL _itemBal;
        private readonly BagTypeBAL _bagBal;
        private readonly OfficeBAL _officeBal;
        private readonly LabWorkflowBAL _labWorkflow;

        public UnloadController(IConfiguration configuration)
        {
            _unloadBal = new UnloadTransactionBAL(configuration);
            _gateBal = new GateEntryBAL(configuration);
            _personBal = new PersonBAL(configuration);
            _itemBal = new ItemBAL(configuration);
            _bagBal = new BagTypeBAL(configuration);
            _officeBal = new OfficeBAL(configuration);
            _labWorkflow = new LabWorkflowBAL(configuration);
        }

        // ============================================================
        // INDEX
        // ============================================================

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

            if (!unload.LocationId.HasValue ||
                unload.LocationId.Value <= 0)
            {
                ModelState.AddModelError(
                    nameof(unload.LocationId),
                    "Please select unloading location."
                );
            }

            // IMPORTANT:
            // UnloadTransaction ke doosre fields ki validation ignore karni hai
            // kyunki Assign screen par unki zarurat nahi hai.

            if (supervisorId > 0 &&
                officeId > 0 &&
                !string.IsNullOrWhiteSpace(unload.RSTNumber) &&
                unload.MethId > 0 &&
                unload.LocationId.HasValue &&
                unload.LocationId.Value > 0)
            {
                try
                {
                    int unloadId = _unloadBal.AssignUnloading(
                        unload.RSTNumber,
                        supervisorId,
                        unload.MethId,
                        unload.ItemId,
                        unload.LocationId.Value
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
                    _officeBal.GetOfficeLocations(officeId),
                    "LocationId",
                    "LocationName",
                    locationId
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

            if (personId <= 0)
            {
                return 0;
            }

            return _officeBal.GetOfficeIdByPersonId(personId);
        }

        [HttpGet]
        public IActionResult SupervisorAction(int id)
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

                // Meth ka work complete hone ke baad hi
                // Supervisor action lega
                if (unload.Status != "Unloaded")
                {
                    TempData["ErrorMessage"] =
                        "This RST is not ready for Supervisor action.";

                    return RedirectToAction("Index");
                }

                var model = new SupervisorUnloadActionViewModel
                {
                    UnloadId = unload.UnloadId,

                    RSTNumber = unload.RSTNumber ?? "",

                    VehicleNumber = unload.VehicleNumber ?? "",

                    PartyName = unload.PartyName ?? "",

                    CompanyName = unload.OfficeName ?? "",

                    ItemName = unload.ItemName ?? "",

                    LocationName = unload.LocationName ?? "",

                    MethName = unload.MethName ?? "",

                    MethTotalBags = unload.NumberOfBags ?? 0
                };


                // =====================================================
                // ITEM CATEGORY - DATABASE
                // =====================================================

                var categoryTable =
                    _itemBal.GetActiveItemCategories();

                ViewBag.ItemCategories =
                    new SelectList(
                        categoryTable.DefaultView,
                        "CategoryId",
                        "CategoryName"
                    );


                // =====================================================
                // BAG TYPE - DATABASE
                // =====================================================

                var bagTypeTable =
                    _bagBal.GetActiveBagTypes();

                ViewBag.BagTypes =
                    new SelectList(
                        bagTypeTable.DefaultView,
                        "BagTypeId",
                        "BagTypeName"
                    );


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
                // METH WORK MUST BE COMPLETE
                // =====================================================

                if (unload.Status != "Unloaded")
                {
                    TempData["ErrorMessage"] =
                        "Meth work is not completed for this RST.";

                    return RedirectToAction("Index");
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
                            x.BagTypeId
                        })
                        .Any(g => g.Count() > 1);


                if (duplicateExists)
                {
                    TempData["ErrorMessage"] =
                        "Same Category, Item and Bag Type cannot be entered twice.";

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

                int supervisorTotal =
                    rows.Sum(x => x.BagCount);


                // Hidden MethTotalBags par trust nahi kar rahe
                // DB ki value use hogi
                int methTotal =
                    unload.NumberOfBags ?? 0;


                if (methTotal <= 0)
                {
                    TempData["ErrorMessage"] =
                        "Meth bag total is not available.";

                    return RedirectToAction(
                        nameof(SupervisorAction),
                        new
                        {
                            id = model.UnloadId
                        }
                    );
                }


                if (supervisorTotal != methTotal)
                {
                    TempData["ErrorMessage"] =
                        $"Supervisor total ({supervisorTotal}) must match Meth total ({methTotal}).";

                    return RedirectToAction(
                        nameof(SupervisorAction),
                        new
                        {
                            id = model.UnloadId
                        }
                    );
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
                        rows
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

                TempData["SuccessMessage"] =
                    $"RST {unload.RSTNumber} Supervisor details saved successfully.";

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

                return View(records);
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
    }
}