using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using RiceMillProject.BAL;
using RiceMillProject.Models;
using System.Linq;

namespace RiceMillProject.Controllers
{
    public class ItemController : Controller
    {
        private readonly ItemBAL _itemBal;
        private readonly ItemCategoryBAL _categoryBal;

        public ItemController(IConfiguration configuration)
        {
            _itemBal = new ItemBAL(configuration);
            _categoryBal = new ItemCategoryBAL(configuration);
        }

        public IActionResult Index()
        {
            var items = _itemBal.GetAllItems();
            return View(items);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Categories = new SelectList(_categoryBal.GetAllItemCategories(), "CategoryId", "CategoryName");
            return View(new Item { IsActive = true });
        }

        [HttpPost]
        public IActionResult Create(Item item)
        {
            if (ModelState.IsValid)
            {
                _itemBal.AddItem(item);
                return RedirectToAction("Index");
            }
            ViewBag.Categories = new SelectList(_categoryBal.GetAllItemCategories(), "CategoryId", "CategoryName", item.CategoryId);
            return View(item);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var item = _itemBal.GetAllItems().Find(i => i.ItemId == id);
            if (item == null)
            {
                return NotFound();
            }
            ViewBag.Categories = new SelectList(_categoryBal.GetAllItemCategories(), "CategoryId", "CategoryName", item.CategoryId);
            return View(item);
        }

        [HttpPost]
        public IActionResult Edit(Item item)
        {
            if (ModelState.IsValid)
            {
                _itemBal.UpdateItem(item);
                return RedirectToAction("Index");
            }
            ViewBag.Categories = new SelectList(_categoryBal.GetAllItemCategories(), "CategoryId", "CategoryName", item.CategoryId);
            return View(item);
        }
        
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var item = _itemBal.GetAllItems().Find(i => i.ItemId == id);
            if (item != null)
            {
                item.IsActive = false;
                _itemBal.UpdateItem(item);
            }
            return RedirectToAction("Index");
        }
    }
}
