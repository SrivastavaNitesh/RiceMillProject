using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RiceMillProject.Models;
using System.Data;
using System.Security.Claims;

namespace RiceMillProject.Controllers
{
    [Authorize]
    public class TempMaterialController : Controller
    {
        private readonly BusinessLayer _busLayer;

        public TempMaterialController(IConfiguration configuration)
        {
            _busLayer = new BusinessLayer(configuration);
        }

        public IActionResult Index()
        {
            var dt = _busLayer.GetAllTempMaterials();
            var items = new List<TempMaterialRegister>();
            if (dt != null && dt.Rows.Count > 0)
            {
                foreach (DataRow row in dt.Rows)
                {
                    items.Add(new TempMaterialRegister
                    {
                        TempMaterialId = Convert.ToInt32(row["TempMaterialId"]),
                        EntryNo = row["EntryNo"].ToString() ?? "",
                        OutwardNo = row["OutwardNo"].ToString(),
                        GateId = Convert.ToInt32(row["GateId"]),
                        EntryDateTime = Convert.ToDateTime(row["EntryDateTime"]),
                        OwnerVendor = row["OwnerVendor"].ToString() ?? "",
                        VehicleNo = row["VehicleNo"].ToString(),
                        ItemCategory = row["ItemCategory"].ToString() ?? "",
                        ItemDescription = row["ItemDescription"].ToString() ?? "",
                        Quantity = Convert.ToDecimal(row["Quantity"]),
                        Unit = row["Unit"].ToString() ?? "",
                        SerialAssetNo = row["SerialAssetNo"].ToString(),
                        Purpose = row["Purpose"].ToString() ?? "",
                        DepartmentId = row["DepartmentId"] != DBNull.Value ? Convert.ToInt32(row["DepartmentId"]) : null,
                        ExpectedReturnDate = row["ExpectedReturnDate"] != DBNull.Value ? Convert.ToDateTime(row["ExpectedReturnDate"]) : null,
                        ReturnDateTime = row["ReturnDateTime"] != DBNull.Value ? Convert.ToDateTime(row["ReturnDateTime"]) : null,
                        GatePassNo = row["GatePassNo"].ToString(),
                        Status = row["Status"].ToString() ?? "In Yard",
                        Remarks = row["Remarks"].ToString()
                    });
                }
            }
            return View(items);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Gates = Allclass.CreateDropdown(_busLayer.ManageGateMaster("GETALL"));
            ViewBag.Departments = Allclass.CreateDropdown(_busLayer.ManageDepartmentMaster("GETALL"));
            return View(new TempMaterialRegister());
        }

        [HttpPost]
        public IActionResult Create(TempMaterialRegister item)
        {
            int createdBy = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var dt = _busLayer.CreateTempMaterialEntry(item, createdBy);

            if (dt != null && dt.Rows.Count > 0)
            {
                TempData["SuccessMessage"] = "Temporary Tools Inward Registered! Gate Pass No: " + dt.Rows[0]["GatePassNo"];
                return RedirectToAction("Index");
            }
            ViewBag.Gates = Allclass.CreateDropdown(_busLayer.ManageGateMaster("GETALL"));
            ViewBag.Departments = Allclass.CreateDropdown(_busLayer.ManageDepartmentMaster("GETALL"));
            return View(item);
        }

        [HttpPost]
        public IActionResult Return(string entryNo, string remarks)
        {
            _busLayer.ReturnTempMaterial(entryNo, remarks);
            TempData["SuccessMessage"] = "Material Return Recorded Successfully.";
            return RedirectToAction("Index");
        }
    }
}
