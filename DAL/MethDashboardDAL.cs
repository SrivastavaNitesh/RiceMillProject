using System.Data;
using Microsoft.Data.SqlClient;
using RiceMillProject.Models;
namespace RiceMillProject.DAL;
public class MethDashboardDAL(IConfiguration configuration)
{
    private readonly string cs=configuration.GetConnectionString("DefaultConnection")??"";
    public MethDashboard Get(int personId)
    {
        var m=new MethDashboard();using var c=new SqlConnection(cs);using var cmd=new SqlCommand("dbo.sp_GetMethDashboard",c){CommandType=CommandType.StoredProcedure};cmd.Parameters.Add("@MethPersonId",SqlDbType.Int).Value=personId;using var da=new SqlDataAdapter(cmd);var ds=new DataSet();da.Fill(ds);
        if(ds.Tables.Count>0&&ds.Tables[0].Rows.Count>0){var r=ds.Tables[0].Rows[0];m.MethName=T(r,"MethName");m.CompanyName=T(r,"CompanyName");m.PendingWork=I(r,"PendingWork");m.CompletedWork=I(r,"CompletedWork");m.TodayHandledBags=I(r,"TodayHandledBags");m.TotalPayable=D(r,"TotalPayable");m.AwaitingAssignment=I(r,"AwaitingAssignment");}
        if(ds.Tables.Count>1)m.Statuses=ds.Tables[1].AsEnumerable().Select(r=>new MethChartPoint{Name=T(r,"Name"),Value=D(r,"Value")}).ToList();
        if(ds.Tables.Count>2)m.DailyTrend=ds.Tables[2].AsEnumerable().Select(r=>new MethTrendPoint{DateLabel=T(r,"DateLabel"),Bags=I(r,"Bags")}).ToList();
        if(ds.Tables.Count>3)m.WorkTypes=ds.Tables[3].AsEnumerable().Select(r=>new MethChartPoint{Name=T(r,"Name"),Value=D(r,"Value")}).ToList();
        if(ds.Tables.Count>4)m.AwaitingRsts=ds.Tables[4].AsEnumerable().Select(r=>new MethAwaitingRst{RSTNumber=T(r,"RSTNumber"),VehicleNumber=T(r,"VehicleNumber"),PartyName=T(r,"PartyName"),GateEntryTime=Convert.ToDateTime(r["GateEntryTime"])}).ToList();return m;
    }
    static string T(DataRow r,string c)=>r[c]==DBNull.Value?"":r[c]?.ToString()??"";static int I(DataRow r,string c)=>r[c]==DBNull.Value?0:Convert.ToInt32(r[c]);static decimal D(DataRow r,string c)=>r[c]==DBNull.Value?0:Convert.ToDecimal(r[c]);
}
