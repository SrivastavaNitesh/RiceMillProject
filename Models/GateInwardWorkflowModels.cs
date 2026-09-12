using System;
using System.ComponentModel.DataAnnotations;

namespace RiceMillProject.Models
{
    public class InwardTypeMaster
    {
        public int InwardTypeId { get; set; }
        public string InwardTypeName { get; set; } = string.Empty;
        public bool RSTRequired { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class VehicleTypeMaster
    {
        public int VehicleTypeId { get; set; }
        public string VehicleTypeName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class InwardHeader
    {
        public int InwardId { get; set; }
        public string? GateEntryNo { get; set; }
        public string? InwardNo { get; set; }

        [DataType(DataType.Date)]
        public DateTime InwardDate { get; set; } = DateTime.Now;
        public string? InwardTime { get; set; } = DateTime.Now.ToString("hh:mm tt");

        [Required(ErrorMessage = "Gate selection is required.")]
        public int GateId { get; set; }
        public string? GateName { get; set; }

        [Required(ErrorMessage = "Inward Type selection is required.")]
        public int InwardTypeId { get; set; }
        public string? InwardTypeName { get; set; }
        public bool RSTRequired { get; set; }

        [Required(ErrorMessage = "Party / Supplier selection is required.")]
        public int PartyId { get; set; }
        public string? PartyName { get; set; }

        public string? TransporterName { get; set; }

        [Required(ErrorMessage = "Vehicle Number is required.")]
        public string VehicleNo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vehicle Type is required.")]
        public int VehicleTypeId { get; set; }
        public string? VehicleTypeName { get; set; }

        [Required(ErrorMessage = "Driver Name is required.")]
        public string DriverName { get; set; } = string.Empty;

        public string? DriverMobile { get; set; }
        public string? ChallanNo { get; set; }
        public int ApproxNoOfBags { get; set; }
        public decimal ApproxWeight { get; set; }
        public string? PurposeRemarks { get; set; }
        public DateTime GateInDateTime { get; set; } = DateTime.Now;
        public string? GateManName { get; set; } = "GateMan1 (System)";
        public string StatusName { get; set; } = "Pending";
        public bool IsRSTGenerated { get; set; }
    }
}
