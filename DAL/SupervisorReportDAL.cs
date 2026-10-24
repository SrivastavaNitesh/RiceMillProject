using System.Data;
using Microsoft.Data.SqlClient;
using RiceMillProject.Models;
namespace RiceMillProject.DAL;
public class SupervisorReportDAL(IConfiguration configuration)
{
    public Dictionary<int,string> CategoryNames(int personId,int officeId,bool meth=false)
    {
        using var con=new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
        using var cmd=new SqlCommand(@"SELECT u.UnloadId,STRING_AGG(CONVERT(nvarchar(max),x.CategoryName),', ') CategoryNames
FROM dbo.t_UnloadTransaction u JOIN dbo.t_GateEntry g ON g.RSTNumber=u.RSTNumber
OUTER APPLY (SELECT DISTINCT c.CategoryName FROM dbo.t_SupervisorUnloadAction a
 JOIN dbo.t_SupervisorUnloadDetail d ON d.SupervisorActionId=a.SupervisorActionId
 JOIN dbo.m_ItemCategory c ON c.CategoryId=d.CategoryId WHERE a.UnloadId=u.UnloadId
 UNION SELECT c.CategoryName FROM dbo.m_ItemCategory c WHERE c.CategoryId=g.CategoryId
 AND NOT EXISTS(SELECT 1 FROM dbo.t_SupervisorUnloadAction a JOIN dbo.t_SupervisorUnloadDetail d ON d.SupervisorActionId=a.SupervisorActionId WHERE a.UnloadId=u.UnloadId)) x
WHERE ((@Meth=0 AND u.SupervisorId=@Person) OR (@Meth=1 AND u.MethId=@Person)) AND g.TargetOfficeId=@Office GROUP BY u.UnloadId;",con);
        cmd.Parameters.Add("@Meth",SqlDbType.Bit).Value=meth;
        cmd.Parameters.Add("@Person",SqlDbType.Int).Value=personId;cmd.Parameters.Add("@Office",SqlDbType.Int).Value=officeId;
        con.Open();using var reader=cmd.ExecuteReader();var names=new Dictionary<int,string>();while(reader.Read())names[reader.GetInt32(0)]=reader.IsDBNull(1)?"—":reader.GetString(1);return names;
    }
    public SupervisorLocationReport Load(int personId, int officeId, DateTime from, DateTime to, int? locationId, string? status, string? search)
    {
        var model = new SupervisorLocationReport { FromDate=from, ToDate=to, LocationId=locationId, Status=status, Search=search };
        using var con = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
        using var cmd = new SqlCommand(@"
SELECT DISTINCT l.LocationId,l.LocationName INTO #Locations
FROM dbo.m_LocationMaster l JOIN dbo.t_OfficeLocationMapping m ON m.LocationId=l.LocationId
WHERE m.OfficeId=@Office AND m.IsActive=1 AND l.IsActive=1 AND ISNULL(m.UnloadingAllowed,1)=1;

SELECT u.UnloadId,u.RSTNumber,u.UnloadTime,u.Status,u.LocationId,u.NumberOfBags,
 v.VehicleNumber,p.PersonName PartyName,m.PersonName MethName,i.ItemName,
 ge.GrossWeight,ge.TareWeight,ge.NetWeight,
 CAST(CASE WHEN sa.SupervisorActionId IS NOT NULL AND sa.Status='Completed' THEN 1 ELSE 0 END AS bit) FieldCompleted,
 CAST(ISNULL(wa.WorkerCompleted,0) AS bit) WorkerCompleted,
 sa.SupervisorActionId
INTO #Entries
FROM dbo.t_UnloadTransaction u
JOIN dbo.t_GateEntry ge ON ge.RSTNumber=u.RSTNumber
LEFT JOIN dbo.m_Vehicle v ON v.VehicleId=ge.VehicleId
LEFT JOIN dbo.P02_Person p ON p.PersonId=ge.PartyId
LEFT JOIN dbo.P02_Person m ON m.PersonId=u.MethId
LEFT JOIN dbo.m_Item i ON i.ItemId=u.ItemId
OUTER APPLY (SELECT TOP (1) a.SupervisorActionId,a.Status FROM dbo.t_SupervisorUnloadAction a WHERE a.UnloadId=u.UnloadId ORDER BY a.SupervisorActionId DESC) sa
OUTER APPLY (SELECT TOP (1) 1 WorkerCompleted FROM dbo.t_WorkerAllocation w WHERE w.UnloadId=u.UnloadId AND ISNULL(w.BagCount,0)>0) wa
WHERE u.SupervisorId=@Person AND ge.TargetOfficeId=@Office
 AND u.UnloadTime>=@From AND u.UnloadTime<@End
 AND (@Status='' OR (@Status='Completed' AND sa.Status='Completed' AND ISNULL(wa.WorkerCompleted,0)=1)
 OR (@Status='Pending' AND (ISNULL(sa.Status,'')<>'Completed' OR ISNULL(wa.WorkerCompleted,0)=0)))
 AND (@Search='' OR CHARINDEX(@Search,CONCAT(u.RSTNumber,' ',v.VehicleNumber,' ',p.PersonName,' ',m.PersonName))>0);

SELECT DISTINCT e.UnloadId,x.LocationId INTO #Assignment FROM #Entries e
CROSS APPLY (
 SELECT ul.LocationId FROM dbo.t_UnloadLocation ul WHERE ul.UnloadId=e.UnloadId AND ul.IsActive=1
 UNION SELECT e.LocationId WHERE e.LocationId IS NOT NULL
 UNION SELECT d.LocationId FROM dbo.t_SupervisorUnloadDetail d WHERE d.SupervisorActionId=e.SupervisorActionId AND d.LocationId IS NOT NULL
 UNION SELECT d.LocationId FROM dbo.t_SupervisorUnloadLocationDetail d WHERE d.SupervisorActionId=e.SupervisorActionId
) x JOIN #Locations l ON l.LocationId=x.LocationId;

SELECT * FROM #Locations ORDER BY LocationName;
SELECT e.*,a.LocationId ReportLocationId FROM #Entries e JOIN #Assignment a ON a.UnloadId=e.UnloadId ORDER BY e.UnloadTime DESC,e.UnloadId DESC;
SELECT e.UnloadId,e.RSTNumber,d.SupervisorDetailId DetailId,COALESCE(d.LocationId,e.LocationId) LocationId,d.CategoryId,d.ItemId,c.CategoryName,i.ItemName,b.BagTypeName,d.BagCount,d.ActualWeightKg
FROM #Entries e JOIN dbo.t_SupervisorUnloadDetail d ON d.SupervisorActionId=e.SupervisorActionId
LEFT JOIN dbo.m_ItemCategory c ON c.CategoryId=d.CategoryId
LEFT JOIN dbo.m_Item i ON i.ItemId=d.ItemId LEFT JOIN dbo.m_BagType b ON b.BagTypeId=d.BagTypeId;
SELECT e.UnloadId,d.LocationId,b.BagTypeName,d.BagCount FROM #Entries e
JOIN dbo.t_SupervisorUnloadLocationDetail d ON d.SupervisorActionId=e.SupervisorActionId
LEFT JOIN dbo.m_BagType b ON b.BagTypeId=d.BagTypeId;
SELECT e.UnloadId,p.PersonName WorkerName,w.WorkType,b.BagTypeName,w.BagCount,w.PerBagCharge,w.PalledariAmount
FROM #Entries e JOIN dbo.t_WorkerAllocation w ON w.UnloadId=e.UnloadId
LEFT JOIN dbo.P02_Person p ON p.PersonId=w.WorkerId LEFT JOIN dbo.m_BagType b ON b.BagTypeId=w.BagTypeId;
SELECT u.RSTNumber,d.SupervisorDetailId DetailId,d.BagCount,d.ActualWeightKg
FROM dbo.t_UnloadTransaction u JOIN dbo.t_SupervisorUnloadAction a ON a.UnloadId=u.UnloadId
JOIN dbo.t_SupervisorUnloadDetail d ON d.SupervisorActionId=a.SupervisorActionId
WHERE EXISTS(SELECT 1 FROM #Entries e WHERE e.RSTNumber=u.RSTNumber);
", con);
        cmd.Parameters.Add("@Person",SqlDbType.Int).Value=personId;
        cmd.Parameters.Add("@Office",SqlDbType.Int).Value=officeId;
        cmd.Parameters.Add("@From",SqlDbType.DateTime2).Value=from;
        cmd.Parameters.Add("@End",SqlDbType.DateTime2).Value=to.AddDays(1);
        cmd.Parameters.Add("@Status",SqlDbType.NVarChar,20).Value=status??"";
        cmd.Parameters.Add("@Search",SqlDbType.NVarChar,200).Value=(search??"").Trim();
        var data=new DataSet(); using var adapter=new SqlDataAdapter(cmd); adapter.Fill(data);
        model.Locations=data.Tables[0];model.Entries=data.Tables[1];model.Materials=data.Tables[2];model.LocationBags=data.Tables[3];model.Workers=data.Tables[4];
        model.Materials.Columns.Add("WeightKg",typeof(decimal));
        model.Materials.Columns.Add("Estimated",typeof(bool));
        foreach(var group in model.Materials.AsEnumerable().GroupBy(r=>r.Field<string>("RSTNumber")))
        {
            var header=model.Entries.AsEnumerable().FirstOrDefault(r=>r.Field<string>("RSTNumber")==group.Key);
            var lots=data.Tables[5].AsEnumerable().Where(r=>r.Field<string>("RSTNumber")==group.Key).Select(r=>new PurchaseLot{DetailId=r.Field<int>("DetailId"),BagCount=r.Field<int>("BagCount"),ActualWeightKg=r.Field<decimal?>("ActualWeightKg")}).ToList();
            RiceMillProject.BAL.PurchaseReportBuilder.Allocate(new PurchaseHeader{RSTNumber=group.Key!,NetWeight=header?.Field<decimal?>("NetWeight")},lots,model.Notices);
            foreach(var row in group){var lot=lots.First(l=>l.DetailId==row.Field<int>("DetailId"));row["WeightKg"]=(object?)lot.WeightKg??DBNull.Value;row["Estimated"]=lot.Estimated;}
        }
        return model;
    }
}
