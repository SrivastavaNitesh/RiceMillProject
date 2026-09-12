using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RiceMillProject.Models;
using System.Data;

namespace RiceMillProject.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DepartmentController : Controller
    {
        private readonly BusinessLayer _busLayer;

        public DepartmentController(IConfiguration configuration)
        {
            _busLayer = new BusinessLayer(configuration);
        }

        public IActionResult Index()
        {
            var dt = _busLayer.ManageDepartmentMaster("GETALL");
            var depts = new List<DepartmentMaster>();
            if (dt != null && dt.Rows.Count > 0)
            {
                foreach (DataRow row in dt.Rows)
                {
                    depts.Add(new DepartmentMaster
                    {
                        DepartmentId = Convert.ToInt32(row["DepartmentId"]),
                        DepartmentCode = row["DepartmentCode"].ToString() ?? "",
                        DepartmentName = row["DepartmentName"].ToString() ?? "",
                        OfficeName = row["OfficeName"] != DBNull.Value ? row["OfficeName"].ToString() : "N/A",
                        ResponsibleOfficerName = row["ResponsibleOfficerName"] != DBNull.Value ? row["ResponsibleOfficerName"].ToString() : "Unassigned",
                        IsActive = Convert.ToBoolean(row["IsActive"])
                    });
                }
            }
            return View(depts);
        }

        [HttpGet]
        public IActionResult Create()
        {
            DataTable dtOffices = _busLayer.GetAllMainoffice();
            ViewBag.Offices = Allclass.CreateDropdown(dtOffices);
            return View(new DepartmentMaster { IsActive = true });
        }

        [HttpPost]
        public IActionResult Create(DepartmentMaster dept)
        {
            if (ModelState.IsValid)
            {
                _busLayer.ManageDepartmentMaster("INSERT", dept);
                return RedirectToAction("Index");
            }
            DataTable dtOffices = _busLayer.GetAllMainoffice();
            ViewBag.Offices = Allclass.CreateDropdown(dtOffices);
            return View(dept);
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            _busLayer.ManageDepartmentMaster("DELETE", new DepartmentMaster { DepartmentId = id });
            return RedirectToAction("Index");
        }
    }
}
