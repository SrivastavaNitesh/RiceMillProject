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
            // GATEMAN
            // Existing code + other developer code BOTH preserved
            // =====================================================

            bool isGatemanOnly =
                (
                    User.IsInRole("Gate Man")
                    || User.IsInRole("Gateman")
                    || User.FindFirst("PostId")?.Value == "1"
                )
                && !User.IsInRole("Admin")
                && !(User.Identity?.Name ?? "")
                    .ToLower()
                    .Contains("admin");


            if (User.IsGatemanUser() || isGatemanOnly)
            {
                return RedirectToAction(
                    "Dashboard",
                    "Gateman"
                );
            }


            // =====================================================
            // WEIGHTMAN
            // =====================================================

            if (User.IsWeightmanUser())
            {
                return RedirectToAction(
                    "Dashboard",
                    "GateEntry"
                );
            }


            // =====================================================
            // SUPERVISOR
            // =====================================================

            if (User.IsSupervisorUser())
            {
                return RedirectToAction(
                    "Index",
                    "Unload"
                );
            }


            // =====================================================
            // METH
            // =====================================================

            if (User.IsMethUser())
            {
                return RedirectToAction(
                    "Index",
                    "Meth"
                );
            }


            // =====================================================
            // LAB
            // =====================================================

            if (User.IsLabUser())
            {
                return RedirectToAction(
                    "Index",
                    "Lab"
                );
            }


            // =====================================================
            // ADMIN / GENERAL DASHBOARD
            // =====================================================

            var stats = new DashboardStats();

            using (SqlConnection con =
                   new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd =
                       new SqlCommand(
                           "sp_GetDashboardStats",
                           con))
                {
                    cmd.CommandType =
                        CommandType.StoredProcedure;

                    con.Open();

                    using (SqlDataReader reader =
                           cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            stats.TotalEntered =
                                Convert.ToInt32(
                                    reader["TotalEntered"]);

                            stats.PendingUnload =
                                Convert.ToInt32(
                                    reader["PendingUnload"]);

                            stats.PendingLab =
                                Convert.ToInt32(
                                    reader["PendingLab"]);

                            stats.PendingSettlement =
                                Convert.ToInt32(
                                    reader["PendingSettlement"]);
                        }
                    }
                }
            }

            return View(stats);
        }
    }
}