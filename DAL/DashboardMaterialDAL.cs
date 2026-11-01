using System.Data;
using Microsoft.Data.SqlClient;
using RiceMillProject.Models;
using RiceMillProject.BAL;
namespace RiceMillProject.DAL;
public class DashboardMaterialDAL(IConfiguration configuration)
{
 public DashboardMaterialPage Load(int officeId,DateTime from,DateTime to)
 {
  if(officeId<=0||from.Date>to.Date||to.Year>9998||(to.Date-from.Date).TotalDays>366)throw new ArgumentException("Select a valid company and date range (up to 366 days).");
  var page=new DashboardMaterialPage{FromDate=from.Date,ToDate=to.Date};
  using var con=new SqlConnection(configuration.GetConnectionString("DefaultConnection"));using var cmd=new SqlCommand(Query,con){CommandTimeout=90};
  cmd.Parameters.Add("@Office",SqlDbType.Int).Value=officeId;cmd.Parameters.Add("@From",SqlDbType.DateTime2).Value=from.Date;cmd.Parameters.Add("@Until",SqlDbType.DateTime2).Value=to.Date.AddDays(1);
  var ds=new DataSet();using var da=new SqlDataAdapter(cmd);da.Fill(ds);
  page.Categories=ds.Tables[0].AsEnumerable().Select(r=>new DashboardMaterialCategory{CategoryId=r.Field<int>("CategoryId"),CategoryName=r.Field<string>("CategoryName")??""}).ToList();
  foreach(var group in ds.Tables[1].AsEnumerable().GroupBy(r=>r.Field<string>("RSTNumber")))
  {
   var lots=group.Select(r=>new PurchaseLot{DetailId=r.Field<int>("DetailId"),BagCount=r.Field<int>("BagCount"),ActualWeightKg=r.Field<decimal?>("ActualWeightKg")}).ToList();
   PurchaseReportBuilder.Allocate(new PurchaseHeader{RSTNumber=group.Key!,NetWeight=group.First().Field<decimal?>("NetWeight")},lots,page.Notices);
   var weights=lots.ToDictionary(l=>l.DetailId);
   foreach(var r in group){var lot=weights[r.Field<int>("DetailId")];page.Stock.Add(new(){UnloadId=r.Field<int>("UnloadId"),RSTNumber=group.Key!,CategoryId=r.Field<int>("CategoryId"),CategoryName=r.Field<string>("CategoryName")??"Unknown category",ItemId=r.Field<int>("ItemId"),ItemName=r.Field<string>("ItemName")??"Unknown item",BagTypeId=r.Field<int>("BagTypeId"),BagTypeName=r.Field<string>("BagTypeName")??"",ReceivedBags=lot.BagCount,ReceivedKg=lot.WeightKg,Estimated=lot.Estimated,Direction=r.Field<int>("Direction")});}
  }
  var returns=ds.Tables[2].AsEnumerable().Select(r=>new DashboardReturnedStock(r.Field<int>("UnloadId"),r.Field<int>("BagTypeId"),Convert.ToDecimal(r["Bags"]),r.Field<decimal?>("WeightKg")));
  DashboardMaterialBuilder.ApplyReturns(page.Stock,returns,page.Notices);
  var charges=ds.Tables[3].AsEnumerable().Select(r=>new DashboardSavedCharge(r.Field<int>("UnloadId"),r.Field<string>("RSTNumber")!,r.Field<int>("BagTypeId"),r.Field<string>("BagTypeName")??"",r.Field<string>("WorkType")??"",Convert.ToDecimal(r["Bags"]),Convert.ToDecimal(r["Rate"]),Convert.ToDecimal(r["Amount"])));
  page.Charges=DashboardMaterialBuilder.AllocateCharges(page.Stock,charges);DashboardMaterialBuilder.Summarize(page);return page;
 }
 public const string Query="""
 SET NOCOUNT ON;
 SELECT CategoryId,CategoryName FROM dbo.m_ItemCategory WHERE IsActive=1 AND ISNULL(IsDeleted,0)=0 ORDER BY CategoryName;
 SELECT g.RSTNumber,u.UnloadId,d.SupervisorDetailId DetailId,d.CategoryId,c.CategoryName,d.ItemId,i.ItemName,d.BagTypeId,b.BagTypeName,d.BagCount,d.ActualWeightKg,g.NetWeight,
 CASE WHEN g.InwardOutward='Outward' THEN -1 ELSE 1 END Direction
 FROM dbo.t_GateEntry g JOIN dbo.t_UnloadTransaction u ON u.RSTNumber=g.RSTNumber
 CROSS APPLY(SELECT TOP(1) a.SupervisorActionId FROM dbo.t_SupervisorUnloadAction a WHERE a.UnloadId=u.UnloadId AND a.Status='Completed' AND ISNULL(a.IsDeleted,0)=0 AND ISNULL(a.IsActive,1)=1 AND a.CreatedAt<@Until ORDER BY a.SupervisorActionId DESC) a
 JOIN dbo.t_SupervisorUnloadDetail d ON d.SupervisorActionId=a.SupervisorActionId
 LEFT JOIN dbo.m_ItemCategory c ON c.CategoryId=d.CategoryId LEFT JOIN dbo.m_Item i ON i.ItemId=d.ItemId LEFT JOIN dbo.m_BagType b ON b.BagTypeId=d.BagTypeId
 WHERE g.TargetOfficeId=@Office AND g.GateEntryTime<@Until AND ISNULL(g.IsDeleted,0)=0 AND ISNULL(u.IsDeleted,0)=0 AND ISNULL(d.IsDeleted,0)=0 AND ISNULL(d.IsActive,1)=1;
 ;WITH Returned AS (
 SELECT r.UnloadId,m.BagTypeId,m.Bags,
 CASE WHEN d.NetWeight IS NOT NULL THEN CONVERT(decimal(19,6),d.NetWeight*m.Bags/NULLIF(SUM(CONVERT(decimal(19,6),m.Bags)) OVER(PARTITION BY d.DispatchId),0)) END WeightKg
 FROM dbo.tbl_OrderReturn r JOIN dbo.tbl_OrderReturnDispatch d ON d.OrderReturnId=r.OrderReturnId
 JOIN dbo.tbl_OrderReturnMovement m ON m.DispatchId=d.DispatchId AND m.MovementType='ReturnTruckLoading' AND m.Status='Completed'
 WHERE r.OfficeId=@Office AND d.GateOutAt IS NOT NULL AND d.GateOutAt<@Until AND ISNULL(r.IsDeleted,0)=0 AND ISNULL(d.IsDeleted,0)=0 AND ISNULL(m.IsDeleted,0)=0 AND ISNULL(m.IsActive,1)=1
 ) SELECT UnloadId,BagTypeId,SUM(CONVERT(decimal(19,4),Bags)) Bags,SUM(WeightKg) WeightKg FROM Returned GROUP BY UnloadId,BagTypeId;
 ;WITH Work AS (
 SELECT e.UnloadId,e.BagTypeId,e.WorkType,e.BagCount Bags,e.PerBagPrice Rate,e.TotalAmount Amount,e.CreatedAt WorkAt
 FROM dbo.t_SupervisorMethEntry e WHERE ISNULL(e.IsActive,1)=1 AND ISNULL(e.IsDeleted,0)=0 AND e.BagCount>0
 UNION ALL
 SELECT w.UnloadId,w.BagTypeId,w.WorkType,w.BagCount,w.PerBagCharge,w.PalledariAmount,w.AllocationTime
 FROM dbo.t_WorkerAllocation w WHERE ISNULL(w.IsActive,1)=1 AND ISNULL(w.IsDeleted,0)=0 AND w.BagCount>0
 AND NOT EXISTS(SELECT 1 FROM dbo.t_SupervisorMethEntry e WHERE e.UnloadId=w.UnloadId AND ISNULL(e.IsActive,1)=1 AND ISNULL(e.IsDeleted,0)=0 AND e.BagCount>0)
 UNION ALL
 SELECT r.UnloadId,m.BagTypeId,m.MovementType,w.BagsHandled,w.PerBagRate,w.Amount,w.CreatedAt
 FROM dbo.tbl_OrderReturnWorker w JOIN dbo.tbl_OrderReturnMovement m ON m.MovementId=w.MovementId JOIN dbo.tbl_OrderReturn r ON r.OrderReturnId=m.OrderReturnId
 WHERE r.OfficeId=@Office AND m.Status='Completed' AND ISNULL(w.IsDeleted,0)=0 AND ISNULL(w.IsActive,1)=1 AND ISNULL(m.IsDeleted,0)=0 AND ISNULL(r.IsDeleted,0)=0
 ) SELECT w.UnloadId,g.RSTNumber,w.BagTypeId,b.BagTypeName,w.WorkType,w.Bags,w.Rate,w.Amount
 FROM Work w JOIN dbo.t_UnloadTransaction u ON u.UnloadId=w.UnloadId JOIN dbo.t_GateEntry g ON g.RSTNumber=u.RSTNumber
 LEFT JOIN dbo.m_BagType b ON b.BagTypeId=w.BagTypeId
 WHERE g.TargetOfficeId=@Office AND ISNULL(g.IsDeleted,0)=0 AND ISNULL(u.IsDeleted,0)=0 AND w.WorkAt>=@From AND w.WorkAt<@Until;
 """;
}
