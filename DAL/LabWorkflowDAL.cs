using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using RiceMillProject.Models;

namespace RiceMillProject.DAL;

public class LabWorkflowDAL(IConfiguration configuration)
{
    private readonly string connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
    private SqlCommand Command(SqlConnection connection, string procedure, params SqlParameter[] parameters)
    {
        var cmd = new SqlCommand(procedure, connection) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.AddRange(parameters);
        return cmd;
    }
    private static SqlParameter P(string name, SqlDbType type, object? value, int size = 0) =>
        new(name, type) { Value = value ?? DBNull.Value, Size = size };
    private DataTable Read(string procedure, params SqlParameter[] parameters)
    {
        using var con = new SqlConnection(connectionString);
        using var cmd = Command(con, procedure, parameters);
        using var adapter = new SqlDataAdapter(cmd);
        var table = new DataTable();
        adapter.Fill(table);
        return table;
    }
    private int Execute(string procedure, params SqlParameter[] parameters)
    {
        using var con = new SqlConnection(connectionString);
        using var cmd = Command(con, procedure, parameters);
        con.Open();
        return Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
    }
    private static string S(DataRow row, string key) => Convert.ToString(row[key]) ?? "";
    private static int I(DataRow row, string key) => Convert.ToInt32(row[key]);
    private static LabTestMaster Test(DataRow r) => new()
    {
        TestId = I(r, "TestId"), TestName = S(r, "TestName"), Unit = S(r, "Unit"),
        ResultType = S(r, "ResultType"), Description = S(r, "Description"), IsActive = Convert.ToBoolean(r["IsActive"]),
        Deducations = r.Table.Columns.Contains("Deducations") && !r.IsNull("Deducations") ? I(r, "Deducations") : null,
        UnitId = r.Table.Columns.Contains("UnitId") && !r.IsNull("UnitId") ? I(r, "UnitId") : null,
        UnitName = r.Table.Columns.Contains("UnitName") ? S(r, "UnitName") : null
    };
    public List<LabTestMaster> GetTests() => Read("sp_LabTestList").AsEnumerable().Select(Test).ToList();
    public void SaveTest(LabTestMaster test, int userId) => Execute(
    "sp_LabTestSave",

    P("@TestId", SqlDbType.Int, test.TestId),

    P("@TestName", SqlDbType.NVarChar, test.TestName.Trim(), 120),

    P("@Unit", SqlDbType.NVarChar, test.Unit?.Trim(), 30),

    P("@ResultType", SqlDbType.VarChar, test.ResultType, 10),

    P("@Description", SqlDbType.NVarChar, test.Description, 500),

    P("@Deducations", SqlDbType.Int, test.Deducations),

    P("@UnitId", SqlDbType.Int, test.UnitId),

    P("@UserId", SqlDbType.Int, userId)
);
    public DataTable GetUnits()
    {
        DataTable dt = new DataTable();

        using var con = new SqlConnection(connectionString);
        using var cmd = new SqlCommand(@"
        SELECT 
            UnitId,
            UnitName,
            UnitCode
        FROM dbo.tbl_Masterofunit
        WHERE IsActive = 1
        ORDER BY UnitId
    ", con);

        using var da = new SqlDataAdapter(cmd);

        con.Open();
        da.Fill(dt);

        return dt;
    }
    public void RemoveTest(int id, int userId) => Execute("sp_LabTestRemove", P("@TestId", SqlDbType.Int, id), P("@UserId", SqlDbType.Int, userId));
    public List<LabItemTestMapping> GetMappings() => Read("sp_LabMappingList").AsEnumerable().Select(r => new LabItemTestMapping
    {
        MappingId = I(r, "MappingId"), CategoryId = I(r, "CategoryId"), ItemId = I(r, "ItemId"), TestId = I(r, "TestId"),
        CategoryName = S(r, "CategoryName"), ItemName = S(r, "ItemName"), TestName = S(r, "TestName")
    }).ToList();
    public void SaveMapping(LabItemTestMapping mapping, int userId) => Execute("sp_LabMappingSave",
        P("@MappingId", SqlDbType.Int, mapping.MappingId), P("@CategoryId", SqlDbType.Int, mapping.CategoryId),
        P("@ItemId", SqlDbType.Int, mapping.ItemId), P("@TestId", SqlDbType.Int, mapping.TestId), P("@UserId", SqlDbType.Int, userId));
    public void SaveMappings(LabItemTestSelection form, int userId) => Execute("sp_LabMappingSetSave",
        P("@CategoryId", SqlDbType.Int, form.CategoryId), P("@ItemId", SqlDbType.Int, form.ItemId),
        P("@TestIds", SqlDbType.NVarChar, JsonSerializer.Serialize(form.SelectedTestIds), -1),
        P("@OriginalTestIds", SqlDbType.NVarChar, JsonSerializer.Serialize(form.OriginalTestIds), -1),
        P("@UserId", SqlDbType.Int, userId));
    public void RemoveMapping(int id, int userId) => Execute("sp_LabMappingRemove", P("@MappingId", SqlDbType.Int, id), P("@UserId", SqlDbType.Int, userId));
    public List<LabItemOption> GetItems(string? rstNumber = null)
    {
        using var con = new SqlConnection(connectionString);
        using var cmd = Command(con, "sp_LabItemOptions", P("@RSTNumber", SqlDbType.NVarChar, rstNumber, 50));
        using var adapter = new SqlDataAdapter(cmd);
        var data = new DataSet();
        adapter.Fill(data);
        var items = data.Tables[0].AsEnumerable().Select(r => new LabItemOption
        {
            ItemId = I(r, "ItemId"), ItemName = S(r, "ItemName"), CategoryId = I(r, "CategoryId"), CategoryName = S(r, "CategoryName")
        }).ToList();
        foreach (var r in data.Tables[1].AsEnumerable())
            items.FirstOrDefault(i => i.ItemId == I(r, "ItemId") && i.CategoryId == I(r, "CategoryId"))?.Tests.Add(Test(r));
        return items;
    }
    public List<LabRstSummary> GetRsts() => Read("sp_LabRstList").AsEnumerable().Select(r => new LabRstSummary
    {
        RSTNumber = S(r, "RSTNumber"), VehicleNumber = S(r, "VehicleNumber"), PartyName = S(r, "PartyName"),
        ItemCount = I(r, "ItemCount"), RequiredTests = I(r, "RequiredTests"), CompletedTests = I(r, "CompletedTests"), UnmappedItems = I(r, "UnmappedItems")
    }).ToList();
    public int SaveReport(LabEntryViewModel entry, int userId) => Execute("sp_LabReportSave",
        P("@RSTNumber", SqlDbType.NVarChar, entry.RSTNumber, 50), P("@SubmissionId", SqlDbType.UniqueIdentifier, entry.SubmissionId),
        P("@SelectedItems", SqlDbType.NVarChar, JsonSerializer.Serialize(entry.SelectedItemIds), -1),
        P("@Results", SqlDbType.NVarChar, JsonSerializer.Serialize(entry.Results), -1),
        P("@Remarks", SqlDbType.NVarChar, entry.Remarks, 1000), P("@UserId", SqlDbType.Int, userId));
    public List<LabReport> GetReports(string? rst, DateTime? from, DateTime? to) => Read("sp_LabReportList",
        P("@RSTNumber", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(rst) ? null : rst.Trim(), 50),
        P("@From", SqlDbType.Date, from), P("@To", SqlDbType.Date, to)).AsEnumerable().Select(r => new LabReport
        {
            ReportId = I(r, "ReportId"), RSTNumber = S(r, "RSTNumber"), TechnicianName = S(r, "TechnicianName"),
            TestedAt = Convert.ToDateTime(r["TestedAt"]), Remarks = S(r, "Remarks"), ItemCount = I(r, "ItemCount"), ResultCount = I(r, "ResultCount")
        }).ToList();
    public LabReport? GetReport(int id)
    {
        using var con = new SqlConnection(connectionString);
        using var cmd = Command(con, "sp_LabReportGet", P("@ReportId", SqlDbType.Int, id));
        using var adapter = new SqlDataAdapter(cmd);
        var data = new DataSet();
        adapter.Fill(data);
        if (data.Tables[0].Rows.Count == 0) return null;
        var r = data.Tables[0].Rows[0];
        return new LabReport
        {
            ReportId = I(r, "ReportId"), RSTNumber = S(r, "RSTNumber"), TechnicianName = S(r, "TechnicianName"),
            TestedAt = Convert.ToDateTime(r["TestedAt"]), Remarks = S(r, "Remarks"),
            Results = data.Tables[1].AsEnumerable().Select(x => new LabReportResult
            {
                CategoryId = I(x, "CategoryId"), ItemId = I(x, "ItemId"),
                CategoryName = S(x, "CategoryName"), ItemName = S(x, "ItemName"), TestName = S(x, "TestName"), Unit = S(x, "Unit"), Value = S(x, "ResultValue")
            }).ToList()
        };
    }
}
