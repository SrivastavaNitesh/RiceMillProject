using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using RiceMillProject.DAL;
using RiceMillProject.Models;

namespace RiceMillProject.BAL
{
    public class StackTypeMasterBAL
    {
        private readonly StackTypeMasterDAL _stackTypeDal;

        public StackTypeMasterBAL(IConfiguration configuration)
        {
            _stackTypeDal = new StackTypeMasterDAL(configuration);
        }

        public List<StackTypeMaster> GetActiveStackTypes()
        {
            return _stackTypeDal.GetActiveStackTypes();
        }
    }
}