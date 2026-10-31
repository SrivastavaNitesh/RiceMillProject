using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using RiceMillProject.Models;

namespace RiceMillProject.DAL;

public class OrderReturnDAL(IConfiguration configuration)
{
    private readonly string connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";

    public OrderReturnPage GetPage(int officeId)
    {
        var page = new OrderReturnPage();
        using var connection = new SqlConnection(connectionString);
        using var command = new SqlCommand("dbo.sp_GetOrderReturnPage", connection) { CommandType = CommandType.StoredProcedure };
        command.Parameters.Add("@OfficeId", SqlDbType.Int).Value = officeId;
        using var adapter = new SqlDataAdapter(command);
        var data = new DataSet();
        adapter.Fill(data);
        if (data.Tables.Count > 0) page.Returns = data.Tables[0].AsEnumerable().Select(MapSummary).ToList();
        if (data.Tables.Count > 1) page.EligibleRsts = data.Tables[1].AsEnumerable().Select(row => new OrderReturnSource
        {
            UnloadId = Int(row,"UnloadId"), RSTNumber = Text(row,"RSTNumber"), VehicleNumber = Text(row,"VehicleNumber"),
            PartyName = Text(row,"PartyName"), AvailableBags = Int(row,"AvailableBags"), ReturnBags = Int(row,"ReturnBags"),
            OrderReturnId = Int(row,"OrderReturnId"), ReturnNumber = Text(row,"ReturnNumber"), ReturnStatus = Text(row,"ReturnStatus"),
            StageCode = Text(row,"StageCode"), StageName = Text(row,"StageName"), LastUpdatedAt = Date(row,"LastUpdatedAt"),
            CanCreateReturn = Bool(row,"CanCreateReturn"), ActionMessage = Text(row,"ActionMessage")
        }).ToList();
        if (data.Tables.Count > 2) page.Locations = Options(data.Tables[2]);
        if (data.Tables.Count > 3) page.Vehicles = Options(data.Tables[3]);
        if (data.Tables.Count > 4) page.BagTypes = Options(data.Tables[4]);
        if (data.Tables.Count > 5) page.Workers = Options(data.Tables[5]);
        return page;
    }

    public OrderReturnDetails? GetDetails(int id, int officeId)
    {
        using var connection = new SqlConnection(connectionString);
        using var command = new SqlCommand("dbo.sp_GetOrderReturnDetails", connection) { CommandType = CommandType.StoredProcedure };
        command.Parameters.Add("@OrderReturnId", SqlDbType.Int).Value = id;
        command.Parameters.Add("@OfficeId", SqlDbType.Int).Value = officeId;
        using var adapter = new SqlDataAdapter(command);
        var data = new DataSet(); adapter.Fill(data);
        if (data.Tables.Count == 0 || data.Tables[0].Rows.Count == 0) return null;
        var summary = MapSummary(data.Tables[0].Rows[0]);
        var details = new OrderReturnDetails { OrderReturnId=summary.OrderReturnId, ReturnNumber=summary.ReturnNumber, UnloadId=summary.UnloadId, RSTNumber=summary.RSTNumber, VehicleNumber=summary.VehicleNumber, PartyName=summary.PartyName, SourceLocationName=summary.SourceLocationName, RequestedBags=summary.RequestedBags, ApprovedBags=summary.ApprovedBags, DispatchedBags=summary.DispatchedBags, ReturnReason=summary.ReturnReason, QualityRemarks=summary.QualityRemarks, QualityDecision=summary.QualityDecision, Status=summary.Status, RequestedAt=summary.RequestedAt };
        if (data.Tables.Count > 1) details.Dispatches = data.Tables[1].AsEnumerable().Select(row => new OrderReturnDispatch { DispatchId=Int(row,"DispatchId"), DispatchNumber=Text(row,"DispatchNumber"), DispatchBags=Int(row,"DispatchBags"), VehicleId=NullableInt(row,"VehicleId"), VehicleNumber=Text(row,"VehicleNumber"), GrossWeight=NullableDecimal(row,"GrossWeight"), TareWeight=NullableDecimal(row,"TareWeight"), NetWeight=NullableDecimal(row,"NetWeight"), Status=Text(row,"Status"), CreatedAt=Date(row,"CreatedAt") }).ToList();
        if (data.Tables.Count > 2) details.Movements = data.Tables[2].AsEnumerable().Select(row => new OrderReturnMovement { MovementId=Int(row,"MovementId"), DispatchId=NullableInt(row,"DispatchId"), MovementType=Text(row,"MovementType"), FromLocation=Text(row,"FromLocation"), ToLocation=Text(row,"ToLocation"), BagType=Text(row,"BagType"), Bags=Int(row,"Bags"), Amount=Decimal(row,"Amount"), MovementAt=Date(row,"MovementAt") }).ToList();
        if (data.Tables.Count > 3) details.History = data.Tables[3].AsEnumerable().Select(row => new OrderReturnHistory { NewStatus=Text(row,"NewStatus"), Remarks=Text(row,"Remarks"), ChangedBy=Text(row,"ChangedBy"), ChangedAt=Date(row,"ChangedAt") }).ToList();
        return details;
    }

    public int Create(CreateOrderReturnInput input, int userId, int officeId) => Scalar("dbo.sp_CreateOrderReturn", p => { p.AddWithValue("@UnloadId",input.UnloadId); p.AddWithValue("@RequestedBags",input.RequestedBags); p.AddWithValue("@SourceLocationId",(object?)input.SourceLocationId??DBNull.Value); p.AddWithValue("@Reason",input.Reason); p.AddWithValue("@UserId",userId); p.AddWithValue("@OfficeId",officeId); });
    public void Decide(DecideOrderReturnInput input,int userId,int officeId) => Execute("dbo.sp_DecideOrderReturn",p=>{p.AddWithValue("@OrderReturnId",input.OrderReturnId);p.AddWithValue("@Decision",input.Decision);p.AddWithValue("@ApprovedBags",(object?)input.ApprovedBags??DBNull.Value);p.AddWithValue("@Remarks",(object?)input.Remarks??DBNull.Value);p.AddWithValue("@UserId",userId);p.AddWithValue("@OfficeId",officeId);});
    public int CreateDispatch(CreateReturnDispatchInput input,int userId,int officeId) => Scalar("dbo.sp_CreateOrderReturnDispatch",p=>{p.AddWithValue("@OrderReturnId",input.OrderReturnId);p.AddWithValue("@DispatchBags",input.DispatchBags);p.AddWithValue("@VehicleId",(object?)input.VehicleId??DBNull.Value);p.AddWithValue("@Remarks",(object?)input.Remarks??DBNull.Value);p.AddWithValue("@UserId",userId);p.AddWithValue("@OfficeId",officeId);});
    public int SaveMovement(SaveReturnMovementInput input,int userId,int officeId) => Scalar("dbo.sp_SaveOrderReturnMovement",p=>{p.AddWithValue("@OrderReturnId",input.OrderReturnId);p.AddWithValue("@DispatchId",(object?)input.DispatchId??DBNull.Value);p.AddWithValue("@MovementType",input.MovementType);p.AddWithValue("@FromLocationId",(object?)input.FromLocationId??DBNull.Value);p.AddWithValue("@ToLocationId",(object?)input.ToLocationId??DBNull.Value);p.AddWithValue("@BagTypeId",input.BagTypeId);p.AddWithValue("@Bags",input.Bags);p.AddWithValue("@WorkersJson",JsonSerializer.Serialize(input.Workers));p.AddWithValue("@Remarks",(object?)input.Remarks??DBNull.Value);p.AddWithValue("@UserId",userId);p.AddWithValue("@OfficeId",officeId);});
    public void SaveWeight(SaveReturnWeightInput input,int userId,int officeId) => Execute("dbo.sp_SaveOrderReturnWeight",p=>{p.AddWithValue("@DispatchId",input.DispatchId);p.AddWithValue("@GrossWeight",input.GrossWeight);p.AddWithValue("@TareWeight",input.TareWeight);p.AddWithValue("@VehicleId",input.VehicleId);p.AddWithValue("@UserId",userId);p.AddWithValue("@OfficeId",officeId);});
    public void CompleteOutward(int dispatchId,int userId,int officeId) => Execute("dbo.sp_CompleteOrderReturnOutward",p=>{p.AddWithValue("@DispatchId",dispatchId);p.AddWithValue("@UserId",userId);p.AddWithValue("@OfficeId",officeId);});

    private int Scalar(string procedure, Action<SqlParameterCollection> add)
    { using var connection=new SqlConnection(connectionString);using var command=new SqlCommand(procedure,connection){CommandType=CommandType.StoredProcedure};add(command.Parameters);connection.Open();using var reader=command.ExecuteReader();return reader.Read()?Convert.ToInt32(reader[0]):0; }
    private void Execute(string procedure, Action<SqlParameterCollection> add)
    { using var connection=new SqlConnection(connectionString);using var command=new SqlCommand(procedure,connection){CommandType=CommandType.StoredProcedure};add(command.Parameters);connection.Open();command.ExecuteNonQuery(); }
    private static OrderReturnSummary MapSummary(DataRow row)=>new(){OrderReturnId=Int(row,"OrderReturnId"),ReturnNumber=Text(row,"ReturnNumber"),UnloadId=Int(row,"UnloadId"),RSTNumber=Text(row,"RSTNumber"),VehicleNumber=Text(row,"VehicleNumber"),PartyName=Text(row,"PartyName"),SourceLocationName=Text(row,"SourceLocationName"),RequestedBags=Int(row,"RequestedBags"),ApprovedBags=Int(row,"ApprovedBags"),DispatchedBags=Int(row,"DispatchedBags"),ReturnReason=Text(row,"ReturnReason"),QualityRemarks=Text(row,"QualityRemarks"),QualityDecision=Text(row,"QualityDecision"),Status=Text(row,"Status"),RequestedAt=Date(row,"RequestedAt")};
    private static List<LookupOption> Options(DataTable table)=>table.AsEnumerable().Select(row=>new LookupOption{Id=Int(row,"Id"),Name=Text(row,"Name")}).ToList();
    private static bool Bool(DataRow r,string c)=>r[c]!=DBNull.Value&&Convert.ToBoolean(r[c]);
    private static string Text(DataRow r,string c)=>r[c]==DBNull.Value?"":Convert.ToString(r[c])??""; private static int Int(DataRow r,string c)=>r[c]==DBNull.Value?0:Convert.ToInt32(r[c]); private static int? NullableInt(DataRow r,string c)=>r[c]==DBNull.Value?null:Convert.ToInt32(r[c]); private static decimal Decimal(DataRow r,string c)=>r[c]==DBNull.Value?0:Convert.ToDecimal(r[c]); private static decimal? NullableDecimal(DataRow r,string c)=>r[c]==DBNull.Value?null:Convert.ToDecimal(r[c]); private static DateTime Date(DataRow r,string c)=>r[c]==DBNull.Value?DateTime.MinValue:Convert.ToDateTime(r[c]);
}
