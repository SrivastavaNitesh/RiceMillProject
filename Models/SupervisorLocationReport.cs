using System.Data;
namespace RiceMillProject.Models;
public class SupervisorLocationReport
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int? CategoryId { get; set; }
    public List<string> Notices { get; set; } = new();
    public int? LocationId { get; set; }
    public string? Status { get; set; }
    public string? Search { get; set; }
    public string WeightText(List<DataRow> rows) => rows.Count==0 ? "0.000" : rows.Any(r=>r.IsNull("WeightKg")) ? "Pending" : rows.Sum(r=>r.Field<decimal>("WeightKg")).ToString("N3")+(rows.Any(r=>r.Field<bool>("Estimated"))?" *":"");
    public decimal BagsAt(int locationId) => Entries.AsEnumerable().Where(e=>e.Field<int>("ReportLocationId")==locationId).Sum(e=>{
        var id=e.Field<int>("UnloadId");var saved=LocationBags.AsEnumerable().Where(r=>r.Field<int>("UnloadId")==id&&r.Field<int>("LocationId")==locationId).ToList();
        return saved.Count>0?saved.Sum(r=>Convert.ToDecimal(r["BagCount"])):Materials.AsEnumerable().Where(r=>r.Field<int>("UnloadId")==id&&r.Field<int?>("LocationId")==locationId).Sum(r=>Convert.ToDecimal(r["BagCount"]));
    });
    public DataTable Locations { get; set; } = new();
    public DataTable Entries { get; set; } = new();
    public DataTable Materials { get; set; } = new();
    public DataTable LocationBags { get; set; } = new();
    public DataTable Workers { get; set; } = new();
}
