using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RiceMillProject.BAL;
using RiceMillProject.Models;

namespace RiceMillProject.Controllers
{
    public class ItemCategoryController : Controller
    {
        private readonly ItemCategoryBAL _categoryBal;

        public ItemCategoryController(IConfiguration configuration)
        {
            _categoryBal = new ItemCategoryBAL(configuration);
        }

        public IActionResult Index()
        {
            var categories = _categoryBal.GetAllItemCategories();
            return View(categories);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new ItemCategory { IsActive = true });
        }

        [HttpPost]
        public IActionResult Create(ItemCategory category)
        {
            if (ModelState.IsValid)
            {
                _categoryBal.AddItemCategory(category);
                return RedirectToAction("Index");
            }
            return View(category);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var category = _categoryBal.GetAllItemCategories().Find(c => c.CategoryId == id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        [HttpPost]
        public IActionResult Edit(ItemCategory category)
        {
            if (ModelState.IsValid)
            {
                _categoryBal.UpdateItemCategory(category);
                return RedirectToAction("Index");
            }
            return View(category);
        }
        
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var category = _categoryBal.GetAllItemCategories().Find(c => c.CategoryId == id);
            if (category != null)
            {
                category.IsActive = false;
                _categoryBal.UpdateItemCategory(category);
            }
            return RedirectToAction("Index");
        }
    }
}
