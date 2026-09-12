using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using RiceMillProject.DAL;
using RiceMillProject.Models;

namespace RiceMillProject.BAL
{
    public class OfficeBAL
    {
        private readonly OfficeDAL _officeDal;

        public OfficeBAL(IConfiguration configuration)
        {
            _officeDal = new OfficeDAL(configuration);
        }

        public List<Office> GetAllOffices()
        {
            return _officeDal.GetAllOffices();
        }

        public List<OfficeLocation> GetAllLocations()
        {
            return _officeDal.GetAllLocations();
        }

        public int AddOffice(Office office)
        {
            return _officeDal.InsertOffice(office);
        }

        public bool UpdateOffice(Office office)
        {
            return _officeDal.UpdateOffice(office);
        }
    }
}
