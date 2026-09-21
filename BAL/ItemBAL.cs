using Microsoft.Extensions.Configuration;
using RiceMillProject.DAL;
using RiceMillProject.Models;
using System.Collections.Generic;
using System.Data;

namespace RiceMillProject.BAL
{
    public class ItemBAL
    {
        private readonly ItemDAL _itemDal;

        public ItemBAL(IConfiguration configuration)
        {
            _itemDal = new ItemDAL(configuration);
        }

        public List<Item> GetAllItems()
        {
            return _itemDal.GetAllItems();
        }
        public DataTable GetItemsByCategory(int categoryId)
        {
            return _itemDal.GetItemsByCategory(categoryId);
        }
        public int AddItem(Item item)
        {
            return _itemDal.InsertItem(item);
        }

        public bool UpdateItem(Item item)
        {
            return _itemDal.UpdateItem(item);
        }
        public DataTable GetActiveItemCategories()
        {
            return _itemDal.GetActiveItemCategories();
        }
    }
}
