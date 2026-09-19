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

        public string CreateGateEntry(GateEntry entry)
        {
            return _gateDal.CreateGateEntry(entry);
        }

        public List<dynamic> GetDriversWithMobile()
        {
            return _gateDal.GetDriversWithMobile();
        }

        public bool CompleteGateExit(string rstNumber, decimal tareWeight)
        {
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

        public List<dynamic> GetDriversByParty(int partyId)
        {
            return _gateDal.GetDriversByParty(partyId);
        }
    }
}
