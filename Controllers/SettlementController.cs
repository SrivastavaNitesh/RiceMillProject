using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RiceMillProject.BAL;
using System.Linq;

namespace RiceMillProject.Controllers
{
    public class SettlementController : Controller
    {
        private readonly SettlementBAL _settlementBal;
        private readonly GateEntryBAL _gateBal;

        public SettlementController(IConfiguration configuration)
        {
            _settlementBal = new SettlementBAL(configuration);
            _gateBal = new GateEntryBAL(configuration);
        }

        public IActionResult Index()
        {
            // Get trucks that have been quality tested and are ready for settlement
            var readyForBill = _gateBal.GetAllGateEntries()
                .Where(g => g.Status == "Tested" || g.Status == "Exited")
                .ToList();
            return View(readyForBill);
        }

        public IActionResult Bill(string rstNumber)
        {
            var settlement = _settlementBal.GetSettlementDetails(rstNumber);
            if (settlement == null || string.IsNullOrEmpty(settlement.RSTNumber))
            {
                return NotFound();
            }
            return View(settlement);
        }
    }
}
