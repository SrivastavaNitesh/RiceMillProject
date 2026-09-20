using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RiceMillProject.Models;
using System.Diagnostics;

namespace RiceMillProject.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                if (User.IsGatemanUser())
                    return RedirectToAction("Dashboard", "Gateman");

                if (User.IsWeightmanUser())
                    return RedirectToAction("Dashboard", "GateEntry");

                if (User.IsLabUser())
                    return RedirectToAction("Index", "Lab");
                if (User.IsMethUser())
                    return RedirectToAction("Index", "Meth");
                if (User.IsSupervisorUser())
                    return RedirectToAction("Index", "Unload");

                return RedirectToAction("Index", "Dashboard");
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var exceptionHandlerPathFeature = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
            if (exceptionHandlerPathFeature?.Error != null)
            {
                HttpContext.Items["ExceptionMessage"] = exceptionHandlerPathFeature.Error.Message + "\n" + exceptionHandlerPathFeature.Error.StackTrace;
            }
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
