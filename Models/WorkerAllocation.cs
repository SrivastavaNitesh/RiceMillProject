using System;

namespace RiceMillProject.Models
{
    public class WorkerAllocation
    {
        public int AllocationId { get; set; }

        public int UnloadId { get; set; }

        public int WorkerId { get; set; }

        public string WorkerName { get; set; } = string.Empty;

        public string WorkType { get; set; } = string.Empty;

        public int? BagTypeId { get; set; }

        public int BagCount { get; set; }

        public decimal PerBagCharge { get; set; }

        public decimal PalledariAmount { get; set; }

        public DateTime AllocationTime { get; set; }

        public decimal RowTotal =>
            BagCount * PerBagCharge;
    }
}