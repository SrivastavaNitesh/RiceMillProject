using System;
using System.Collections.Generic;
namespace RiceMillProject.Models
{
    public class UnloadTransaction
    {
        public int? LocationId { get; set; }

        public string? VehicleNumber { get; set; }

        public string? PartyName { get; set; }

        public string? OfficeName { get; set; }

        public decimal GrossWeight { get; set; }

        public decimal TareWeight { get; set; }

        public decimal NetWeight { get; set; }

        public List<WorkerAllocation> WorkerRows { get; set; } = new();
        public int UnloadId { get; set; }
        [System.ComponentModel.DataAnnotations.MinLength(1, ErrorMessage = "Select the items actually unloaded.")]
        public int[] SelectedItemIds { get; set; } = [];
        public string RSTNumber { get; set; } = string.Empty;
        public int OfficeId { get; set; }
        public int[]? SelectedLocationIds { get; set; }
        public int SupervisorId { get; set; }
        public int GateManId { get; set; }
        public int MethId { get; set; }
        public int? ItemId { get; set; }
        public int? BagTypeId { get; set; }
        public int? NumberOfBags { get; set; }
        public decimal? TotalBagDeductionGrams { get; set; }
        public DateTime? UnloadTime { get; set; }
        public string Status { get; set; } = "Assigned";
        
        // Display Fields
        public string? LocationName { get; set; }
        public string? SupervisorName { get; set; }
        public string? MethName { get; set; }
        public string? BagTypeName { get; set; }
        public string? ItemName { get; set; }
    }
}
