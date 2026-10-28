namespace RiceMillProject.Models;

public class MethDashboard
{
    public string MethName { get; set; } = "Meth";
    public string CompanyName { get; set; } = "Company";
    public int PendingWork { get; set; }
    public int CompletedWork { get; set; }
    public int TodayHandledBags { get; set; }
    public decimal TotalPayable { get; set; }
    public int AwaitingAssignment { get; set; }
    public List<MethChartPoint> Statuses { get; set; } = [];
    public List<MethTrendPoint> DailyTrend { get; set; } = [];
    public List<MethChartPoint> WorkTypes { get; set; } = [];
    public List<MethAwaitingRst> AwaitingRsts { get; set; } = [];
}
public class MethChartPoint { public string Name { get; set; }=""; public decimal Value { get; set; } }
public class MethTrendPoint { public string DateLabel { get; set; }=""; public int Bags { get; set; } }
public class MethAwaitingRst { public string RSTNumber { get; set; }=""; public string VehicleNumber { get; set; }=""; public string PartyName { get; set; }=""; public DateTime GateEntryTime { get; set; } }
public class MethWorkReport
{
    public string Status { get; set; }="All"; public string Search { get; set; }=""; public DateTime? FromDate { get; set; } public DateTime? ToDate { get; set; }
    public List<UnloadTransaction> Rows { get; set; }=[];
}
