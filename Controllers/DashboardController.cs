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
            _connectionString =
                configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public IActionResult Index()
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
                    "Index",
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


            // =====================================================
            // ADMIN / OTHER DASHBOARD
            // =====================================================

            var stats = new DashboardStats();

            using SqlConnection con =
                new SqlConnection(_connectionString);

            using SqlCommand cmd =
                new SqlCommand(
                    "sp_GetDashboardStats",
                    con
                );

            cmd.CommandType =
                CommandType.StoredProcedure;

            con.Open();

            using SqlDataReader reader =
                cmd.ExecuteReader();

            if (reader.Read())
            {
                stats.TotalEntered =
                    Convert.ToInt32(
                        reader["TotalEntered"]
                    );

                stats.PendingUnload =
                    Convert.ToInt32(
                        reader["PendingUnload"]
                    );

                stats.PendingLab =
                    Convert.ToInt32(
                        reader["PendingLab"]
                    );

                stats.PendingSettlement =
                    Convert.ToInt32(
                        reader["PendingSettlement"]
                    );
            }

            return View(stats);
        }
    }
}