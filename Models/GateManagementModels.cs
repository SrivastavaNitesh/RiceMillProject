namespace RiceMillProject.Models
{
    public class GateMaster
    {
        public int GateId { get; set; }
        public string GateCode { get; set; } = string.Empty;
        public string GateName { get; set; } = string.Empty;
        public string GateType { get; set; } = "Main Gate"; // Main Gate, Dispatch Gate, Staff Gate, Gate-1, etc.
        public string? LocationArea { get; set; }
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public bool InwardAllowed { get; set; } = true;
        public bool OutwardAllowed { get; set; } = true;
        public bool IsActive { get; set; } = true;
    }

    public class DepartmentMaster
    {
        public int DepartmentId { get; set; }
        public string DepartmentCode { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public int? OfficeId { get; set; }
        public string? OfficeName { get; set; }
        public int? ResponsibleOfficerId { get; set; }
        public string? ResponsibleOfficerName { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class VisitorRegister
    {
        public int VisitorId { get; set; }
        public string VisitorNo { get; set; } = string.Empty;
        public int GateId { get; set; }
        public string GateName { get; set; } = string.Empty;
        public DateTime VisitDateTime { get; set; } = DateTime.Now;
        public string VisitorName { get; set; } = string.Empty;
        public string MobileNo { get; set; } = string.Empty;
        public string? CompanyOrg { get; set; }
        public string PurposeOfVisit { get; set; } = "Official";
        public int? PersonToMeetId { get; set; }
        public string? PersonToMeetName { get; set; }
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public string? VisitorIDType { get; set; }
        public string? IDReference { get; set; }
        public string? VehicleNo { get; set; }
        public string? VisitorPassNo { get; set; }
        public DateTime? ExitDateTime { get; set; }
        public string PassStatus { get; set; } = "Active";
        public string? Remarks { get; set; }
    }

    public class TempMaterialRegister
    {
        public int TempMaterialId { get; set; }
        public string EntryNo { get; set; } = string.Empty;
        public string? OutwardNo { get; set; }
        public int GateId { get; set; }
        public string? GateName { get; set; }
        public DateTime EntryDateTime { get; set; } = DateTime.Now;
        public string OwnerVendor { get; set; } = string.Empty;
        public string? VehicleNo { get; set; }
        public string ItemCategory { get; set; } = "Tool";
        public string ItemDescription { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "Nos";
        public string? SerialAssetNo { get; set; }
        public string Purpose { get; set; } = "Repair";
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public DateTime? ExpectedReturnDate { get; set; }
        public DateTime? ReturnDateTime { get; set; }
        public string? GatePassNo { get; set; }
        public string Status { get; set; } = "In Yard";
        public string? Remarks { get; set; }
    }

    public class SecurityIncident
    {
        public int IncidentId { get; set; }
        public string IncidentNo { get; set; } = string.Empty;
        public int GateId { get; set; }
        public string? GateName { get; set; }
        public DateTime IncidentDateTime { get; set; } = DateTime.Now;
        public string IncidentType { get; set; } = "Document Mismatch";
        public string? RelatedEntryNo { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? ActionTaken { get; set; }
        public int? ReportedToPersonId { get; set; }
        public string? ReportedToPersonName { get; set; }
        public string Severity { get; set; } = "Medium";
        public string Status { get; set; } = "Open";
    }
}
