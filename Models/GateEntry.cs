using System;
using System.Collections.Generic;

namespace RiceMillProject.Models
{
    public class GateEntry
    {
        public string RSTNumber { get; set; } = string.Empty;
        public string InwardNo { get; set; } = string.Empty;
        public int VehicleId { get; set; }
        public int PartyId { get; set; }
        public int DriverId { get; set; }
        public decimal GrossWeight { get; set; }
        public int? TargetOfficeId { get; set; }
        public List<int>? TargetLocationIds { get; set; }
        public DateTime GateEntryTime { get; set; }
        public decimal? TareWeight { get; set; }
        public decimal? NetWeight { get; set; }
        public DateTime? GateExitTime { get; set; }
        public int StatusId { get; set; } = 1;
        public string Status { get; set; } = "Gadi In";

        // Display properties for listing
        public string VehicleNumber { get; set; } = string.Empty;
        public string PartyName { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public string? DriverMobile { get; set; }
        public int? TotalBags { get; set; }
        public decimal WeighmentCharge { get; set; } = 0;
        public string InwardOutward { get; set; } = "Inward";
        public int CreatedBy { get; set; } = 1;
        public string? GateManName { get; set; } = "Weightman";
    }
}
