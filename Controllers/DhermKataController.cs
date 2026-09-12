using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RiceMillProject.BAL;
using RiceMillProject.DAL;
using RiceMillProject.Models;
using System.Data;

namespace RiceMillProject.Controllers
{
    public class DhermKataController : Controller
    {
        private readonly BusinessLayer _BusLayer;
        private readonly DhermKata _DhermKata;
        public DhermKataController(IConfiguration configuration)
        {
            _BusLayer = new BusinessLayer(configuration);
            _DhermKata = new DhermKata(configuration);
        }
        public IActionResult Index()
        {
            DhermKata obj = _DhermKata;
            obj = obj.GetAlldhermkata(obj);
            return View(obj);
        }
        [HttpGet]
        public IActionResult Create()
        {

            DhermKata obj = new DhermKata(); 
            DataTable dt = _BusLayer.GetAllMainoffice();
            if (dt!=null && dt.Rows.Count>0)
            {
                obj.MainOfficelist = dt.AsEnumerable().Select(row => new SelectListItem
                {
                    Value = row["OfficeId"].ToString(),
                    Text = row["OfficeName"].ToString()
                }).ToList();
            }
            
            return View(obj);
        }

        [HttpPost]
        public IActionResult Create(DhermKata obj)
        {
            bool flag = false;
            if (ModelState.IsValid)
            {
                flag = _BusLayer.AdddhermKata(obj);
                if (flag)
                {
                    ViewBag.Active = true;
                }
                else
                {
                    ViewBag.Active = false;
                }
                return RedirectToAction("Index");
            }
            return View(obj);
        }
    }
}
