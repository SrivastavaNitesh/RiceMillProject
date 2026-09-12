using System;

namespace RiceMillProject.Models
{
    public class WorkerAllocation
    {
        public int AllocationId { get; set; }
        public int UnloadId { get; set; }
        public int WorkerId { get; set; }
        public decimal PalledariAmount { get; set; }
        public DateTime AllocationTime { get; set; }
        
        public string WorkerName { get; set; } = string.Empty;
    }
}
