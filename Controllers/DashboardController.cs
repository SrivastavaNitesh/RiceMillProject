using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RiceMillProject.BAL;
using RiceMillProject.Models;

namespace RiceMillProject.Controllers
{
    [Authorize]
    public class DashboardController(IConfiguration configuration) : Controller
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
                    "Index",
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
            var rangeEnd = (toDate ?? DateTime.Today).Date;
            var rangeStart = (fromDate ?? rangeEnd.AddDays(-29)).Date;
            return View(_dashboard.GetAdminDashboard(User.CompanyId(), rangeStart, rangeEnd));
        }
    }
}
