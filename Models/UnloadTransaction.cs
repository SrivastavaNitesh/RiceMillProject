using System;

namespace RiceMillProject.Models
{
    public class UnloadTransaction
    {
        public int UnloadId { get; set; }
        [System.ComponentModel.DataAnnotations.MinLength(1, ErrorMessage = "Select the items actually unloaded.")]
        public int[] SelectedItemIds { get; set; } = [];
        public string RSTNumber { get; set; } = string.Empty;
        public int OfficeId { get; set; }
        public int[]? SelectedLocationIds { get; set; }
        public int SupervisorId { get; set; }
        public int GateManId { get; set; }
        public int MethId { get; set; }
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
    }
}
