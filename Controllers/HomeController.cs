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
                if (User.HasClaim("PostId", "5")) return RedirectToAction("Index", "Lab");
                else if (User.HasClaim("PostId", "1")) return RedirectToAction("Dashboard", "Gateman");
                else if (User.HasClaim("PostId", "2")) return RedirectToAction("Index", "GateEntry");
                else if (User.HasClaim("PostId", "3")) return RedirectToAction("Index", "Unload");
                else if (User.HasClaim("PostId", "4")) return RedirectToAction("Index", "Dashboard");
                else if (User.HasClaim("PostId", "5")) return RedirectToAction("Index", "Lab");
                else if (User.HasClaim("PostId", "6")) return RedirectToAction("Index", "Meth");
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
