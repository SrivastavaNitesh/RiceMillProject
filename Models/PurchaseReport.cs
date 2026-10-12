using System.ComponentModel.DataAnnotations;

namespace RiceMillProject.Models;

public class PurchaseFilter
{
    public int? Year { get; set; }
    [Range(1, 12)] public int? Month { get; set; }
    [DataType(DataType.Date)] public DateTime? Date { get; set; }
    [DataType(DataType.Date)] public DateTime? From { get; set; }
    [DataType(DataType.Date)] public DateTime? To { get; set; }
    public int? CompanyId { get; set; }
    public int? PartyId { get; set; }
    public int? LocationId { get; set; }
    public string Mode { get; set; } = "Monthly";
}

public record PurchaseOption(int Id, string Name);
public record PurchaseColumn(int LocationId, string LocationName, int BagTypeId, string BagTypeName);
public class PurchaseLot : BillingLot
{
    public string RSTNumber { get; set; } = "";
    public string LocationName { get; set; } = "Unassigned";
    public string MethName { get; set; } = "";
    public decimal? WeightKg { get; set; }
    public bool Estimated { get; set; }
}
public class PurchaseHeader : BillingRst
{
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = "Unassigned company";
    public int PartyId { get; set; }
    public DateTime WeighingDate { get; set; }
    public List<PurchaseItem> Items { get; set; } = [];
}
public class PurchaseLab : BillingLabResult
{
    public string RSTNumber { get; set; } = "";
}
public class PurchaseItem
{
    public int ItemId { get; set; }
    public int CategoryId { get; set; }
    public string ItemName { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public List<PurchaseLot> Lots { get; set; } = [];
    public int Bags => Lots.Sum(x => x.BagCount);
    public decimal? Weight => Lots.Any(x => !x.WeightKg.HasValue) ? null : Lots.Sum(x => x.WeightKg);
    public bool Estimated => Lots.Any(x => x.Estimated);
    public string Supervisors => Names(Lots.Select(x => x.SupervisorName));
    public string Meths => Names(Lots.Select(x => x.MethName));
    public string Technicians { get; set; } = "";
    public string Deduction { get; set; } = "Pending";
    public static string Names(IEnumerable<string> names) => string.Join(", ", names.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().Order());
}
public class PurchasePage
{
    public PurchaseFilter Filter { get; set; } = new();
    public string Title { get; set; } = "Purchase Reports";
    public List<PurchaseHeader> Headers { get; set; } = [];
    public List<PurchaseOption> Companies { get; set; } = [];
    public List<PurchaseOption> Parties { get; set; } = [];
    public List<LocationMaster> Locations { get; set; } = [];
    public List<PurchaseOption> BagTypes { get; set; } = [];
    public List<PurchaseColumn> Columns { get; set; } = [];
    public List<string> Notices { get; set; } = [];
    public IEnumerable<PurchaseLot> Lots => Headers.SelectMany(h => h.Items).SelectMany(i => i.Lots);
    public static string Kg(decimal? value, bool estimated = false) => value.HasValue ? value.Value.ToString("N3") + (estimated ? " *" : "") : "Pending";
}
