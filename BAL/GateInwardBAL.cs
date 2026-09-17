using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using RiceMillProject.DAL;
using RiceMillProject.Models;

namespace RiceMillProject.BAL
{
    public class GateInwardBAL
    {
        private readonly GateInwardDAL _dal;

        public GateInwardBAL(IConfiguration configuration)
        {
            _dal = new GateInwardDAL(configuration);
        }

        public List<InwardTypeMaster> GetInwardTypes() => _dal.GetInwardTypes();
        public List<VehicleTypeMaster> GetVehicleTypes() => _dal.GetVehicleTypes();
        public List<InwardHeader> GetAllInwardEntries() => _dal.GetAllInwardEntries();
        public bool SaveInwardEntry(InwardHeader model) => _dal.SaveInwardEntry(model);
        public List<dynamic> GetGatemenList() => _dal.GetGatemenList();
        public bool IsDriverMobileDuplicate(string mobileNumber) => _dal.IsDriverMobileDuplicate(mobileNumber);
    }
}
