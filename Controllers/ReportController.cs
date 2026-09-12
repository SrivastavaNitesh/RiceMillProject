using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RiceMillProject.BAL;

namespace RiceMillProject.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportController : Controller
    {
        private readonly GateEntryBAL _gateBal;
        private readonly UnloadTransactionBAL _unloadBal;

        public ReportController(IConfiguration configuration)
        {
            _gateBal = new GateEntryBAL(configuration);
            _unloadBal = new UnloadTransactionBAL(configuration);
        }

        public IActionResult Index()
        {
            var gateEntries = _gateBal.GetAllGateEntries();
            var unloads = _unloadBal.GetAllUnloading();
            
            ViewBag.TotalGateEntries = gateEntries.Count;
            ViewBag.TotalUnloads = unloads.Count;
            
            return View();
        }

        public IActionResult DailyGateReport()
        {
            var gateEntries = _gateBal.GetAllGateEntries();
            return View(gateEntries);
        }
        
        public IActionResult UnloadSummaryReport()
        {
            var unloads = _unloadBal.GetAllUnloading();
            return View(unloads);
        }
    }
}
