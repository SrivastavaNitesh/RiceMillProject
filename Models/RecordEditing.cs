using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RiceMillProject.Models;

public class RecordEditRow
{
    public string ModuleKey { get; set; } = "";
    public string RecordId { get; set; } = "";
    public string ReferenceNo { get; set; } = "";
    public string VehicleNumber { get; set; } = "";
    public string PartyName { get; set; } = "";
    public string DriverName { get; set; } = "";
    public DateTime EntryAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool CanEdit { get; set; }
    public bool IsFinalized { get; set; }
    public bool OutwardComplete { get; set; }
    public string Version { get; set; } = "";
    public int PolicyId { get; set; }
    public string PolicyName { get; set; } = "";
    public bool IsAdmin { get; set; }
    public string LockedReason => IsFinalized && !IsAdmin ? "Finalized — admin only" : OutwardComplete && ModuleKey == "Weight" && !IsAdmin ? "Outward completed" : "Edit time expired or policy disabled";
}

public class RecordEditForm
{
    [Required] public string ModuleKey { get; set; } = "";
    [Required, StringLength(100)] public string RecordId { get; set; } = "";
    [Required] public string Version { get; set; } = "";
    [Required, StringLength(500)] public string Reason { get; set; } = "";
    public Dictionary<string, string?> Values { get; set; } = new();
    public List<Dictionary<string, string?>> Details { get; set; } = [];
    [ValidateNever] public string InwardNo { get; set; } = "";
    [ValidateNever] public bool HasRecordedTare { get; set; }
    [ValidateNever] public decimal? NetWeight { get; set; }
    [ValidateNever] public RecordEditRow Access { get; set; } = new();
    [ValidateNever] public List<EditField> Fields { get; set; } = [];
    [ValidateNever] public List<EditField> DetailFields { get; set; } = [];
}

public record EditField(string Name, string Label, string Type = "text", bool Required = false, int MaxLength = 150)
{
    public List<SelectListItem> Options { get; set; } = [];
}

public class EditPolicyForm
{
    public int PolicyId { get; set; }
    [Required, StringLength(100)] public string PolicyName { get; set; } = "";
    [Range(1, 5256000)] public int? WindowMinutes { get; set; }
    public bool StopAtOutward { get; set; }
    public bool IsActive { get; set; }
    [Required] public string Version { get; set; } = "";
}
