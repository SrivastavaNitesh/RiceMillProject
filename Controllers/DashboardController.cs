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
            if (User.IsGatemanUser())
            {
                return RedirectToAction("Dashboard", "Gateman");
            }

            if (User.IsWeightmanUser())
            {
                return RedirectToAction("Dashboard", "GateEntry");
            }

            if (User.IsSupervisorUser()) return RedirectToAction("Index", "Unload");
            if (User.IsLabUser()) return RedirectToAction("Index", "Lab");

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
