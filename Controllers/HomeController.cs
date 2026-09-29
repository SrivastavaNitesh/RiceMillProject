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
            if (User.Identity?.IsAuthenticated != true) return View();

            // Use the same access rules as the dashboard and layouts.
            if (User.IsAdminUser()) return RedirectToAction("Index", "Dashboard");
            if (User.IsGatemanUser()) return RedirectToAction("Dashboard", "Gateman");
            if (User.IsWeightmanUser()) return RedirectToAction("Dashboard", "GateEntry");
            if (User.IsSupervisorUser()) return RedirectToAction("Index", "Unload");
            if (User.IsLabUser()) return RedirectToAction("Index", "Lab");
            if (User.IsMethUser()) return RedirectToAction("Index", "Meth");

            // Support existing login cookies containing only PostId.
            return User.FindFirst("PostId")?.Value switch
            {
                "1" => RedirectToAction("Dashboard", "Gateman"),
                "2" => RedirectToAction("Dashboard", "GateEntry"),
                "3" => RedirectToAction("Index", "Unload"),
                "4" => RedirectToAction("Index", "Dashboard"),
                "5" => RedirectToAction("Index", "Lab"),
                "6" => RedirectToAction("Index", "Meth"),
                _ => View()
            };
        }
        public IActionResult Privacy()
        {
            return View();
        }


        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            var exceptionHandlerPathFeature =
                HttpContext.Features
                    .Get<
                        Microsoft.AspNetCore.Diagnostics
                            .IExceptionHandlerPathFeature
                    >();

            if (exceptionHandlerPathFeature?.Error != null)
            {
                HttpContext.Items["ExceptionMessage"] =
                    exceptionHandlerPathFeature.Error.Message
                    + "\n"
                    + exceptionHandlerPathFeature.Error.StackTrace;
            }

            return View(
                new ErrorViewModel
                {
                    RequestId =
                        Activity.Current?.Id
                        ?? HttpContext.TraceIdentifier
                }
            );
        }
    }
}