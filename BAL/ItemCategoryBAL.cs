using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using RiceMillProject.DAL;
using RiceMillProject.Models;

namespace RiceMillProject.BAL
{
    public class ItemCategoryBAL
    {
        private readonly ItemCategoryDAL _categoryDal;

        public ItemCategoryBAL(IConfiguration configuration)
        {
            _categoryDal = new ItemCategoryDAL(configuration);
        }

        public List<ItemCategory> GetAllItemCategories()
        {
            return _categoryDal.GetAllItemCategories();
        }

        public int AddItemCategory(ItemCategory category)
        {
            return _categoryDal.InsertItemCategory(category);
        }

        public bool UpdateItemCategory(ItemCategory category)
        {
            return _categoryDal.UpdateItemCategory(category);
        }
    }
}
