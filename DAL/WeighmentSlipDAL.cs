using Microsoft.Data.SqlClient;
using RiceMillProject.Models;
namespace RiceMillProject.DAL;
public class WeighmentSlipDAL(IConfiguration configuration)
{
 public WeighmentSlipModel? Load(string rst)
 {
  using var con=new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
  using var cmd=new SqlCommand(@"SELECT h.InwardId,g.RSTNumber,g.InwardNo,h.GateInDateTime,g.GateEntryTime,g.GateExitTime,
p.PersonName PartyName,v.VehicleNumber,d.PersonName DriverName,d.MobileNumber DriverMobile,c.CategoryName,g.GrossWeight,g.TareWeight,g.NetWeight,g.WeighmentCharge,g.Status,
unload.UnloadDateTime
FROM dbo.t_GateEntry g JOIN dbo.t_InwardHeader h ON h.InwardNo=g.InwardNo
LEFT JOIN dbo.P02_Person p ON p.PersonId=g.PartyId LEFT JOIN dbo.P02_Person d ON d.PersonId=g.DriverId
LEFT JOIN dbo.m_Vehicle v ON v.VehicleId=g.VehicleId
OUTER APPLY(SELECT TOP (1) prior.CategoryId FROM dbo.t_GateEntry prior WHERE prior.InwardNo=g.InwardNo AND prior.CategoryId IS NOT NULL ORDER BY prior.GateEntryTime DESC) category
LEFT JOIN dbo.m_ItemCategory c ON c.CategoryId=COALESCE(g.CategoryId,category.CategoryId)
OUTER APPLY(SELECT CONVERT(datetime2,MAX(COALESCE(a.CreatedAt,CASE WHEN u.Status IN ('Unloaded','Verified') THEN u.UnloadTime END)) AT TIME ZONE CURRENT_TIMEZONE_ID() AT TIME ZONE 'India Standard Time') UnloadDateTime
FROM dbo.t_UnloadTransaction u LEFT JOIN dbo.t_SupervisorUnloadAction a ON a.UnloadId=u.UnloadId AND a.Status='Completed' WHERE u.RSTNumber=g.RSTNumber) unload
WHERE g.RSTNumber=@Rst;
SELECT COALESCE(b.BagTypeName,CONCAT('Bag type ',d.BagTypeId)) BagTypeName,SUM(CONVERT(bigint,d.BagCount)) BagCount
FROM dbo.t_UnloadTransaction u JOIN dbo.t_SupervisorUnloadAction a ON a.UnloadId=u.UnloadId AND a.Status='Completed'
JOIN dbo.t_SupervisorUnloadDetail d ON d.SupervisorActionId=a.SupervisorActionId LEFT JOIN dbo.m_BagType b ON b.BagTypeId=d.BagTypeId
WHERE u.RSTNumber=@Rst GROUP BY COALESCE(b.BagTypeName,CONCAT('Bag type ',d.BagTypeId));",con);
  cmd.Parameters.AddWithValue("@Rst",rst);con.Open();using var r=cmd.ExecuteReader();if(!r.Read())return null;
  string S(string k)=>r[k]==DBNull.Value?"":Convert.ToString(r[k])??"";
  DateTime? T(string k)=>r[k]==DBNull.Value?null:Convert.ToDateTime(r[k]);
  decimal? N(string k)=>r[k]==DBNull.Value?null:Convert.ToDecimal(r[k]);
  var slip=new WeighmentSlipModel{InwardId=Convert.ToInt32(r["InwardId"]),RstNumber=S("RSTNumber"),InwardNo=S("InwardNo"),InwardAt=T("GateInDateTime"),WeighedAt=T("GateEntryTime"),FinalAt=T("GateExitTime"),UnloadAt=T("UnloadDateTime"),Party=S("PartyName"),Vehicle=S("VehicleNumber"),Driver=S("DriverName"),DriverMobile=S("DriverMobile"),Category=S("CategoryName"),Gross=N("GrossWeight")??0,Tare=N("TareWeight"),Net=N("NetWeight"),Charge=N("WeighmentCharge")??0,Status=S("Status")};
  r.NextResult();while(r.Read())slip.Bags[S("BagTypeName")]=Convert.ToInt64(r["BagCount"]);return slip;
 }
}
