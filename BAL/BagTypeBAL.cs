using Microsoft.Extensions.Configuration;
using RiceMillProject.DAL;
using RiceMillProject.Models;
using System.Collections.Generic;
using System.Data;

namespace RiceMillProject.BAL
{
    public class BagTypeBAL
    {
        private readonly BagTypeDAL _bagDal;

        public BagTypeBAL(IConfiguration configuration)
        {
            _bagDal = new BagTypeDAL(configuration);
        }

        public List<BagType> GetAllBagTypes()
        {
            return _bagDal.GetAllBagTypes();
        }

        public int AddBagType(BagType bag)
        {
            return _bagDal.InsertBagType(bag);
        }

        public bool UpdateBagType(BagType bag)
        {
            return _bagDal.UpdateBagType(bag);
        }
      
        public DataTable GetActiveBagTypes()
        {
            return _bagDal.GetActiveBagTypes();
        }
    }
}
