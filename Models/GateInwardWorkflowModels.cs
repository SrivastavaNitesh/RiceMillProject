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

        public int GateId { get; set; } = 1;
        public string? GateName { get; set; }

        [Required(ErrorMessage = "Inward Type selection is required.")]
        public int InwardTypeId { get; set; } = 1; // 1 = With RST, 2 = Without RST
        public string? InwardTypeName { get; set; }
        public bool RSTRequired { get; set; } = true;

        public int? PartyId { get; set; }

        [Required(ErrorMessage = "Party / Supplier name is required.")]
        public string? PartyName { get; set; }

        public string? TransporterName { get; set; }

        [Required(ErrorMessage = "Vehicle Number is required.")]
        public string VehicleNo { get; set; } = string.Empty;

        public int VehicleTypeId { get; set; } = 1;
        public string? VehicleTypeName { get; set; }

        [Required(ErrorMessage = "Driver Name is required.")]
        public string DriverName { get; set; } = string.Empty;

        public string? DriverMobile { get; set; }
        public string? ChallanNo { get; set; }
        public int ApproxNoOfBags { get; set; }
        public decimal ApproxWeight { get; set; }
        public string? PurposeRemarks { get; set; }
        public DateTime GateInDateTime { get; set; } = DateTime.Now;
        public int CreatedBy { get; set; } = 1;
        public string? GateManName { get; set; } = "Gateman";
        public string StatusName { get; set; } = "Pending";
        public bool IsRSTGenerated { get; set; }
    }
}
