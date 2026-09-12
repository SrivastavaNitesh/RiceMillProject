using System;

namespace RiceMillProject.Models
{
    public class Settlement
    {
        public string RSTNumber { get; set; } = string.Empty;
        public string PartyName { get; set; } = string.Empty;
        public string VehicleNumber { get; set; } = string.Empty;
        
        public decimal GrossWeight { get; set; }
        public decimal TareWeight { get; set; }
        public decimal NetWeight { get; set; }
        
        public decimal TotalBagDeductionKG { get; set; }
        public decimal LabDeductionPct { get; set; }
        
        public decimal FinalPayableWeight { get; set; }
        public decimal TotalPalledariCharges { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
