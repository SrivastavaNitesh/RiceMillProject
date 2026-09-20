using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RiceMillProject.BAL;
using RiceMillProject.Models;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RiceMillProject.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserBAL _userBal;

        public AccountController(IConfiguration configuration)
        {
            _userBal = new UserBAL(configuration);
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(User model, string? returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                var user = _userBal.ValidateUser(model.Username, model.Password);
                
                if (user != null)
                {
                    var claims = new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                        new Claim("PersonId", user.PersonId.ToString()),
                        new Claim(ClaimTypes.Name, user.PersonName),
                        new Claim(ClaimTypes.Role, user.Role),
                        new Claim("PostId", user.o10_postid.ToString()),
                        new Claim("UserTypeId", user.sa10_usertypeid.ToString()),
                        new Claim("CompanyId", user.OfficeId.ToString()),
                        new Claim("LayoutName", user.LayoutName)
                    };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    
                    return RedirectToAction("Index", "Home");
                }
                
                ModelState.AddModelError("", "Invalid Username or Password.");
            }
            
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
        
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
