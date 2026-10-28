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
    public partial class MethController : Controller
    {
        private readonly UnloadTransactionBAL _unloadBal;
        private readonly BagTypeBAL _bagBal;
        private readonly RiceMillProject.DAL.RecordEditDAL _edits;
        private readonly RiceMillProject.DAL.SupervisorReportDAL _reports;
        private readonly MethDashboardBAL _dashboardBal;

        public MethController(
            IConfiguration configuration)
        {
            _edits=new RiceMillProject.DAL.RecordEditDAL(configuration);
            _reports=new RiceMillProject.DAL.SupervisorReportDAL(configuration);
            _unloadBal =
                new UnloadTransactionBAL(configuration);

            _bagBal =
                new BagTypeBAL(configuration);

            _dashboardBal = new MethDashboardBAL(configuration);
        }


        private int CurrentPersonId()
        {
            if (User.IsAdminUser()) return HttpContext.EffectivePersonId();
            return int.TryParse(
                User.FindFirstValue("PersonId"),
                out var personId)
                ? personId
                : 0;
        }


        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpGet]
        public IActionResult Dashboard()
        {
            return LoadDashboard();
        }

        [HttpGet]
        public IActionResult Report(string status="All", string? search=null, DateTime? fromDate=null, DateTime? toDate=null)
        {
            var rows=_unloadBal.GetMethAssignments(CurrentPersonId()).AsEnumerable();
            if(status=="Pending") rows=rows.Where(x=>x.Status=="Assigned"&&!x.IsWorkerWorkCompleted);
            else if(status=="Completed") rows=rows.Where(x=>x.IsWorkerWorkCompleted||x.Status!="Assigned");
            if(!string.IsNullOrWhiteSpace(search)) rows=rows.Where(x=>x.RSTNumber.Contains(search,StringComparison.OrdinalIgnoreCase)||(x.VehicleNumber?.Contains(search,StringComparison.OrdinalIgnoreCase)??false)||(x.PartyName?.Contains(search,StringComparison.OrdinalIgnoreCase)??false));
            if(fromDate.HasValue) rows=rows.Where(x=>x.UnloadTime.HasValue&&x.UnloadTime.Value.Date>=fromDate.Value.Date);
            if(toDate.HasValue) rows=rows.Where(x=>x.UnloadTime.HasValue&&x.UnloadTime.Value.Date<=toDate.Value.Date);
            return View(new MethWorkReport{Status=status,Search=search??"",FromDate=fromDate,ToDate=toDate,Rows=rows.ToList()});
        }

        private IActionResult LoadDashboard()
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
                        x.Status == "Assigned" &&
                        !x.IsWorkerWorkCompleted)
                    .ToList();

            ViewBag.CompletedUnloads =
                assignments
                    .Where(x =>
                        x.IsWorkerWorkCompleted ||
                        x.Status != "Assigned")
                    .ToList();

            ViewBag.ViewMode = "Dashboard";

            return View("Index", _dashboardBal.Get(methPersonId));
        }


        [HttpGet]
        public IActionResult ExecuteUnload(int id, bool edit=false, bool viewOnly=false)
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

            if (!edit && !viewOnly && _edits.Exists("Labour",id.ToString()))
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

            if(edit || viewOnly){
                if(!_edits.Exists("Labour",id.ToString()))return NotFound();
                if(_edits.Access(ActorId,"Labour",id.ToString())==null)return Forbid();
                var saved=_edits.Load(ActorId,"Labour",id.ToString());
                if(!viewOnly&&!saved.Access.CanEdit)return StatusCode(403,saved.Access.LockedReason);
                SeedMethDetails(unload,saved,viewOnly);
            }
            ViewBag.ItemCategoryName = _reports.CategoryNames(methPersonId, User.CompanyId(), true).GetValueOrDefault(unload.UnloadId, "—");
            return View(unload);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ExecuteUnload(UnloadTransaction unload)
        {
            int methPersonId = CurrentPersonId();


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


            if (_edits.Exists("Labour",unload.UnloadId.ToString())) return SaveMethCorrection(unload);

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

            var returnRows = unload.HasDamagedReturn
                ? (unload.ReturnRows ?? []).Where(x => x.BagTypeId > 0 && x.BagCount > 0).ToList()
                : [];
            if (unload.HasDamagedReturn)
            {
                if (returnRows.Count == 0 || string.IsNullOrWhiteSpace(unload.ReturnReason))
                {
                    ModelState.Clear();
                    ModelState.AddModelError(string.Empty, "Damaged/return bag type, quantity and reason are required.");
                    PrepareExecuteUnloadView(saved, methPersonId);
                    return View(saved);
                }
                if (returnRows.GroupBy(x => x.BagTypeId).Any(x => x.Count() > 1))
                {
                    ModelState.Clear();
                    ModelState.AddModelError(string.Empty, "Same return bag type cannot be entered twice.");
                    PrepareExecuteUnloadView(saved, methPersonId);
                    return View(saved);
                }
                if (returnRows.Sum(x => x.BagCount) > workerRows.Sum(x => x.BagCount))
                {
                    ModelState.Clear();
                    ModelState.AddModelError(string.Empty, "Damaged/return bags cannot exceed total handled bags.");
                    PrepareExecuteUnloadView(saved, methPersonId);
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
                    workerRows,
                    returnRows,
                    unload.ReturnReason
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
            ViewBag.ItemCategoryName = _reports.CategoryNames(methPersonId, User.CompanyId(), true).GetValueOrDefault(unload.UnloadId, "—");
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
