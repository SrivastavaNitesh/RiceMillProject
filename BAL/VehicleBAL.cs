using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using RiceMillProject.DAL;
using RiceMillProject.Models;

namespace RiceMillProject.BAL
{
    public class VehicleBAL
    {
        private readonly VehicleDAL _vehicleDal;

        public VehicleBAL(IConfiguration configuration)
        {
            _vehicleDal = new VehicleDAL(configuration);
        }

        public List<Vehicle> GetAllVehicles()
        {
            return _vehicleDal.GetAllVehicles();
        }

        public int AddVehicle(Vehicle vehicle)
        {
            return _vehicleDal.InsertVehicle(vehicle);
        }

        public bool UpdateVehicle(Vehicle vehicle)
        {
            return _vehicleDal.UpdateVehicle(vehicle);
        }
    }
}
