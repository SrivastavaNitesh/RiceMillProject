using System;

namespace RiceMillProject.Models
{
    public class LabQualityCheck
    {
        public int LabCheckId { get; set; }
        public string RSTNumber { get; set; } = string.Empty;
        public decimal MoisturePct { get; set; }
        public decimal DustPct { get; set; }
        public decimal PayiaPct { get; set; }
        public decimal GrainQualityPct { get; set; }
        public decimal TotalDeductionPct { get; set; }
        public int TestedBy { get; set; }
        public DateTime TestTime { get; set; }
    }
}
