using System.Data;
using Microsoft.Data.SqlClient;
using RiceMillProject.Models;

namespace RiceMillProject.DAL;

public class SupervisorDashboardDAL(IConfiguration configuration)
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;

    public SupervisorDashboard GetDashboard(int supervisorId, int officeId)
    {
        var model = new SupervisorDashboard();
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("dbo.sp_GetSupervisorDashboard", connection) { CommandType = CommandType.StoredProcedure };
        command.Parameters.Add("@SupervisorId", SqlDbType.Int).Value = supervisorId;
        command.Parameters.Add("@OfficeId", SqlDbType.Int).Value = officeId;
        using var adapter = new SqlDataAdapter(command);
        var data = new DataSet();
        adapter.Fill(data);

        if (data.Tables.Count > 0 && data.Tables[0].Rows.Count > 0)
        {
            var row = data.Tables[0].Rows[0];
            model.SupervisorName = Text(row, "SupervisorName", "Supervisor");
            model.CompanyName = Text(row, "CompanyName", "Company");
            model.PendingAssignments = Value(row, "PendingAssignments", 0);
            model.WorkerPending = Value(row, "WorkerPending", 0);
            model.FieldPending = Value(row, "FieldPending", 0);
            model.TodayCompleted = Value(row, "TodayCompleted", 0);
            model.TotalAssigned = Value(row, "TotalAssigned", 0);
            model.TotalRecordedBags = Value(row, "TotalRecordedBags", 0);
        }

        if (data.Tables.Count > 1) model.Statuses = Points(data.Tables[1]);
        if (data.Tables.Count > 2) model.DailyTrend = data.Tables[2].AsEnumerable().Select(row => new SupervisorTrendPoint
        {
            DateLabel = Text(row, "DateLabel"), Completed = Value(row, "Completed", 0), Bags = Value(row, "Bags", 0)
        }).ToList();
        if (data.Tables.Count > 3) model.LocationDetails = data.Tables[3].AsEnumerable().Select(row => new SupervisorLocationDetail
        {
            LocationId = Value(row, "LocationId", 0), LocationName = Text(row, "LocationName"),
            CategoryId = Value(row, "CategoryId", 0), CategoryName = Text(row, "CategoryName"),
            ItemId = Value(row, "ItemId", 0), ItemName = Text(row, "ItemName"),
            BagTypeId = Value(row, "BagTypeId", 0), BagTypeName = Text(row, "BagTypeName"), Bags = Value(row, "Bags", 0)
        }).ToList();
        if (data.Tables.Count > 4) model.BagTypes = Points(data.Tables[4]);
        if (data.Tables.Count > 5) model.WorkerDetails = data.Tables[5].AsEnumerable().Select(row => new SupervisorWorkerDetail
        {
            WorkerId = Value(row, "WorkerId", 0), WorkerName = Text(row, "WorkerName"), WorkType = Text(row, "WorkType"),
            BagTypeId = Value(row, "BagTypeId", 0), BagTypeName = Text(row, "BagTypeName"), Bags = Value(row, "Bags", 0)
        }).ToList();
        if (data.Tables.Count > 6) model.LiveWork = data.Tables[6].AsEnumerable().Select(row => new SupervisorLiveWork
        {
            UnloadId = Value(row, "UnloadId", 0), RSTNumber = Text(row, "RSTNumber"), VehicleNumber = Text(row, "VehicleNumber"),
            PartyName = Text(row, "PartyName"), MethName = Text(row, "MethName"), LocationName = Text(row, "LocationName"),
            CurrentStage = Text(row, "CurrentStage"), Bags = Value(row, "Bags", 0),
            LastActivity = row["LastActivity"] == DBNull.Value ? null : Convert.ToDateTime(row["LastActivity"])
        }).ToList();
        return model;
    }

    private static List<SupervisorChartPoint> Points(DataTable table) => table.AsEnumerable().Select(row => new SupervisorChartPoint
    {
        Name = Text(row, "Name"), Value = Value(row, "Value", 0m)
    }).ToList();
    private static string Text(DataRow row, string column, string fallback = "") => row[column] == DBNull.Value ? fallback : row[column]?.ToString() ?? fallback;
    private static T Value<T>(DataRow row, string column, T fallback) => row[column] == DBNull.Value ? fallback : (T)Convert.ChangeType(row[column], typeof(T));
}
