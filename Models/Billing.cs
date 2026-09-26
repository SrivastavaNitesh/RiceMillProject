using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace RiceMillProject.Models;

public class BillingRst
{
    public string RSTNumber { get; set; } = "";
    public string PartyName { get; set; } = "";
    public string PartyMobile { get; set; } = "";
    public string DriverName { get; set; } = "";
    public string DriverMobile { get; set; } = "";
    public string VehicleNumber { get; set; } = "";
    public string InwardNo { get; set; } = "";
    public decimal GrossWeight { get; set; }
    public decimal? TareWeight { get; set; }
    public decimal? NetWeight { get; set; }
    public DateTime? GeneratedAt { get; set; }
    public string GateStatus { get; set; } = "";
    public string SupervisorName { get; set; } = "";
    public bool LabComplete { get; set; }
    public int ItemCount { get; set; }
    public int RequiredTests { get; set; }
    public int CompletedTests { get; set; }
    public int? BillId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public string Status => BillId.HasValue ? (PaidAmount >= TotalAmount ? "Paid" : PaidAmount > 0 ? "Part paid" : "Billed") : LabComplete ? "Ready to bill" : ItemCount > 0 ? "Lab pending" : GateStatus;
}
public class BillingLot
{
    public int DetailId { get; set; }
    public int UnloadId { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public int ItemId { get; set; }
    public string ItemName { get; set; } = "";
    public int BagTypeId { get; set; }
    public string BagTypeName { get; set; } = "";
    public int DeductionWeightGrams { get; set; }
    public int BagCount { get; set; }
    public string SupervisorName { get; set; } = "";
    public int SupervisorTotalBags { get; set; }
    public int MethTotalBags { get; set; }
}
public class BillingLocation
{
    public int UnloadId { get; set; }
    public int LocationId { get; set; }
    public string LocationName { get; set; } = "";
}
public class BillingWorker
{
    public int AllocationId { get; set; }
    public int UnloadId { get; set; }
    public int WorkerId { get; set; }
    public string WorkerName { get; set; } = "";
    public string WorkType { get; set; } = "";
    public int? BagTypeId { get; set; }
    public string BagTypeName { get; set; } = "";
    public int BagCount { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
}
public class BillingLabResult
{
    public int CategoryId { get; set; }
    public int ItemId { get; set; }
    public int TestId { get; set; }
    public string TestName { get; set; } = "";
    public string Unit { get; set; } = "";
    public decimal? MasterDeduction { get; set; }
    public string MasterUnit { get; set; } = "";
    public string? ResultValue { get; set; }
    public int? ReportId { get; set; }
    public DateTime? TestedAt { get; set; }
    public string RecordedBy { get; set; } = "";
    public int? RuleId { get; set; }
    public decimal Threshold { get; set; }
    public string DeductionMode { get; set; } = "";
    public decimal DeductionValue { get; set; }
}
public class BillingSource
{
    public BillingRst Header { get; set; } = new();
    public List<BillingLot> Lots { get; set; } = [];
    public List<BillingLocation> Locations { get; set; } = [];
    public List<BillingWorker> Workers { get; set; } = [];
    public List<BillingLabResult> LabResults { get; set; } = [];
    public List<string> UnloadStatuses { get; set; } = [];
}
public class BillingItemInput
{
    public int ItemId { get; set; }
    [Range(typeof(decimal),"0.01","99999999.99")] public decimal Rate { get; set; }
    [RegularExpression("Quintal|Kg|Bag")] public string RateBasis { get; set; } = "Quintal";
    public decimal? MeasuredWeight { get; set; }
}
public class BillingWorkerInput
{
    public int AllocationId { get; set; }
    [Range(typeof(decimal),"0","999999.99")] public decimal Rate { get; set; }
}
public class BillingAllocationInput
{
    public int DetailId { get; set; }
    public int LocationId { get; set; }
    [Range(0,int.MaxValue)] public int BagCount { get; set; }
}
public class BillingForm
{
    [Required,StringLength(50)] public string RSTNumber { get; set; } = "";
    public Guid SubmissionId { get; set; } = Guid.NewGuid();
    [Required] public string SourceVersion { get; set; } = "";
    [RegularExpression("ByBags|Measured")] public string WeightMode { get; set; } = "ByBags";
    public List<BillingItemInput> Items { get; set; } = [];
    public List<BillingWorkerInput> Workers { get; set; } = [];
    public List<BillingAllocationInput> Allocations { get; set; } = [];
    [Range(typeof(decimal),"0","100")] public decimal GstPercent { get; set; }
    [StringLength(1000)] public string? Notes { get; set; }
}
public class BillingItemLine
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public int ItemId { get; set; }
    public string ItemName { get; set; } = "";
    public int Bags { get; set; }
    public decimal GrossKg { get; set; }
    public decimal BagDeductionKg { get; set; }
    public decimal LabDeductionKg { get; set; }
    public decimal PayableKg { get; set; }
    public decimal Rate { get; set; }
    public string RateBasis { get; set; } = "Quintal";
    public decimal BaseAmount { get; set; }
    public decimal LabDeductionAmount { get; set; }
    public decimal Amount { get; set; }
    public List<BillingLocationQuantity> Locations { get; set; } = [];
    public List<string> DeductionNotes { get; set; } = [];
}
public class BillingLocationQuantity
{
    public int LocationId { get; set; }
    public string LocationName { get; set; } = "";
    public int Bags { get; set; }
    public decimal WeightKg { get; set; }
}
public class BillingCalculation
{
    public BillingSource Source { get; set; } = new();
    public string WeightMode { get; set; } = "ByBags";
    public List<BillingItemLine> Items { get; set; } = [];
    public List<BillingWorker> Workers { get; set; } = [];
    public decimal ItemAmount { get; set; }
    public decimal LabDeduction { get; set; }
    public decimal WorkerAmount { get; set; }
    public decimal GstPercent { get; set; }
    public decimal GstAmount { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public string? Notes { get; set; }
    public List<string> Issues { get; set; } = [];
}
public class BillingPage
{
    public BillingForm Form { get; set; } = new();
    [ValidateNever] public BillingCalculation Calculation { get; set; } = new();
}
public class BillingPaymentInput
{
    public int BillId { get; set; }
    public Guid SubmissionId { get; set; } = Guid.NewGuid();
    [Range(typeof(decimal),"0.01","999999999999.99")] public decimal Amount { get; set; }
    [Required,RegularExpression("Cash|Bank transfer|UPI|Cheque")] public string Method { get; set; } = "Cash";
    [StringLength(100)] public string? Reference { get; set; }
}
public class BillingPayment
{
    public int PaymentId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = "";
    public string Reference { get; set; } = "";
    public DateTime PaidAt { get; set; }
}
public class BillingDocument
{
    public int BillId { get; set; }
    public DateTime CreatedAt { get; set; }
    public BillingCalculation Calculation { get; set; } = new();
    public List<BillingPayment> Payments { get; set; } = [];
    public decimal Paid => Payments.Sum(p => p.Amount);
    public decimal Balance => Calculation.Total - Paid;
    public string Status => Balance <= 0 ? "Paid" : Paid > 0 ? "Part paid" : "Unpaid";
}
public class BillingRule
{
    public int CategoryId { get; set; }
    public int TestId { get; set; }
    [ValidateNever] public string CategoryName { get; set; } = "";
    [ValidateNever] public string TestName { get; set; } = "";
    [ValidateNever] public string MasterReference { get; set; } = "";
    public decimal? MasterDeduction { get; set; }
    [ValidateNever] public string MasterUnit { get; set; } = "";
    public bool Configured { get; set; }
    [Range(typeof(decimal),"0","999999999")] public decimal Threshold { get; set; }
    public string DeductionMode { get; set; } = "None";
    [Range(typeof(decimal),"0","999999")] public decimal DeductionValue { get; set; }
}
