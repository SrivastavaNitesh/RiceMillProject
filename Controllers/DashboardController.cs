using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using RiceMillProject.Models;
using System.Data;

namespace RiceMillProject.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly string _connectionString;

        public DashboardController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public IActionResult Index()
        {
            bool isGatemanOnly = (User.IsInRole("Gate Man") || User.IsInRole("Gateman") || User.FindFirst("PostId")?.Value == "2")
                                && !User.IsInRole("Admin") 
                                && !(User.Identity?.Name ?? "").ToLower().Contains("admin");
            if (isGatemanOnly)
            {
                return RedirectToAction("Dashboard", "Gateman");
            }

            bool isWeightmanOnly = (User.IsInRole("Weighbridge Man") || User.IsInRole("WeightMan") || User.IsInRole("Weightman") || User.IsInRole("Weight Man") || User.FindFirst("PostId")?.Value == "4")
                                  && !User.IsInRole("Admin")
                                  && !(User.Identity?.Name ?? "").ToLower().Contains("admin");
            if (isWeightmanOnly)
            {
                return RedirectToAction("Dashboard", "GateEntry");
            }

            var stats = new DashboardStats();
            
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_GetDashboardStats", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            stats.TotalEntered = (int)reader["TotalEntered"];
                            stats.PendingUnload = (int)reader["PendingUnload"];
                            stats.PendingLab = (int)reader["PendingLab"];
                            stats.PendingSettlement = (int)reader["PendingSettlement"];
                        }
                    }
                }
            }
            
            return View(stats);
        }
    }
}
