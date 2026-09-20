using System;
using System.Collections.Generic;

using System.ComponentModel.DataAnnotations;

namespace RiceMillProject.Models
{
    public class GateEntry
    {
        public string RSTNumber { get; set; } = string.Empty;
        [Required(ErrorMessage = "Gate inward entry is required.")]
        public string InwardNo { get; set; } = string.Empty;
        [Range(1, int.MaxValue, ErrorMessage = "Vehicle is required.")]
        public int VehicleId { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Party is required.")]
        public int PartyId { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Driver is required.")]
        public int DriverId { get; set; }
        [Range(typeof(decimal), "0.01", "999999999999", ErrorMessage = "Gross Weight must be greater than zero.")]
        public decimal GrossWeight { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Material / Variety is required.")]
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
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
