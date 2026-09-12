using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RiceMillProject.BAL;
using RiceMillProject.Models;

namespace RiceMillProject.Controllers
{
    public class BagTypeController : Controller
    {
        private readonly BagTypeBAL _bagBal;

        public BagTypeController(IConfiguration configuration)
        {
            _bagBal = new BagTypeBAL(configuration);
        }

        public IActionResult Index()
        {
            var bags = _bagBal.GetAllBagTypes();
            return View(bags);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new BagType { IsActive = true });
        }

        [HttpPost]
        public IActionResult Create(BagType bag)
        {
            if (ModelState.IsValid)
            {
                _bagBal.AddBagType(bag);
                return RedirectToAction("Index");
            }
            return View(bag);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var bag = _bagBal.GetAllBagTypes().Find(b => b.BagTypeId == id);
            if (bag == null)
            {
                return NotFound();
            }
            return View(bag);
        }

        [HttpPost]
        public IActionResult Edit(BagType bag)
        {
            if (ModelState.IsValid)
            {
                _bagBal.UpdateBagType(bag);
                return RedirectToAction("Index");
            }
            return View(bag);
        }
        
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var bag = _bagBal.GetAllBagTypes().Find(b => b.BagTypeId == id);
            if (bag != null)
            {
                bag.IsActive = false;
                _bagBal.UpdateBagType(bag);
            }
            return RedirectToAction("Index");
        }
    }
}
