using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using RiceMillProject.DAL;
using RiceMillProject.Models;

namespace RiceMillProject.BAL
{
    public class GateEntryBAL
    {
        private readonly GateEntryDAL _gateDal;

        public GateEntryBAL(IConfiguration configuration)
        {
            _gateDal = new GateEntryDAL(configuration);
        }

        public List<GateEntry> GetAllGateEntries()
        {
            return _gateDal.GetAllGateEntries();
        }

        public List<PendingRstInward> GetPendingRstInwards() => _gateDal.GetPendingRstInwards();

        public string CreateGateEntry(GateEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));
            if (string.IsNullOrWhiteSpace(entry.InwardNo))
                throw new ArgumentException("Inward entry is required.", nameof(entry));
            if (entry.GrossWeight <= 0)
                throw new ArgumentException("Gross weight must be greater than zero.", nameof(entry));
            if (entry.ItemCategoryId <= 0)
                throw new ArgumentException("Item category is required.", nameof(entry));
            if (entry.PartyId <= 0)
                throw new ArgumentException("Party is required.", nameof(entry));
            if (entry.VehicleId <= 0)
                throw new ArgumentException("Vehicle is required.", nameof(entry));
            if (entry.DriverId <= 0)
                throw new ArgumentException("Driver is required.", nameof(entry));

            return _gateDal.CreateGateEntry(entry);
        }

        public List<dynamic> GetDriversWithMobile()
        {
            return _gateDal.GetDriversWithMobile();
        }

        public System.Data.DataTable GetItemCategories() => _gateDal.GetItemCategories();

        public bool CompleteGateExit(string rstNumber, decimal tareWeight)
        {
            if (string.IsNullOrWhiteSpace(rstNumber))
                throw new ArgumentException("RST number is required.", nameof(rstNumber));
            if (tareWeight <= 0)
                throw new ArgumentException("Tare weight must be greater than zero.", nameof(tareWeight));

            return _gateDal.CompleteGateExit(rstNumber, tareWeight);
        }

        public System.Data.DataTable GetInwardDetails(string inwardNo)
        {
            return _gateDal.GetInwardDetails(inwardNo);
        }

        public System.Data.DataTable GetInwardDetailsById(int inwardId)
        {
            return _gateDal.GetInwardDetailsById(inwardId);
        }

        public dynamic GetLinkageByParty(int partyId)
        {
            return _gateDal.GetLinkageByParty(partyId);
        }

        public List<dynamic> GetVehiclesByParty(int partyId)
        {
            return _gateDal.GetVehiclesByParty(partyId);
        }

        public List<dynamic> GetDriversByVehicle(
            int partyId,
            int vehicleId)
        {
            return _gateDal.GetDriversByVehicle(
                partyId,
                vehicleId);
        }

        public List<dynamic> GetDriversByParty(int partyId)
        {
            return _gateDal.GetDriversByParty(partyId);
        }


        public int QuickAddParty(
            string partyName,
            string? mobileNumber)
        {
            return _gateDal.QuickAddParty(
                partyName,
                mobileNumber);
        }


        public int QuickAddVehicle(
            int partyId,
            string vehicleNumber)
        {
            return _gateDal.QuickAddVehicle(
                partyId,
                vehicleNumber);
        }


        public int QuickAddDriver(
            int partyId,
            int vehicleId,
            string driverName,
            string? mobileNumber)
        {
            return _gateDal.QuickAddDriver(
                partyId,
                vehicleId,
                driverName,
                mobileNumber);
        }


        public void EnsureVehicleMapping(
            int partyId,
            int vehicleId,
            int driverId)
        {
            _gateDal.EnsureVehicleMapping(
                partyId,
                vehicleId,
                driverId);
        }
    }
}
