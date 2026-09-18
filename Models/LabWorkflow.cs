using System.ComponentModel.DataAnnotations;

namespace RiceMillProject.Models;

public class LabTestMaster
{
    public int TestId { get; set; }
    [Required, StringLength(120)] public string TestName { get; set; } = "";
    [StringLength(30)] public string? Unit { get; set; }
    [RegularExpression("Number|Text")] public string ResultType { get; set; } = "Number";
    [StringLength(500)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class LabItemTestMapping
{
    public int MappingId { get; set; }
    [Range(1, int.MaxValue)] public int CategoryId { get; set; }
    [Range(1, int.MaxValue)] public int ItemId { get; set; }
    [Range(1, int.MaxValue)] public int TestId { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever] public string CategoryName { get; set; } = "";
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever] public string ItemName { get; set; } = "";
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever] public string TestName { get; set; } = "";
}

public class LabItemOption
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public int ItemId { get; set; }
    public string ItemName { get; set; } = "";
    public List<LabTestMaster> Tests { get; set; } = [];
}

public class LabRstSummary
{
    public string RSTNumber { get; set; } = "";
    public string VehicleNumber { get; set; } = "";
    public string PartyName { get; set; } = "";
    public int ItemCount { get; set; }
    public int RequiredTests { get; set; }
    public int CompletedTests { get; set; }
    public int UnmappedItems { get; set; }
}

public class LabResultInput
{
    public int CategoryId { get; set; }
    public int ItemId { get; set; }
    public int TestId { get; set; }
    [Required, StringLength(500)] public string Value { get; set; } = "";
}

public class LabEntryViewModel
{
    [Required, StringLength(50)] public string RSTNumber { get; set; } = "";
    public Guid SubmissionId { get; set; } = Guid.NewGuid();
    public List<int> SelectedCategoryIds { get; set; } = [];
    public List<int> SelectedItemIds { get; set; } = [];
    public List<LabResultInput> Results { get; set; } = [];
    [StringLength(1000)] public string? Remarks { get; set; }
    public List<LabItemOption> AvailableItems { get; set; } = [];
}

public class LabTestMasterPage
{
    public LabTestMaster Form { get; set; } = new();
    public List<LabTestMaster> Tests { get; set; } = [];
}

public class LabMappingPage
{
    public LabItemTestMapping Form { get; set; } = new();
    public List<LabItemTestMapping> Mappings { get; set; } = [];
    public List<LabItemOption> Items { get; set; } = [];
    public List<LabTestMaster> Tests { get; set; } = [];
}

public class LabReport
{
    public int ReportId { get; set; }
    public string RSTNumber { get; set; } = "";
    public string TechnicianName { get; set; } = "";
    public DateTime TestedAt { get; set; }
    public string? Remarks { get; set; }
    public int ItemCount { get; set; }
    public int ResultCount { get; set; }
    public List<LabReportResult> Results { get; set; } = [];
}

public class LabReportResult
{
    public int CategoryId { get; set; }
    public int ItemId { get; set; }
    public string CategoryName { get; set; } = "";
    public string ItemName { get; set; } = "";
    public string TestName { get; set; } = "";
    public string Unit { get; set; } = "";
    public string Value { get; set; } = "";
}

public class LabReportsPage
{
    public string? RstNumber { get; set; }
    [DataType(DataType.Date)] public DateTime? From { get; set; }
    [DataType(DataType.Date)] public DateTime? To { get; set; }
    public List<LabReport> Reports { get; set; } = [];
}
