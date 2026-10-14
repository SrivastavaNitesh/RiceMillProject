using System.Data;
using Microsoft.Data.SqlClient;
using RiceMillProject.Models;

namespace RiceMillProject.DAL;

public class DashboardDAL(IConfiguration configuration)
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";

    public DashboardStats GetAdminDashboard(int officeId, DateTime fromDate, DateTime toDate)
    {
        var model = new DashboardStats { OfficeId = officeId };
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("dbo.sp_GetAdminDashboard", connection) { CommandType = CommandType.StoredProcedure };
        command.Parameters.Add("@OfficeId", SqlDbType.Int).Value = officeId;
        command.Parameters.Add("@FromDate", SqlDbType.Date).Value = fromDate;
        command.Parameters.Add("@ToDate", SqlDbType.Date).Value = toDate;
        using var adapter = new SqlDataAdapter(command);
        var data = new DataSet();
        adapter.Fill(data);

        if (data.Tables.Count > 0 && data.Tables[0].Rows.Count > 0)
        {
            var row = data.Tables[0].Rows[0];
            model.OfficeName = Text(row, "OfficeName", "Company");
            model.AsOf = Value(row, "AsOf", DateTime.Now);
            model.FromDate = fromDate;
            model.ToDate = toDate;
            model.TodayInwardEntries = Value(row, "TodayInwardEntries", 0);
            model.PendingWeighmentRst = Value(row, "PendingWeighmentRst", 0);
            model.ActiveRst = Value(row, "ActiveRst", 0);
            model.FinalRst = Value(row, "FinalRst", 0);
            model.TodayTotalBags = Value(row, "TodayTotalBags", 0);
            model.TodayTotalWeight = Value(row, "TodayTotalWeight", 0m);
            model.PendingUnload = Value(row, "PendingUnload", 0);
            model.PendingLab = Value(row, "PendingLab", 0);
            model.PendingBilling = Value(row, "PendingBilling", 0);
        }
        if (data.Tables.Count > 1) model.RstStatuses = data.Tables[1].AsEnumerable().Select(row => new DashboardChartPoint { Name = Text(row, "Name"), Value = Value(row, "Value", 0m) }).ToList();
        if (data.Tables.Count > 2) model.CategoryBreakdown = data.Tables[2].AsEnumerable().Select(row => new DashboardCategoryBreakdown
        {
            CategoryId = Value(row, "CategoryId", 0), CategoryName = Text(row, "CategoryName"), ItemId = Value(row, "ItemId", 0),
            ItemName = Text(row, "ItemName"), BagTypeId = Value(row, "BagTypeId", 0), BagTypeName = Text(row, "BagTypeName"), Bags = Value(row, "Bags", 0)
        }).ToList();
        if (data.Tables.Count > 3) model.LocationSummary = data.Tables[3].AsEnumerable().Select(row => new DashboardLocationPoint { LocationId = Value(row, "LocationId", 0), LocationName = Text(row, "LocationName"), Bags = Value(row, "Bags", 0), WeightKg = Value(row, "WeightKg", 0m) }).ToList();
        if (data.Tables.Count > 4) model.LocationDetails = data.Tables[4].AsEnumerable().Select(row => new DashboardLocationDetail
        {
            LocationId = Value(row, "LocationId", 0), LocationName = Text(row, "LocationName"), RSTNumber = Text(row, "RSTNumber"),
            CategoryId = Value(row, "CategoryId", 0), CategoryName = Text(row, "CategoryName"), ItemId = Value(row, "ItemId", 0),
            VarietyName = Text(row, "VarietyName"), BagTypeId = Value(row, "BagTypeId", 0), BagTypeName = Text(row, "BagTypeName"), Bags = Value(row, "Bags", 0),
            WeightKg = Value(row, "WeightKg", 0m), UnloadTime = row.IsNull("UnloadTime") ? null : Convert.ToDateTime(row["UnloadTime"])
        }).ToList();
        if (data.Tables.Count > 5) model.PendingDetails = data.Tables[5].AsEnumerable().Select(row => new DashboardPendingDetail
        {
            Stage = Text(row, "Stage"), RSTNumber = Text(row, "RSTNumber"), VehicleNumber = Text(row, "VehicleNumber"), PartyName = Text(row, "PartyName"),
            Since = Value(row, "Since", DateTime.Now), AgeHours = Value(row, "AgeHours", 0m)
        }).ToList();
        if (data.Tables.Count > 6) model.SupplierPerformance = data.Tables[6].AsEnumerable().Select(row => new DashboardSupplierPerformance
        {
            SupplierName = Text(row, "SupplierName"), Vehicles = Value(row, "Vehicles", 0), Bags = Value(row, "Bags", 0),
            WeightKg = Value(row, "WeightKg", 0m), AverageWeightKg = Value(row, "AverageWeightKg", 0m)
        }).ToList();
        if (data.Tables.Count > 7) model.RstJourneys = data.Tables[7].AsEnumerable().Select(row => new DashboardRstJourney
        {
            RSTNumber = Text(row, "RSTNumber"), VehicleNumber = Text(row, "VehicleNumber"), PartyName = Text(row, "PartyName"),
            CurrentStage = Text(row, "CurrentStage"), CurrentStatus = Text(row, "CurrentStatus"), LastActivity = Value(row, "LastActivity", DateTime.Now), AgeHours = Value(row, "AgeHours", 0m)
        }).ToList();
        return model;
    }

    private static string Text(DataRow row, string column, string fallback = "") => row.Table.Columns.Contains(column) && !row.IsNull(column) ? Convert.ToString(row[column]) ?? fallback : fallback;
    private static T Value<T>(DataRow row, string column, T fallback) => row.Table.Columns.Contains(column) && !row.IsNull(column) ? (T)Convert.ChangeType(row[column], Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T)) : fallback;
}
