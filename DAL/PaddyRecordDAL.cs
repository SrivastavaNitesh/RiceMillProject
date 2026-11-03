using System.Data;
using Microsoft.Data.SqlClient;
using RiceMillProject.Models;
using RiceMillProject.BAL;
namespace RiceMillProject.DAL;
public class PaddyRecordDAL(IConfiguration configuration)
{
 public PaddyRecordPage Load(int supervisorId,int officeId,PaddyRecordFilter filter,string category="Paddy")
 {
  if(supervisorId<=0||officeId<=0||filter.FromDate==null||filter.ToDate==null||filter.FromDate>filter.ToDate||filter.FromDate.Value.Year<1900||filter.ToDate.Value.Year>9998)throw new ArgumentException("Select a valid supervisor, company and date range.");
  if(category is not ("Paddy" or "Rice"))throw new ArgumentException("Unknown record category.");
  var page=new PaddyRecordPage{Filter=filter,CategoryName=category};
  using var con=new SqlConnection(configuration.GetConnectionString("DefaultConnection"));using var cmd=new SqlCommand(Query,con){CommandTimeout=90};
  cmd.Parameters.Add("@Category",SqlDbType.NVarChar,10).Value=category;
  cmd.Parameters.Add("@Supervisor",SqlDbType.Int).Value=supervisorId;cmd.Parameters.Add("@Office",SqlDbType.Int).Value=officeId;
  cmd.Parameters.Add("@From",SqlDbType.DateTime2).Value=filter.FromDate.Value.Date;cmd.Parameters.Add("@Until",SqlDbType.DateTime2).Value=filter.ToDate.Value.Date.AddDays(1);
  var ds=new DataSet();using var adapter=new SqlDataAdapter(cmd);adapter.Fill(ds);
  if(ds.Tables[0].Rows.Count>0){var r=ds.Tables[0].Rows[0];page.SupervisorName=Text(r,"SupervisorName");page.OfficeName=Text(r,"OfficeName");page.HasPaddyCategory=Convert.ToInt32(r["PaddyCategories"])>0;}
  page.Items=ds.Tables[1].AsEnumerable().Select(r=>new PaddyRecordOption(Convert.ToInt32(r["Id"]),Text(r,"Name"))).ToList();
  page.Locations=ds.Tables[2].AsEnumerable().Select(r=>new PaddyRecordOption(Convert.ToInt32(r["Id"]),Text(r,"Name"))).ToList();
  var assignments=ds.Tables[3].AsEnumerable().Select(r=>new PaddyRecordAssignment(Convert.ToInt32(r["UnloadId"]),Text(r,"RSTNumber"),Text(r,"VehicleNumber"),Text(r,"PartyName"),Text(r,"MethName"),Convert.ToBoolean(r["GateIsPaddy"]),Number(r,"StackPP"),Number(r,"StackJute"),Number(r,"HaudiPP"),Number(r,"HaudiJute"),Number(r,"SupervisorTotalBags"),r.IsNull("NetWeight")?null:Convert.ToDecimal(r["NetWeight"])));
  var lots=ds.Tables[4].AsEnumerable().Select(r=>new PaddyRecordLot(Convert.ToInt32(r["UnloadId"]),Convert.ToInt32(r["ItemId"]),Text(r,"ItemName"),Convert.ToInt32(r["LocationId"]),Text(r,"LocationName"),Number(r,"BagCount"),Convert.ToBoolean(r["IsPaddy"]),Convert.ToInt32(r["BagTypeId"]),Text(r,"BagTypeName")));
  page.BagTypes=ds.Tables[5].AsEnumerable().Select(r=>new PaddyRecordOption(Convert.ToInt32(r["Id"]),Text(r,"Name"))).ToList();
  var savedLocations=ds.Tables[6].AsEnumerable().Select(r=>new PaddyRecordLocationLot(Convert.ToInt32(r["UnloadId"]),Convert.ToInt32(r["LocationId"]),Text(r,"LocationName"),Convert.ToInt32(r["BagTypeId"]),Text(r,"BagTypeName"),Number(r,"BagCount")));
  page.Rows=PaddyRecordBuilder.Build(assignments,lots,savedLocations);PaddyRecordBuilder.ConfigureColumns(page);return page;
 }
 private static string Text(DataRow row,string col)=>row.IsNull(col)?"":Convert.ToString(row[col])?.Trim()??"";
 private static long Number(DataRow row,string col)=>row.IsNull(col)?0:Convert.ToInt64(row[col]);
 public const string Query="""
 SET NOCOUNT ON;
 SELECT CategoryId INTO #PaddyCategories FROM dbo.m_ItemCategory
 WHERE ISNULL(IsDeleted,0)=0 AND ((@Category=N'Paddy' AND UPPER(LTRIM(RTRIM(CategoryName))) IN (N'PADDY',N'DHAN',N'धान')) OR (@Category=N'Rice' AND UPPER(LTRIM(RTRIM(CategoryName))) IN (N'RICE',N'CHAWAL',N'चावल')));
 SELECT u.UnloadId,u.RSTNumber,u.LocationId,u.UnloadTime,g.VehicleId,g.PartyId,g.NetWeight,
 CAST(CASE WHEN EXISTS(SELECT 1 FROM #PaddyCategories c WHERE c.CategoryId=g.CategoryId) THEN 1 ELSE 0 END AS bit) GateIsPaddy,
 sa.SupervisorActionId,sa.StackPP,sa.StackJute,sa.HaudiPP,sa.HaudiJute,sa.SupervisorTotalBags,u.MethId
 INTO #Scope FROM dbo.t_UnloadTransaction u JOIN dbo.t_GateEntry g ON g.RSTNumber=u.RSTNumber
 OUTER APPLY(SELECT TOP(1) a.SupervisorActionId,a.StackPP,a.StackJute,a.HaudiPP,a.HaudiJute,a.SupervisorTotalBags FROM dbo.t_SupervisorUnloadAction a
 WHERE a.UnloadId=u.UnloadId AND a.Status='Completed' AND ISNULL(a.IsActive,1)=1 AND ISNULL(a.IsDeleted,0)=0 ORDER BY a.SupervisorActionId DESC) sa
 WHERE u.SupervisorId=@Supervisor AND g.TargetOfficeId=@Office AND u.UnloadTime>=@From AND u.UnloadTime<@Until
 AND ISNULL(u.IsDeleted,0)=0 AND ISNULL(u.IsActive,1)=1 AND ISNULL(g.IsDeleted,0)=0;
 SELECT s.UnloadId,d.ItemId,ISNULL(i.ItemName,N'Unknown item') ItemName,COALESCE(d.LocationId,oneLocation.LocationId,0) LocationId,
 ISNULL(l.LocationName,N'Unspecified location') LocationName,d.BagCount,d.BagTypeId,ISNULL(b.BagTypeName,N'Unknown bag type') BagTypeName,
 CAST(CASE WHEN c.CategoryId IS NULL THEN 0 ELSE 1 END AS bit) IsPaddy
 INTO #Lots FROM #Scope s JOIN dbo.t_SupervisorUnloadDetail d ON d.SupervisorActionId=s.SupervisorActionId
 LEFT JOIN dbo.m_BagType b ON b.BagTypeId=d.BagTypeId
 LEFT JOIN dbo.m_Item i ON i.ItemId=d.ItemId LEFT JOIN #PaddyCategories c ON c.CategoryId=d.CategoryId
 OUTER APPLY(SELECT CASE WHEN COUNT(*)=1 THEN MIN(x.LocationId) END LocationId FROM
 (SELECT s.LocationId WHERE s.LocationId IS NOT NULL UNION SELECT ul.LocationId FROM dbo.t_UnloadLocation ul WHERE ul.UnloadId=s.UnloadId AND ul.IsActive=1 AND ISNULL(ul.IsDeleted,0)=0) x) oneLocation
 LEFT JOIN dbo.m_LocationMaster l ON l.LocationId=COALESCE(d.LocationId,oneLocation.LocationId)
 WHERE ISNULL(d.IsDeleted,0)=0 AND ISNULL(d.IsActive,1)=1;
 SELECT s.UnloadId,d.LocationId,ISNULL(l.LocationName,N'Unspecified location') LocationName,d.BagTypeId,ISNULL(b.BagTypeName,N'Unknown bag type') BagTypeName,d.BagCount
 INTO #ActualLocations FROM #Scope s JOIN dbo.t_SupervisorUnloadLocationDetail d ON d.SupervisorActionId=s.SupervisorActionId
 LEFT JOIN dbo.m_LocationMaster l ON l.LocationId=d.LocationId LEFT JOIN dbo.m_BagType b ON b.BagTypeId=d.BagTypeId
 WHERE ISNULL(d.IsDeleted,0)=0 AND ISNULL(d.IsActive,1)=1;
 SELECT (SELECT PersonName FROM dbo.P02_Person WHERE PersonId=@Supervisor) SupervisorName,
 (SELECT OfficeName FROM dbo.o05_office WHERE OfficeId=@Office) OfficeName,(SELECT COUNT(*) FROM #PaddyCategories) PaddyCategories;
 SELECT x.Id,LTRIM(RTRIM(x.Name)) Name FROM (
 SELECT i.ItemId Id,i.ItemName Name FROM dbo.m_Item i JOIN #PaddyCategories c ON c.CategoryId=i.CategoryId WHERE i.IsActive=1 AND ISNULL(i.IsDeleted,0)=0
 UNION SELECT ItemId,ItemName FROM #Lots WHERE IsPaddy=1) x ORDER BY Name,Id;
 SELECT x.Id,LTRIM(RTRIM(x.Name)) Name FROM (
 SELECT l.LocationId Id,l.LocationName Name FROM dbo.m_LocationMaster l WHERE l.IsActive=1 AND ISNULL(l.IsDeleted,0)=0
 AND EXISTS(SELECT 1 FROM dbo.t_OfficeLocationMapping m WHERE m.OfficeId=@Office AND m.LocationId=l.LocationId AND m.IsActive=1 AND ISNULL(m.IsDeleted,0)=0)
 UNION SELECT LocationId,LocationName FROM #Lots WHERE IsPaddy=1 UNION SELECT LocationId,LocationName FROM #ActualLocations) x ORDER BY Name,Id;
 SELECT s.*,v.VehicleNumber,p.PersonName PartyName,m.PersonName MethName FROM #Scope s
 LEFT JOIN dbo.m_Vehicle v ON v.VehicleId=s.VehicleId LEFT JOIN dbo.P02_Person p ON p.PersonId=s.PartyId LEFT JOIN dbo.P02_Person m ON m.PersonId=s.MethId
 ORDER BY s.UnloadTime,s.RSTNumber,s.UnloadId;
 SELECT * FROM #Lots;
 SELECT x.Id,LTRIM(RTRIM(x.Name)) Name FROM (SELECT BagTypeId Id,BagTypeName Name FROM dbo.m_BagType WHERE IsActive=1 AND ISNULL(IsDeleted,0)=0 UNION SELECT BagTypeId,BagTypeName FROM #Lots WHERE IsPaddy=1 UNION SELECT BagTypeId,BagTypeName FROM #ActualLocations) x ORDER BY Name,Id;
 SELECT * FROM #ActualLocations;
 """;
}
