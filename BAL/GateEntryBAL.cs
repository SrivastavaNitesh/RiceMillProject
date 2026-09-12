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
            // Auto generate RST Number if not provided
            if (string.IsNullOrEmpty(entry.RSTNumber))
            {
                entry.RSTNumber = "RST-" + DateTime.Now.ToString("yyyyMMddHHmmss");
            }
            return _gateDal.CreateGateEntry(entry);
        }

        public bool CompleteGateExit(string rstNumber, decimal tareWeight)
        {
            return _gateDal.CompleteGateExit(rstNumber, tareWeight);
        }
    }
}
