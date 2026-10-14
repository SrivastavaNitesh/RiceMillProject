namespace RiceMillProject.Models;

public class DashboardStats
{
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = "Company";
    public DateTime AsOf { get; set; } = DateTime.Now;
    public DateTime FromDate { get; set; } = DateTime.Today.AddDays(-29);
    public DateTime ToDate { get; set; } = DateTime.Today;
    public int TodayInwardEntries { get; set; }
    public int PendingWeighmentRst { get; set; }
    public int ActiveRst { get; set; }
    public int FinalRst { get; set; }
    public int TodayTotalBags { get; set; }
    public decimal TodayTotalWeight { get; set; }
    public int PendingUnload { get; set; }
    public int PendingLab { get; set; }
    public int PendingBilling { get; set; }
    public List<DashboardChartPoint> RstStatuses { get; set; } = [];
    public List<DashboardCategoryBreakdown> CategoryBreakdown { get; set; } = [];
    public List<DashboardLocationPoint> LocationSummary { get; set; } = [];
    public List<DashboardLocationDetail> LocationDetails { get; set; } = [];
    public List<DashboardPendingDetail> PendingDetails { get; set; } = [];
    public List<DashboardSupplierPerformance> SupplierPerformance { get; set; } = [];
    public List<DashboardRstJourney> RstJourneys { get; set; } = [];
}

public class DashboardKpi
{
    public string Title { get; set; } = "";
    public string Value { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Tone { get; set; } = "blue";
    public string? DrilldownStage { get; set; }
}

public class DashboardChartPoint
{
    public string Name { get; set; } = "";
    public decimal Value { get; set; }
}

public class DashboardCategoryBreakdown
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public int ItemId { get; set; }
    public string ItemName { get; set; } = "";
    public int BagTypeId { get; set; }
    public string BagTypeName { get; set; } = "";
    public int Bags { get; set; }
}

public class DashboardLocationPoint
{
    public int LocationId { get; set; }
    public string LocationName { get; set; } = "";
    public int Bags { get; set; }
    public decimal WeightKg { get; set; }
}

public class DashboardPendingDetail
{
    public string Stage { get; set; } = "";
    public string RSTNumber { get; set; } = "";
    public string VehicleNumber { get; set; } = "";
    public string PartyName { get; set; } = "";
    public DateTime Since { get; set; }
    public decimal AgeHours { get; set; }
}

public class DashboardSupplierPerformance
{
    public string SupplierName { get; set; } = "";
    public int Vehicles { get; set; }
    public int Bags { get; set; }
    public decimal WeightKg { get; set; }
    public decimal AverageWeightKg { get; set; }
}

public class DashboardRstJourney
{
    public string RSTNumber { get; set; } = "";
    public string VehicleNumber { get; set; } = "";
    public string PartyName { get; set; } = "";
    public string CurrentStage { get; set; } = "";
    public string CurrentStatus { get; set; } = "";
    public DateTime LastActivity { get; set; }
    public decimal AgeHours { get; set; }
}


public class DashboardLocationDetail
{
    public int LocationId { get; set; }
    public string LocationName { get; set; } = "";
    public string RSTNumber { get; set; } = "";
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public int ItemId { get; set; }
    public string VarietyName { get; set; } = "";
    public int BagTypeId { get; set; }
    public string BagTypeName { get; set; } = "";
    public int Bags { get; set; }
    public decimal WeightKg { get; set; }
    public DateTime? UnloadTime { get; set; }
}
