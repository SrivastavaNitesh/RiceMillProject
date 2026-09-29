using System.Data;
using System.Reflection;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using RiceMillProject.BAL;
using RiceMillProject.Models;

namespace RiceMillProject.DAL;

public class BillingDAL(IConfiguration configuration)
{
    private readonly string connectionString=configuration.GetConnectionString("DefaultConnection")??"";
    private static SqlParameter P(string name,object? value)=>new(name,value??DBNull.Value);
    private static SqlCommand Cmd(SqlConnection con,SqlTransaction? tx,string text,bool procedure,params SqlParameter[] parameters)
    {
        var cmd=new SqlCommand(text,con,tx){CommandType=procedure?CommandType.StoredProcedure:CommandType.Text};
        cmd.Parameters.AddRange(parameters);return cmd;
    }
    private static DataSet Read(SqlConnection con,SqlTransaction? tx,string text,bool procedure,params SqlParameter[] parameters)
    {
        using var cmd=Cmd(con,tx,text,procedure,parameters);using var adapter=new SqlDataAdapter(cmd);var data=new DataSet();adapter.Fill(data);return data;
    }
    private static List<T> Rows<T>(DataTable table) where T:new()
    {
        var props=typeof(T).GetProperties().Where(p=>p.CanWrite && table.Columns.Contains(p.Name)).ToList();
        return table.AsEnumerable().Select(row=>{
            var value=new T();
            foreach(var prop in props) if(!row.IsNull(prop.Name)) prop.SetValue(value,Convert.ChangeType(row[prop.Name],Nullable.GetUnderlyingType(prop.PropertyType)??prop.PropertyType));
            return value;
        }).ToList();
    }
    public List<BillingRst> List()
    {
        using var con=new SqlConnection(connectionString);con.Open();
        return Rows<BillingRst>(Read(con,null,"sp_BillingRstList",true).Tables[0]);
    }
    private static BillingSource? Source(SqlConnection con,SqlTransaction? tx,string rst)
    {
        var data=Read(con,tx,"sp_BillingSource",true,P("@RSTNumber",rst));
        if(data.Tables[0].Rows.Count==0)return null;
        var source=new BillingSource{Header=Rows<BillingRst>(data.Tables[0])[0],Lots=Rows<BillingLot>(data.Tables[1]),Locations=Rows<BillingLocation>(data.Tables[2]),Workers=Rows<BillingWorker>(data.Tables[3]),LabResults=Rows<BillingLabResult>(data.Tables[4]),UnloadStatuses=data.Tables[5].AsEnumerable().Select(r=>Convert.ToString(r["Status"])??"").ToList()};
        var status=Rows<BillingRst>(Read(con,tx,"sp_BillingRstList",true).Tables[0]).First(r=>string.Equals(r.RSTNumber,rst,StringComparison.OrdinalIgnoreCase));
        source.Header.LabComplete=status.LabComplete;source.Header.ItemCount=status.ItemCount;
        source.Header.RequiredTests=status.RequiredTests;source.Header.CompletedTests=status.CompletedTests;
        source.Header.BillId=status.BillId;source.Header.TotalAmount=status.TotalAmount;source.Header.PaidAmount=status.PaidAmount;
        source.Header.SupervisorName=string.Join(", ",source.Lots.Select(l=>l.SupervisorName).Distinct().OrderBy(n=>n,StringComparer.Ordinal));
        FillReportMetadata(con,tx,source);
        return source;
    }
    public BillingSource? GetSource(string rst)
    {
        using var con=new SqlConnection(connectionString);con.Open();return Source(con,null,rst);
    }
    public int Save(BillingForm form,int userId)
    {
        using var con=new SqlConnection(connectionString);con.Open();using var tx=con.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            using(var gate=Cmd(con,tx,"DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=@Resource,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000; IF @r<0 THROW 50001,'Billing is being updated. Retry shortly.',1;",false,P("@Resource","Billing:"+form.RSTNumber.ToUpperInvariant()))) gate.ExecuteNonQuery();
            using(var existing=Cmd(con,tx,"SELECT BillId FROM t_BillingBill WHERE RSTNumber=@RST",false,P("@RST",form.RSTNumber)))
            {
                var id=existing.ExecuteScalar();if(id!=null){tx.Commit();return Convert.ToInt32(id);}
            }
            var source=Source(con,tx,form.RSTNumber)??throw new ArgumentException("RST not found.");
            if(form.SourceVersion!=BillingCalculator.Version(source))throw new ArgumentException("RST, lab, worker or deduction data changed. Reload the page and review current values before saving.");
            if(form.SubmissionId==Guid.Empty)throw new ArgumentException("Invalid bill submission. Reload the page.");
            var result=BillingCalculator.Calculate(source,form,true);
            if(result.Issues.Count>0)throw new ArgumentException(string.Join(" ",result.Issues));
            using var cmd=Cmd(con,tx,"sp_BillingSave",true,P("@RSTNumber",form.RSTNumber),P("@SubmissionId",form.SubmissionId),P("@SnapshotJson",JsonSerializer.Serialize(result)),
                P("@NetWeight",source.Header.NetWeight),P("@ItemAmount",result.ItemAmount),P("@LabDeduction",result.LabDeduction),
                P("@WorkerAmount",result.WorkerAmount),P("@GstPercent",result.GstPercent),P("@GstAmount",result.GstAmount),P("@TotalAmount",result.Total),P("@UserId",userId));
            var billId=Convert.ToInt32(cmd.ExecuteScalar());tx.Commit();return billId;
        }
        catch {tx.Rollback();throw;}
    }
    public BillingDocument? GetBill(int id)
    {
        using var con=new SqlConnection(connectionString);con.Open();
        var data=Read(con,null,"SELECT BillId,CreatedAt,SnapshotJson FROM t_BillingBill WHERE BillId=@Id; SELECT PaymentId,Amount,Method,Reference,PaidAt FROM t_BillingPayment WHERE BillId=@Id ORDER BY PaymentId;",false,P("@Id",id));
        if(data.Tables[0].Rows.Count==0)return null;
        var row=data.Tables[0].Rows[0];
        var calculation=JsonSerializer.Deserialize<BillingCalculation>(Convert.ToString(row["SnapshotJson"])!)??throw new InvalidOperationException("Invalid bill snapshot.");
        FillReportMetadata(con,null,calculation.Source);
        return new(){BillId=id,CreatedAt=Convert.ToDateTime(row["CreatedAt"]),Calculation=calculation,Payments=Rows<BillingPayment>(data.Tables[1])};
    }
    // Resolve display metadata for older snapshots without changing saved results or amounts.
    private static void FillReportMetadata(SqlConnection con,SqlTransaction? tx,BillingSource source)
    {
        if(!source.LabResults.Any(r=>r.ReportId.HasValue && string.IsNullOrWhiteSpace(r.RecordedBy)))return;
        var reports=Read(con,tx,"SELECT ReportId,TechnicianName,TestedAt FROM t_LabReport WHERE RSTNumber=@RST",false,P("@RST",source.Header.RSTNumber)).Tables[0];
        var lookup=reports.AsEnumerable().ToDictionary(r=>Convert.ToInt32(r["ReportId"]));
        foreach(var lab in source.LabResults)
        {
            if(lab.ReportId.HasValue && lookup.TryGetValue(lab.ReportId.Value,out var report))
            {
                if(string.IsNullOrWhiteSpace(lab.RecordedBy))lab.RecordedBy=Convert.ToString(report["TechnicianName"])??"";
                if(!lab.TestedAt.HasValue && !report.IsNull("TestedAt"))lab.TestedAt=Convert.ToDateTime(report["TestedAt"]);
            }
        }
    }
    public void Pay(BillingPaymentInput input,int userId)
    {
        if(input.SubmissionId==Guid.Empty || BillingCalculator.Money(input.Amount)!=input.Amount)throw new ArgumentException("Enter a valid payment amount with at most 2 decimals.");
        using var con=new SqlConnection(connectionString);con.Open();
        using var cmd=Cmd(con,null,"sp_BillingRecordPayment",true,P("@BillId",input.BillId),P("@SubmissionId",input.SubmissionId),P("@Amount",input.Amount),P("@Method",input.Method),P("@Reference",input.Reference?.Trim()),P("@UserId",userId));
        cmd.ExecuteScalar();
    }
    public List<BillingRule> Rules()
    {
        using var con=new SqlConnection(connectionString);con.Open();
        return Rows<BillingRule>(Read(con,null,@"SELECT DISTINCT m.CategoryId,m.TestId,c.CategoryName,t.TestName,ISNULL(t.Unit,'') MasterReference,t.Deducations MasterDeduction,ISNULL(u.UnitCode,'') MasterUnit,
        CAST(CASE WHEN r.RuleId IS NULL THEN 0 ELSE 1 END AS bit) Configured,ISNULL(r.Threshold,0) Threshold,ISNULL(r.DeductionMode,'None') DeductionMode,ISNULL(r.DeductionValue,0) DeductionValue
        FROM m_ItemLabTest m JOIN m_ItemCategory c ON c.CategoryId=m.CategoryId JOIN m_LabTestMaster t ON t.TestId=m.TestId AND t.IsActive=1
        LEFT JOIN tbl_Masterofunit u ON u.UnitId=t.Deducationsunitid LEFT JOIN m_BillingLabRule r ON r.CategoryId=m.CategoryId AND r.TestId=m.TestId AND r.IsActive=1
        WHERE m.IsActive=1 ORDER BY c.CategoryName,t.TestName;",false).Tables[0]);
    }
    public void SaveRule(BillingRule rule,int userId)
    {
        if(!BillingCalculator.Modes.ContainsKey(rule.DeductionMode) || rule.Threshold<0 || rule.DeductionValue<0 || (rule.DeductionMode=="Percent" && rule.DeductionValue>100) ||
            decimal.Round(rule.Threshold,6)!=rule.Threshold || decimal.Round(rule.DeductionValue,6)!=rule.DeductionValue)
            throw new ArgumentException("Enter a valid threshold and deduction. Percentage cannot exceed 100; use at most 6 decimals.");
        using var con=new SqlConnection(connectionString);con.Open();
        using var tx=con.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            using var cmd=Cmd(con,tx,@"IF NOT EXISTS(SELECT 1 FROM m_ItemLabTest m JOIN m_LabTestMaster t ON t.TestId=m.TestId WHERE m.CategoryId=@CategoryId AND m.TestId=@TestId AND m.IsActive=1 AND t.IsActive=1) THROW 50001,'Select an active mapped category/test.',1;
            UPDATE m_BillingLabRule SET Threshold=@Threshold,DeductionMode=@Mode,DeductionValue=@Value,IsActive=1,ModifiedBy=@UserId,ModifiedAt=sysdatetime() WHERE CategoryId=@CategoryId AND TestId=@TestId;
            IF @@ROWCOUNT=0 INSERT m_BillingLabRule(CategoryId,TestId,Threshold,DeductionMode,DeductionValue,ModifiedBy) VALUES(@CategoryId,@TestId,@Threshold,@Mode,@Value,@UserId);",false,
            P("@CategoryId",rule.CategoryId),P("@TestId",rule.TestId),P("@Threshold",rule.Threshold),P("@Mode",rule.DeductionMode),P("@Value",rule.DeductionValue),P("@UserId",userId));
            cmd.ExecuteNonQuery();tx.Commit();
        }catch{tx.Rollback();throw;}
    }
}
