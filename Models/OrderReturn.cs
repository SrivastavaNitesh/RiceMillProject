using System.ComponentModel.DataAnnotations;

namespace RiceMillProject.Models;

public class OrderReturnPage
{
    public List<OrderReturnSummary> Returns { get; set; } = [];
    public List<OrderReturnSource> EligibleRsts { get; set; } = [];
    public List<LookupOption> Locations { get; set; } = [];
    public List<LookupOption> Vehicles { get; set; } = [];
    public List<LookupOption> BagTypes { get; set; } = [];
    public List<LookupOption> Workers { get; set; } = [];
}

public class OrderReturnDetails : OrderReturnSummary
{
    public List<OrderReturnDispatch> Dispatches { get; set; } = [];
    public List<OrderReturnMovement> Movements { get; set; } = [];
    public List<OrderReturnHistory> History { get; set; } = [];
}

public class OrderReturnSummary
{
    public int OrderReturnId { get; set; }
    public string ReturnNumber { get; set; } = "";
    public int UnloadId { get; set; }
    public string RSTNumber { get; set; } = "";
    public string VehicleNumber { get; set; } = "";
    public string PartyName { get; set; } = "";
    public string SourceLocationName { get; set; } = "";
    public int RequestedBags { get; set; }
    public int ApprovedBags { get; set; }
    public int DispatchedBags { get; set; }
    public int RemainingBags => Math.Max(0, ApprovedBags - DispatchedBags);
    public string ReturnReason { get; set; } = "";
    public string QualityRemarks { get; set; } = "";
    public string QualityDecision { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime RequestedAt { get; set; }
}

public class OrderReturnSource
{
    public int UnloadId { get; set; }
    public string RSTNumber { get; set; } = "";
    public string VehicleNumber { get; set; } = "";
    public string PartyName { get; set; } = "";
    public int AvailableBags { get; set; }
    public int ReturnBags { get; set; }
    public int OrderReturnId { get; set; }
    public string ReturnNumber { get; set; } = "";
    public string ReturnStatus { get; set; } = "";
    public string StageCode { get; set; } = "";
    public string StageName { get; set; } = "";
    public DateTime LastUpdatedAt { get; set; }
    public bool CanCreateReturn { get; set; }
    public string ActionMessage { get; set; } = "";
}

public class LookupOption
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class OrderReturnDispatch
{
    public int DispatchId { get; set; }
    public string DispatchNumber { get; set; } = "";
    public int DispatchBags { get; set; }
    public int? VehicleId { get; set; }
    public string VehicleNumber { get; set; } = "";
    public decimal? GrossWeight { get; set; }
    public decimal? TareWeight { get; set; }
    public decimal? NetWeight { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class OrderReturnMovement
{
    public int MovementId { get; set; }
    public int? DispatchId { get; set; }
    public string MovementType { get; set; } = "";
    public string FromLocation { get; set; } = "";
    public string ToLocation { get; set; } = "";
    public string BagType { get; set; } = "";
    public int Bags { get; set; }
    public decimal Amount { get; set; }
    public DateTime MovementAt { get; set; }
}

public class OrderReturnHistory
{
    public string NewStatus { get; set; } = "";
    public string Remarks { get; set; } = "";
    public string ChangedBy { get; set; } = "";
    public DateTime ChangedAt { get; set; }
}

public class CreateOrderReturnInput
{
    [Range(1, int.MaxValue)] public int UnloadId { get; set; }
    [Range(1, int.MaxValue)] public int RequestedBags { get; set; }
    public int? SourceLocationId { get; set; }
    [Required, StringLength(500)] public string Reason { get; set; } = "";
}

public class DecideOrderReturnInput
{
    public int OrderReturnId { get; set; }
    [Required] public string Decision { get; set; } = "";
    public int? ApprovedBags { get; set; }
    [StringLength(1000)] public string? Remarks { get; set; }
}

public class CreateReturnDispatchInput
{
    public int OrderReturnId { get; set; }
    [Range(1, int.MaxValue)] public int DispatchBags { get; set; }
    public int? VehicleId { get; set; }
    [StringLength(500)] public string? Remarks { get; set; }
}

public class SaveReturnMovementInput
{
    public int OrderReturnId { get; set; }
    public int? DispatchId { get; set; }
    [Required] public string MovementType { get; set; } = "";
    public int? FromLocationId { get; set; }
    public int? ToLocationId { get; set; }
    [Range(1, int.MaxValue)] public int BagTypeId { get; set; }
    [Range(1, int.MaxValue)] public int Bags { get; set; }
    [MinLength(1)] public List<ReturnWorkerInput> Workers { get; set; } = [];
    [StringLength(500)] public string? Remarks { get; set; }
}

public class ReturnWorkerInput
{
    [Range(1, int.MaxValue)] public int WorkerId { get; set; }
    [Range(1, int.MaxValue)] public int BagsHandled { get; set; }
    [Range(0, double.MaxValue)] public decimal PerBagRate { get; set; }
}

public class SaveReturnWeightInput
{
    public int DispatchId { get; set; }
    [Range(0.001, double.MaxValue)] public decimal GrossWeight { get; set; }
    [Range(0, double.MaxValue)] public decimal TareWeight { get; set; }
    [Range(1, int.MaxValue)] public int VehicleId { get; set; }
}
