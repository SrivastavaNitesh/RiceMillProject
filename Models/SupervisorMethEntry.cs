using System.ComponentModel.DataAnnotations;

namespace RiceMillProject.Models
{
    public class SupervisorMethEntryViewModel
    {
        public int UnloadId { get; set; }
        public string RSTNumber { get; set; } = string.Empty;
        public string VehicleNumber { get; set; } = string.Empty;
        public string PartyName { get; set; } = string.Empty;
        public string MethName { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public DateTime? UnloadTime { get; set; }
        public List<SupervisorMethEntryRow> Rows { get; set; } = [];

        [Required(ErrorMessage = "Loose bag count is required. Enter 0 when there are no loose bags.")]
        [Range(0, int.MaxValue, ErrorMessage = "Loose bag count cannot be negative.")]
        public int? LooseBagCount { get; set; }

        public bool HasReturnDuringUnloading { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Return bag count cannot be negative.")]
        public int? ReturnBagCount { get; set; }

        public int TotalBags => Rows.Sum(row => row.BagCount);
        public decimal TotalAmount => Rows.Sum(row => row.TotalAmount);
    }

    public class SupervisorMethExtraCounts
    {
        public int? LooseBagCount { get; set; }
        public int? ReturnBagCount { get; set; }
    }

    public class SupervisorMethEntryRow
    {
        public int EntryId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Bag type is required.")]
        public int BagTypeId { get; set; }

        public string BagTypeName { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Bag count must be greater than zero.")]
        public int BagCount { get; set; }

        [Range(typeof(decimal), "0", "999999999.99", ErrorMessage = "Per bag price cannot be negative.")]
        public decimal PerBagPrice { get; set; }

        public decimal TotalAmount => BagCount * PerBagPrice;
    }
}
