namespace RiceMillProject.Models;

public class MethPaymentReport
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? SupervisorId { get; set; }
    public int? MethId { get; set; }
    public string Search { get; set; } = "";
    public bool IsAdminView { get; set; }
    public List<LookupOption> Supervisors { get; set; } = [];
    public List<LookupOption> Meths { get; set; } = [];
    public List<MethPaymentReportRow> Rows { get; set; } = [];
    public int UnloadingBags => Rows.Sum(x => x.UnloadingBags);
    public int ReturnBags => Rows.Sum(x => x.ReturnBags);
    public int FinalBags => Rows.Sum(x => x.FinalBags);
    public decimal TotalPayable => Rows.Sum(x => x.TotalPayable);
}

public class MethPaymentReportRow
{
    public int UnloadId { get; set; }
    public string RSTNumber { get; set; } = "";
    public int SupervisorId { get; set; }
    public string SupervisorName { get; set; } = "";
    public int MethId { get; set; }
    public string MethName { get; set; } = "";
    public DateTime WorkDate { get; set; }
    public int UnloadingBags { get; set; }
    public int ReturnBags { get; set; }
    public int FinalBags => Math.Max(0, UnloadingBags - ReturnBags);
    public decimal UnloadingAmount { get; set; }
    public decimal ReturnLoadingAmount { get; set; }
    public decimal TotalPayable => UnloadingAmount + ReturnLoadingAmount;
    public List<MethPaymentReportDetail> Details { get; set; } = [];
}

public class MethPaymentReportDetail
{
    public string WorkType { get; set; } = "";
    public string BagTypeName { get; set; } = "";
    public int BagCount { get; set; }
    public decimal PerBagPrice { get; set; }
    public decimal Amount { get; set; }
}
