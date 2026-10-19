using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using RiceMillProject.Models;
using System.Data;

namespace RiceMillProject.Controllers;

[Authorize]
public class AdminOperationsController : Controller
{
    private readonly string _connectionString;
    private static readonly Dictionary<string, int> Roles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Gateman"] = UserAccess.GateManUserTypeId,
        ["Weightman"] = UserAccess.WeightManUserTypeId,
        ["Supervisor"] = UserAccess.SupervisorUserTypeId,
        ["Worker"] = UserAccess.MethUserTypeId
    };

    public AdminOperationsController(IConfiguration configuration) =>
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;

    [HttpGet]
    public IActionResult Index(string role = "Gateman")
    {
        if (!User.IsAdminUser()) return Forbid();
        if (!Roles.ContainsKey(role)) role = "Gateman";
        return View(BuildModel(role));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Select(AdminOperationViewModel model)
    {
        if (!User.IsAdminUser()) return Forbid();
        var selected = GetOperators(model.Role).FirstOrDefault(x => x.UserId == model.UserId);
        if (selected == default)
        {
            ModelState.AddModelError(nameof(model.UserId), "Selected operator is unavailable for this company.");
            return View("Index", BuildModel(model.Role));
        }
        HttpContext.Session.SetString(AdminWorkContext.RoleKey, model.Role);
        HttpContext.Session.SetInt32(AdminWorkContext.UserKey, selected.UserId);
        HttpContext.Session.SetInt32(AdminWorkContext.PersonKey, selected.PersonId);
        HttpContext.Session.SetString(AdminWorkContext.NameKey, selected.Name);
        return model.Role switch
        {
            "Gateman" => RedirectToAction("Index", "Gateman"),
            "Weightman" => RedirectToAction("Report", "Gateman"),
            "Supervisor" => RedirectToAction("Dashboard", "Unload"),
            _ => RedirectToAction("Index", "Meth")
        };
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Clear()
    {
        ClearWorkContext();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Exit()
    {
        if (!User.IsAdminUser()) return Forbid();
        ClearWorkContext();
        return RedirectToAction("Index", "Dashboard");
    }

    private void ClearWorkContext()
    {
        HttpContext.Session.Remove(AdminWorkContext.RoleKey);
        HttpContext.Session.Remove(AdminWorkContext.UserKey);
        HttpContext.Session.Remove(AdminWorkContext.PersonKey);
        HttpContext.Session.Remove(AdminWorkContext.NameKey);
    }

    private AdminOperationViewModel BuildModel(string role)
    {
        var operators = GetOperators(role);
        return new AdminOperationViewModel
        {
            Role = role,
            UserId = HttpContext.Session.GetInt32(AdminWorkContext.UserKey) ?? 0,
            Operators = operators.Select(x => new SelectListItem(x.Name, x.UserId.ToString())).ToList()
        };
    }

    private List<(int UserId, int PersonId, string Name)> GetOperators(string role)
    {
        if (!Roles.TryGetValue(role, out var userTypeId)) return [];
        var officeId = User.CompanyId();
        var result = new List<(int, int, string)>();
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(@"SELECT u.UserId,u.PersonId,p.PersonName FROM dbo.sa05_user u INNER JOIN dbo.P02_Person p ON p.PersonId=u.PersonId WHERE u.IsActive=1 AND p.IsActive=1 AND u.sa10_usertypeid=@UserTypeId AND (@OfficeId=0 OR u.OfficeId=@OfficeId) ORDER BY p.PersonName", connection);
        command.Parameters.Add("@UserTypeId", SqlDbType.Int).Value = userTypeId;
        command.Parameters.Add("@OfficeId", SqlDbType.Int).Value = officeId;
        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read()) result.Add((reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2)));
        return result;
    }
}
