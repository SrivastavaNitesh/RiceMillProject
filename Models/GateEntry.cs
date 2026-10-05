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


        [Range(
            typeof(decimal),
            "0.01",
            "999999999999",
            ErrorMessage = "Gross Weight must be greater than zero."
        )]
        public decimal GrossWeight { get; set; }


        // Material / Variety is now filled by Supervisor later.
        // Property is preserved because other project code may use ItemId.
        public int ItemId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Select an item category.")]
        public int ItemCategoryId { get; set; }


        public string ItemName { get; set; } = string.Empty;

        public string ItemCategoryName { get; set; } = string.Empty;


        public int? TargetOfficeId { get; set; }


        public List<int>? TargetLocationIds { get; set; }


        public DateTime GateEntryTime { get; set; }


        public decimal? TareWeight { get; set; }


        public decimal? NetWeight { get; set; }


        public DateTime? GateExitTime { get; set; }


        public int StatusId { get; set; } = 1;


        public string Status { get; set; } = "Gadi In";


        // =========================================================
        // DISPLAY PROPERTIES
        // =========================================================

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
