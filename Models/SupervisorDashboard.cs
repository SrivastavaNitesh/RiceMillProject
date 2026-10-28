namespace RiceMillProject.Models;

public class SupervisorDashboard
{
    public string SupervisorName { get; set; } = "Supervisor";
    public string CompanyName { get; set; } = "Company";
    public int PendingAssignments { get; set; }
    public int WorkerPending { get; set; }
    public int FieldPending { get; set; }
    public int TodayCompleted { get; set; }
    public int TotalAssigned { get; set; }
    public int TotalRecordedBags { get; set; }
    public List<SupervisorChartPoint> Statuses { get; set; } = [];
    public List<SupervisorTrendPoint> DailyTrend { get; set; } = [];
    public List<SupervisorLocationDetail> LocationDetails { get; set; } = [];
    public List<SupervisorChartPoint> BagTypes { get; set; } = [];
    public List<SupervisorWorkerDetail> WorkerDetails { get; set; } = [];
    public List<SupervisorLiveWork> LiveWork { get; set; } = [];
}

public class SupervisorLocationDetail
{
    public int LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int BagTypeId { get; set; }
    public string BagTypeName { get; set; } = string.Empty;
    public int Bags { get; set; }
}

public class SupervisorWorkerDetail
{
    public int WorkerId { get; set; }
    public string WorkerName { get; set; } = string.Empty;
    public string WorkType { get; set; } = string.Empty;
    public int BagTypeId { get; set; }
    public string BagTypeName { get; set; } = string.Empty;
    public int Bags { get; set; }
}

public class SupervisorChartPoint
{
    public string Name { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

public class SupervisorTrendPoint
{
    public string DateLabel { get; set; } = string.Empty;
    public int Completed { get; set; }
    public int Bags { get; set; }
}

public class SupervisorLiveWork
{
    public int UnloadId { get; set; }
    public string RSTNumber { get; set; } = string.Empty;
    public string VehicleNumber { get; set; } = string.Empty;
    public string PartyName { get; set; } = string.Empty;
    public string MethName { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string CurrentStage { get; set; } = string.Empty;
    public int Bags { get; set; }
    public DateTime? LastActivity { get; set; }
}
