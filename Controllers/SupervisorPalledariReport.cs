using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
namespace RiceMillProject.Controllers;
public partial class UnloadController
{
 [HttpGet]
 public IActionResult PalledariReport(DateTime? fromDate,DateTime? toDate,string? search)
 {
  if(!ModelState.IsValid||fromDate>toDate||toDate?.Date==DateTime.MaxValue.Date||(search?.Length??0)>200)return BadRequest("Select valid report filters.");
  if(GetCurrentPersonId()<=0||GetCurrentOfficeId()<=0)return Forbid();
  ViewBag.FromDate=fromDate?.ToString("yyyy-MM-dd");ViewBag.ToDate=toDate?.ToString("yyyy-MM-dd");ViewBag.Search=search;
  using var con=new SqlConnection(HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection"));
  using var cmd=new SqlCommand(@"
;WITH WorkRows AS (
 SELECT e.UnloadId,e.MethId,e.WorkType,e.BagTypeId,e.BagCount,
        e.PerBagPrice PerBagCharge,e.TotalAmount PalledariAmount,e.CreatedAt WorkAt
 FROM dbo.t_SupervisorMethEntry e
 WHERE ISNULL(e.IsActive,1)=1 AND ISNULL(e.IsDeleted,0)=0 AND e.BagCount>0
 UNION ALL
 SELECT w.UnloadId,u.MethId,w.WorkType,w.BagTypeId,w.BagCount,
        w.PerBagCharge,w.PalledariAmount,w.AllocationTime WorkAt
 FROM dbo.t_WorkerAllocation w
 JOIN dbo.t_UnloadTransaction u ON u.UnloadId=w.UnloadId
 WHERE ISNULL(w.IsActive,1)=1 AND ISNULL(w.IsDeleted,0)=0 AND w.BagCount>0
   AND NOT EXISTS (SELECT 1 FROM dbo.t_SupervisorMethEntry e WHERE e.UnloadId=w.UnloadId
       AND ISNULL(e.IsActive,1)=1 AND ISNULL(e.IsDeleted,0)=0 AND e.BagCount>0)
)
SELECT u.RSTNumber,u.UnloadTime,m.PersonName MethName,s.PersonName SupervisorName,
 material.CategoryNames,material.ItemNames,locations.LocationNames,b.BagTypeName,w.WorkType,w.PerBagCharge,
 SUM(CONVERT(bigint,w.BagCount)) BagCount,SUM(CONVERT(decimal(19,2),w.PalledariAmount)) TotalPalledari
FROM dbo.t_UnloadTransaction u
LEFT JOIN dbo.t_GateEntry g ON g.RSTNumber=u.RSTNumber
JOIN WorkRows w ON w.UnloadId=u.UnloadId
LEFT JOIN dbo.P02_Person m ON m.PersonId=w.MethId
LEFT JOIN dbo.P02_Person s ON s.PersonId=u.SupervisorId
LEFT JOIN dbo.m_BagType b ON b.BagTypeId=w.BagTypeId
OUTER APPLY(SELECT TOP(1) a.SupervisorActionId FROM dbo.t_SupervisorUnloadAction a WHERE a.UnloadId=u.UnloadId ORDER BY a.SupervisorActionId DESC) sa
OUTER APPLY(SELECT
 (SELECT STRING_AGG(CONVERT(nvarchar(max),x.CategoryName),', ') FROM (SELECT DISTINCT c.CategoryName FROM dbo.t_SupervisorUnloadDetail d JOIN dbo.m_ItemCategory c ON c.CategoryId=d.CategoryId WHERE d.SupervisorActionId=sa.SupervisorActionId) x) CategoryNames,
 (SELECT STRING_AGG(CONVERT(nvarchar(max),x.ItemName),', ') FROM (SELECT DISTINCT i.ItemName FROM dbo.t_SupervisorUnloadDetail d JOIN dbo.m_Item i ON i.ItemId=d.ItemId WHERE d.SupervisorActionId=sa.SupervisorActionId) x) ItemNames) material
OUTER APPLY(SELECT STRING_AGG(CONVERT(nvarchar(max),x.LocationName),', ') LocationNames FROM (
 SELECT DISTINCT l.LocationName FROM dbo.m_LocationMaster l WHERE l.LocationId IN (
 SELECT d.LocationId FROM dbo.t_SupervisorUnloadDetail d WHERE d.SupervisorActionId=sa.SupervisorActionId
 UNION SELECT d.LocationId FROM dbo.t_SupervisorUnloadLocationDetail d WHERE d.SupervisorActionId=sa.SupervisorActionId
 UNION SELECT ul.LocationId FROM dbo.t_UnloadLocation ul WHERE ul.UnloadId=u.UnloadId AND ul.IsActive=1
 UNION SELECT u.LocationId)) x) locations
WHERE u.SupervisorId=@Person AND (g.TargetOfficeId=@Office OR (g.RSTNumber IS NULL AND EXISTS(SELECT 1 FROM dbo.sa05_user owner WHERE owner.PersonId=u.SupervisorId AND owner.OfficeId=@Office AND owner.IsActive=1))) AND ISNULL(w.BagCount,0)>0
 AND (@From IS NULL OR COALESCE(w.WorkAt,u.UnloadTime)>=@From) AND (@End IS NULL OR COALESCE(w.WorkAt,u.UnloadTime)<@End)
 AND (@Search='' OR CHARINDEX(@Search,CONCAT(u.RSTNumber,' ',m.PersonName,' ',material.ItemNames,' ',locations.LocationNames))>0)
GROUP BY u.UnloadId,u.RSTNumber,u.UnloadTime,m.PersonName,s.PersonName,material.CategoryNames,material.ItemNames,locations.LocationNames,b.BagTypeName,w.WorkType,w.PerBagCharge
ORDER BY u.UnloadTime DESC,u.RSTNumber,b.BagTypeName,w.WorkType,w.PerBagCharge;",con);
  cmd.Parameters.Add("@Person",SqlDbType.Int).Value=GetCurrentPersonId();cmd.Parameters.Add("@Office",SqlDbType.Int).Value=GetCurrentOfficeId();
  cmd.Parameters.Add("@From",SqlDbType.DateTime2).Value=(object?)fromDate?.Date??DBNull.Value;
  cmd.Parameters.Add("@End",SqlDbType.DateTime2).Value=(object?)toDate?.Date.AddDays(1)??DBNull.Value;
  cmd.Parameters.Add("@Search",SqlDbType.NVarChar,200).Value=(search??"").Trim();
  var rows=new DataTable();using var adapter=new SqlDataAdapter(cmd);adapter.Fill(rows);return View(rows);
 }
}
