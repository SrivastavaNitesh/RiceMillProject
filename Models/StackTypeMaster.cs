using System;

namespace RiceMillProject.Models
{
    public class StackTypeMaster
    {
        public int StackTypeId { get; set; }

        public string StackTypeName { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}