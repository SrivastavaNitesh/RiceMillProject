using System.Data;
using Microsoft.Data.SqlClient;
using RiceMillProject.Models;

namespace RiceMillProject.DAL;

public class MethPaymentReportDAL(IConfiguration configuration)
{
    private readonly string connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";

    public MethPaymentReport Load(int officeId, int supervisorScopeId, DateTime? fromDate, DateTime? toDate, int? selectedSupervisorId, int? methId, string? search, bool isAdmin)
    {
        var model = new MethPaymentReport
        {
            FromDate = fromDate,
            ToDate = toDate,
            SupervisorId = selectedSupervisorId,
            MethId = methId,
            Search = (search ?? "").Trim(),
            IsAdminView = isAdmin
        };
        using var connection = new SqlConnection(connectionString);
        using var command = new SqlCommand("dbo.sp_GetMethPaymentReport", connection) { CommandType = CommandType.StoredProcedure };
        command.Parameters.Add("@OfficeId", SqlDbType.Int).Value = officeId;
        command.Parameters.Add("@SupervisorScopeId", SqlDbType.Int).Value = supervisorScopeId;
        command.Parameters.Add("@SelectedSupervisorId", SqlDbType.Int).Value = selectedSupervisorId ?? 0;
        command.Parameters.Add("@MethId", SqlDbType.Int).Value = methId ?? 0;
        command.Parameters.Add("@FromDate", SqlDbType.Date).Value = fromDate?.Date ?? new DateTime(1900, 1, 1);
        command.Parameters.Add("@ToDate", SqlDbType.Date).Value = toDate?.Date ?? new DateTime(9999, 12, 31);
        command.Parameters.Add("@Search", SqlDbType.NVarChar, 100).Value = model.Search;
        var data = new DataSet();
        using var adapter = new SqlDataAdapter(command);
        adapter.Fill(data);
        model.Supervisors = Options(data.Tables[0]);
        model.Meths = Options(data.Tables[1]);
        model.Rows = data.Tables[2].AsEnumerable().Select(row => new MethPaymentReportRow
        {
            UnloadId = row.Field<int>("UnloadId"), RSTNumber = row.Field<string>("RSTNumber") ?? "",
            SupervisorId = row.Field<int>("SupervisorId"), SupervisorName = row.Field<string>("SupervisorName") ?? "",
            MethId = row.Field<int>("MethId"), MethName = row.Field<string>("MethName") ?? "",
            WorkDate = row.Field<DateTime>("WorkDate"), UnloadingBags = row.Field<int>("UnloadingBags"),
            ReturnBags = row.Field<int>("ReturnBags"), UnloadingAmount = row.Field<decimal>("UnloadingAmount"),
            ReturnLoadingAmount = row.Field<decimal>("ReturnLoadingAmount")
        }).ToList();
        var rowsById = model.Rows.ToDictionary(x => x.UnloadId);
        foreach (DataRow row in data.Tables[3].Rows)
            if (rowsById.TryGetValue(row.Field<int>("UnloadId"), out var parent))
                parent.Details.Add(new MethPaymentReportDetail
                {
                    WorkType = row.Field<string>("WorkType") ?? "", BagTypeName = row.Field<string>("BagTypeName") ?? "",
                    BagCount = row.Field<int>("BagCount"), PerBagPrice = row.Field<decimal>("PerBagPrice"), Amount = row.Field<decimal>("Amount")
                });
        return model;
    }

    private static List<LookupOption> Options(DataTable table) => table.AsEnumerable()
        .Select(row => new LookupOption { Id = row.Field<int>("Id"), Name = row.Field<string>("Name") ?? "" }).ToList();
}
