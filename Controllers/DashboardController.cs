using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RiceMillProject.BAL;
using RiceMillProject.Models;

namespace RiceMillProject.Controllers
{
    [Authorize]
    public partial class DashboardController(IConfiguration configuration) : Controller
    {
        private readonly DashboardBAL _dashboard = new(configuration);

        public IActionResult Index(DateTime? fromDate = null, DateTime? toDate = null)
        {
            // =====================================================
            // ROLE BASED REDIRECT
            // =====================================================

            if (User.IsGatemanUser())
            {
                return RedirectToAction(
                    "Dashboard",
                    "Gateman"
                );
            }

            if (User.IsWeightmanUser())
            {
                return RedirectToAction(
                    "Dashboard",
                    "GateEntry"
                );
            }

            if (User.IsSupervisorUser())
            {
                return RedirectToAction(
                    "Dashboard",
                    "Unload"
                );
            }

            if (User.IsMethUser())
            {
                return RedirectToAction(
                    "Dashboard",
                    "Meth"
                );
            }

            if (User.IsLabUser())
            {
                return RedirectToAction(
                    "Index",
                    "Lab"
                );
            }


            if (!User.IsAdminUser()) return Forbid();
            var rangeEnd = (toDate ?? PurchaseReportBuilder.Today).Date;
            var rangeStart = (fromDate ?? rangeEnd.AddDays(-29)).Date;
            if (!ModelState.IsValid || rangeStart > rangeEnd || rangeEnd.Year > 9998 || (rangeEnd - rangeStart).TotalDays > 366 || User.CompanyId() <= 0) return BadRequest("Select a valid company and date range (up to 366 days).");
            return View(_dashboard.GetAdminDashboard(User.CompanyId(), rangeStart, rangeEnd));
        }
    }
}
