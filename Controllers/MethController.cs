using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RiceMillProject.BAL;
using RiceMillProject.Models;
using System.Linq;
using System.Security.Claims;

namespace RiceMillProject.Controllers
{
    [Authorize(Policy = "MethAccess")]
    public class MethController : Controller
    {
        private readonly UnloadTransactionBAL _unloadBal;
        private readonly BagTypeBAL _bagBal;

        public MethController(
            IConfiguration configuration)
        {
            _unloadBal =
                new UnloadTransactionBAL(configuration);

            _bagBal =
                new BagTypeBAL(configuration);
        }


        private int CurrentPersonId()
        {
            return int.TryParse(
                User.FindFirstValue("PersonId"),
                out var personId)
                ? personId
                : 0;
        }


        [HttpGet]
        public IActionResult Index()
        {
            int methPersonId =
                CurrentPersonId();

            var assignments =
                _unloadBal.GetMethAssignments(
                    methPersonId
                );

            ViewBag.PendingUnloads =
                assignments
                    .Where(x =>
                        x.Status == "Assigned")
                    .ToList();

            ViewBag.CompletedUnloads =
                assignments
                    .Where(x =>
                        x.Status == "Unloaded" ||
                        x.Status == "Verified")
                    .ToList();

            return View();
        }


        [HttpGet]
        public IActionResult ExecuteUnload(int id)
        {
            int methPersonId =
                CurrentPersonId();

            var unload =
                _unloadBal
                    .GetMethAssignments(
                        methPersonId
                    )
                    .FirstOrDefault(
                        x => x.UnloadId == id
                    );

            if (unload == null)
                return NotFound();

            if (unload.Status != "Assigned")
            {
                TempData["ErrorMessage"] =
                    "This unloading has already been submitted.";

                return RedirectToAction(
                    nameof(Index)
                );
            }

            var workers =
                _unloadBal
                    .GetWorkersByMeth(
                        methPersonId
                    );

            unload.WorkerRows =
                workers
                    .Select(x =>
                        new WorkerAllocation
                        {
                            WorkerId =
                                x.PersonId,

                            WorkerName =
                                x.PersonName
                        })
                    .ToList();

            ViewBag.BagTypes =
                _bagBal.GetAllBagTypes();

            return View(unload);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ExecuteUnload(UnloadTransaction unload)
        {
            int methPersonId = 0;

            var personClaim = User.Claims
                .FirstOrDefault(c => c.Type == "PersonId")
                ?.Value;

            if (!string.IsNullOrWhiteSpace(personClaim))
            {
                int.TryParse(
                    personClaim,
                    out methPersonId
                );
            }


            if (methPersonId <= 0)
            {
                return Forbid();
            }


            // ============================================================
            // ASSIGNMENT VERIFY
            // ============================================================

            var saved = _unloadBal
                .GetMethAssignments(methPersonId)
                .FirstOrDefault(x =>
                    x.UnloadId == unload.UnloadId
                );


            if (saved == null)
            {
                return NotFound();
            }


            if (saved.MethId != methPersonId)
            {
                return Forbid();
            }


            if (saved.Status != "Assigned")
            {
                TempData["ErrorMessage"] =
                    "This unloading assignment is no longer pending.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            // ============================================================
            // ONLY ACTUAL WORK ROWS
            // ============================================================

            var workerRows =
                unload.WorkerRows?
                .Where(x =>
                    x.WorkerId > 0 &&
                    x.BagTypeId.GetValueOrDefault() > 0 &&
                    x.BagCount > 0
                )
                .ToList()
                ?? new List<WorkerAllocation>();


            // ============================================================
            // VALIDATION
            // ============================================================

            if (workerRows.Count == 0)
            {
                ModelState.Clear();

                ModelState.AddModelError(
                    string.Empty,
                    "Please enter work details for at least one worker."
                );

                PrepareExecuteUnloadView(
                    saved,
                    methPersonId
                );

                return View(saved);
            }


            foreach (var row in workerRows)
            {
                if (row.WorkType != "Load" &&
                    row.WorkType != "Unload")
                {
                    ModelState.Clear();

                    ModelState.AddModelError(
                        string.Empty,
                        $"Please select Loading or Unloading for {row.WorkerName}."
                    );

                    PrepareExecuteUnloadView(
                        saved,
                        methPersonId
                    );

                    return View(saved);
                }


                if (row.BagTypeId.GetValueOrDefault() <= 0)
                {
                    ModelState.Clear();

                    ModelState.AddModelError(
                        string.Empty,
                        $"Please select Bag Type for {row.WorkerName}."
                    );

                    PrepareExecuteUnloadView(
                        saved,
                        methPersonId
                    );

                    return View(saved);
                }


                if (row.BagCount <= 0)
                {
                    ModelState.Clear();

                    ModelState.AddModelError(
                        string.Empty,
                        $"Bag Count must be greater than zero for {row.WorkerName}."
                    );

                    PrepareExecuteUnloadView(
                        saved,
                        methPersonId
                    );

                    return View(saved);
                }


                if (row.PerBagCharge < 0)
                {
                    ModelState.Clear();

                    ModelState.AddModelError(
                        string.Empty,
                        $"Per Bag Charge cannot be negative for {row.WorkerName}."
                    );

                    PrepareExecuteUnloadView(
                        saved,
                        methPersonId
                    );

                    return View(saved);
                }
            }


            // ============================================================
            // SAVE
            // ============================================================

            try
            {
                _unloadBal.CompleteMethUnload(
                    unload.UnloadId,
                    methPersonId,
                    workerRows
                );


                TempData["SuccessMessage"] =
                    $"RST {saved.RSTNumber} work entry saved successfully.";


                // IMPORTANT:
                // Same ExecuteUnload page return nahi karna.
                // Dashboard par redirect karna hai.
                return RedirectToAction(
                    nameof(Index)
                );
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                ModelState.Clear();

                ModelState.AddModelError(
                    string.Empty,
                    ex.Message
                );
            }
            catch (Exception ex)
            {
                ModelState.Clear();

                ModelState.AddModelError(
                    string.Empty,
                    ex.Message
                );
            }


            PrepareExecuteUnloadView(
                saved,
                methPersonId
            );

            return View(saved);
        }
        private void PrepareExecuteUnloadView(
      UnloadTransaction unload,
      int methPersonId)
        {
            var workers =
                _unloadBal.GetWorkersByMeth(
                    methPersonId
                );

            unload.WorkerRows = workers
                .Select(w => new WorkerAllocation
                {
                    WorkerId = w.PersonId,
                    WorkerName = w.PersonName ?? ""
                })
                .ToList();

            ViewBag.BagTypes =
                _bagBal.GetAllBagTypes();
        }
    }
}