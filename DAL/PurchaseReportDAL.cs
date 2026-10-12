using System.Data;
using Microsoft.Data.SqlClient;
using RiceMillProject.BAL;
using RiceMillProject.Models;

namespace RiceMillProject.DAL;

public class PurchaseReportDAL(IConfiguration configuration)
{
    private readonly string connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";

    public PurchasePage Load(PurchaseFilter filter)
    {
        using var con = new SqlConnection(connectionString);
        using var cmd = new SqlCommand(Query, con) { CommandTimeout = 90 };
        cmd.Parameters.Add("@From", SqlDbType.DateTime2).Value = filter.From!.Value.Date;
        cmd.Parameters.Add("@Until", SqlDbType.DateTime2).Value = filter.To!.Value.Date.AddDays(1);
        cmd.Parameters.Add("@Company", SqlDbType.Int).Value = (object?)filter.CompanyId ?? DBNull.Value;
        cmd.Parameters["@Company"].Direction = ParameterDirection.InputOutput;
        cmd.Parameters.Add("@Party", SqlDbType.Int).Value = filter.Mode == "Party" ? (object?)filter.PartyId ?? DBNull.Value : DBNull.Value;
        cmd.Parameters.Add("@AllDates", SqlDbType.Bit).Value = filter.Mode is "Locations" or "LocationDetails";
        using var adapter = new SqlDataAdapter(cmd);
        var data = new DataSet();
        adapter.Fill(data);
        filter.CompanyId = Convert.ToInt32(cmd.Parameters["@Company"].Value);
        var page = new PurchasePage
        {
            Filter = filter, Headers = Rows<PurchaseHeader>(data.Tables[0]),
            Companies = data.Tables[3].AsEnumerable().Select(r => new PurchaseOption(r.Field<int>("Id"), r.Field<string>("Name")!)).ToList(),
            Parties = data.Tables[4].AsEnumerable().Select(r => new PurchaseOption(r.Field<int>("Id"), r.Field<string>("Name")!)).ToList(),
            Locations = Rows<LocationMaster>(data.Tables[5]),
            BagTypes = data.Tables[6].AsEnumerable().Select(r => new PurchaseOption(r.Field<int>("Id"), r.Field<string>("Name")!)).ToList()
        };
        PurchaseReportBuilder.Build(page, Rows<PurchaseLot>(data.Tables[1]), Rows<PurchaseLab>(data.Tables[2]));
        return page;
    }

    private static List<T> Rows<T>(DataTable table) where T : new()
    {
        var properties = typeof(T).GetProperties().Where(p => p.CanWrite && table.Columns.Contains(p.Name)).ToArray();
        return table.AsEnumerable().Select(row =>
        {
            var model = new T();
            foreach (var p in properties)
                if (!row.IsNull(p.Name)) p.SetValue(model, Convert.ChangeType(row[p.Name], Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType));
            return model;
        }).ToList();
    }

    // Materialize the filtered RST set once so every result set has the same scope.
    internal const string Query = """
        SET NOCOUNT ON;
        IF @Company IS NULL
          SELECT @Company=OfficeId FROM dbo.o05_office WHERE OfficeName=N'Main Mill' AND OfficeType=1 AND IsActive=1;
        IF @Company IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.o05_office WHERE OfficeId=@Company AND OfficeType=1)
          THROW 50001,'Select a valid company. Main Mill must be configured for the default report.',1;
        SELECT g.* INTO #PurchaseRst FROM dbo.t_GateEntry g
        WHERE ISNULL(g.InwardOutward,'Inward')='Inward'
          AND (@AllDates=1 OR (g.GateEntryTime>=@From AND g.GateEntryTime<@Until))
          AND (@Company IS NULL OR g.TargetOfficeId=@Company)
          AND (@Party IS NULL OR g.PartyId=@Party);

        SELECT g.RSTNumber,g.InwardNo,g.PartyId,ISNULL(p.PersonName,'') PartyName,ISNULL(p.MobileNumber,'') PartyMobile,
          ISNULL(d.PersonName,'') DriverName,ISNULL(v.VehicleNumber,'') VehicleNumber,
          ISNULL(g.TargetOfficeId,0) CompanyId,ISNULL(o.OfficeName,'Unassigned company') CompanyName,
          g.GateEntryTime WeighingDate,g.GrossWeight,g.TareWeight,g.NetWeight
        FROM #PurchaseRst g LEFT JOIN dbo.P02_Person p ON p.PersonId=g.PartyId
        LEFT JOIN dbo.P02_Person d ON d.PersonId=g.DriverId LEFT JOIN dbo.m_Vehicle v ON v.VehicleId=g.VehicleId
        LEFT JOIN dbo.o05_office o ON o.OfficeId=g.TargetOfficeId
        ORDER BY o.OfficeName,g.GateEntryTime,g.RSTNumber;

        SELECT g.RSTNumber,sd.SupervisorDetailId DetailId,u.UnloadId,
          COALESCE(sd.LocationId,oneLocation.LocationId) LocationId,
          ISNULL(l.LocationName,'Unassigned') LocationName,sd.ActualWeightKg,sd.CategoryId,c.CategoryName,
          sd.ItemId,i.ItemName,sd.BagTypeId,b.BagTypeName,sd.BagCount,b.DeductionWeightGrams,
          ISNULL(s.PersonName,'') SupervisorName,ISNULL(m.PersonName,'') MethName
        INTO #PurchaseLots
        FROM #PurchaseRst g JOIN dbo.t_UnloadTransaction u ON u.RSTNumber=g.RSTNumber AND u.Status IN ('Unloaded','Verified')
        JOIN dbo.t_SupervisorUnloadAction sa ON sa.UnloadId=u.UnloadId AND sa.Status='Completed'
        JOIN dbo.t_SupervisorUnloadDetail sd ON sd.SupervisorActionId=sa.SupervisorActionId
        JOIN dbo.m_Item i ON i.ItemId=sd.ItemId JOIN dbo.m_ItemCategory c ON c.CategoryId=sd.CategoryId
        JOIN dbo.m_BagType b ON b.BagTypeId=sd.BagTypeId
        LEFT JOIN dbo.P02_Person s ON s.PersonId=sa.SupervisorId LEFT JOIN dbo.P02_Person m ON m.PersonId=u.MethId
        OUTER APPLY (SELECT CASE WHEN COUNT(*)=1 THEN MIN(x.LocationId) END LocationId FROM
          (SELECT u.LocationId WHERE u.LocationId IS NOT NULL UNION SELECT ul.LocationId FROM dbo.t_UnloadLocation ul WHERE ul.UnloadId=u.UnloadId AND ul.IsActive=1) x) oneLocation
        LEFT JOIN dbo.m_LocationMaster l ON l.LocationId=COALESCE(sd.LocationId,oneLocation.LocationId);
        SELECT * FROM #PurchaseLots ORDER BY RSTNumber,DetailId;

        ;WITH TestKeys AS (
          SELECT DISTINCT p.RSTNumber,p.CategoryId,p.ItemId,m.TestId FROM #PurchaseLots p
          JOIN dbo.m_ItemLabTest m ON m.CategoryId=p.CategoryId AND m.ItemId=p.ItemId AND m.IsActive=1
          JOIN dbo.m_LabTestMaster t ON t.TestId=m.TestId AND t.IsActive=1
          UNION
          SELECT DISTINCT p.RSTNumber,p.CategoryId,p.ItemId,r.TestId FROM #PurchaseLots p
          JOIN dbo.t_LabReport h ON h.RSTNumber=p.RSTNumber
          JOIN dbo.t_LabReportResult r ON r.ReportId=h.ReportId AND r.ItemId=p.ItemId AND r.CategoryId=p.CategoryId
        )
        SELECT k.*,COALESCE(r.TestName,t.TestName,'Test') TestName,t.Deducations MasterDeduction,
          ISNULL(un.UnitCode,'') MasterUnit,r.ReportId,r.ResultValue,ISNULL(r.TechnicianName,'') RecordedBy,
          br.RuleId,br.Threshold,br.DeductionMode,br.DeductionValue
        FROM TestKeys k LEFT JOIN dbo.m_LabTestMaster t ON t.TestId=k.TestId
        LEFT JOIN dbo.tbl_Masterofunit un ON un.UnitId=t.Deducationsunitid
        OUTER APPLY (SELECT TOP(1) h.ReportId,h.TechnicianName,rr.ResultValue,rr.TestName FROM dbo.t_LabReport h
          JOIN dbo.t_LabReportResult rr ON rr.ReportId=h.ReportId
          WHERE h.RSTNumber=k.RSTNumber AND rr.ItemId=k.ItemId AND rr.CategoryId=k.CategoryId AND rr.TestId=k.TestId
          ORDER BY h.TestedAt DESC,h.ReportId DESC,rr.ResultId DESC) r
        LEFT JOIN dbo.m_BillingLabRule br ON br.CategoryId=k.CategoryId AND br.TestId=k.TestId AND br.IsActive=1;

        SELECT OfficeId Id,OfficeName Name FROM dbo.o05_office WHERE OfficeType=1 ORDER BY OfficeName;
        SELECT DISTINCT p.PersonId Id,p.PersonName+' ('+ISNULL(p.MobileNumber,'')+')' Name
        FROM dbo.P02_Person p JOIN dbo.t_GateEntry g ON g.PartyId=p.PersonId
        WHERE ISNULL(g.InwardOutward,'Inward')='Inward' ORDER BY Name;
        SELECT l.LocationId,l.LocationName,l.Capacity,l.CapacityUnit,l.IsActive FROM dbo.m_LocationMaster l
        WHERE @Company IS NULL OR EXISTS(SELECT 1 FROM dbo.t_OfficeLocationMapping m WHERE m.LocationId=l.LocationId AND m.OfficeId=@Company)
          OR EXISTS(SELECT 1 FROM #PurchaseLots p WHERE p.LocationId=l.LocationId)
        ORDER BY l.LocationName;
        SELECT BagTypeId Id,BagTypeName Name FROM dbo.m_BagType ORDER BY BagTypeName;
        """;
}
