using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using RiceMillProject.DAL;
using RiceMillProject.Models;

namespace RiceMillProject.BAL
{
    public class LocationMasterBAL
    {
        private readonly LocationMasterDAL _dal;

        public LocationMasterBAL(IConfiguration configuration)
        {
            _dal = new LocationMasterDAL(configuration);
        }

        public List<LocationMaster> GetAllLocations() => _dal.GetAllLocations();
        public LocationMaster? GetLocationById(int id) => _dal.GetLocationById(id);
        public bool SaveLocation(LocationMaster model) => _dal.SaveLocation(model);
        public bool DeleteLocation(int id) => _dal.DeleteLocation(id);

        public List<OfficeLocationMapping> GetAllOfficeLocationMappings() => _dal.GetAllOfficeLocationMappings();
        public bool SaveOfficeLocationMapping(OfficeLocationMapping model) => _dal.SaveOfficeLocationMapping(model);
        public bool DeleteOfficeLocationMapping(int id) => _dal.DeleteOfficeLocationMapping(id);
    }
}
