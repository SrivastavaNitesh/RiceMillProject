using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc.Rendering;
using RiceMillProject.Models;

namespace RiceMillProject.DAL;

public class RecordEditDAL(IConfiguration configuration)
{
    private readonly string connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
    public static readonly string[] Modules = ["GateInward", "Weight", "Assignment", "Supervisor", "Labour"];
    public bool IsAdministrator(int userId)
    {
        using var c=Connect();
        return Read(c,null,@"SELECT 1 FROM dbo.sa05_user u JOIN dbo.P02_Person p ON p.PersonId=u.PersonId AND p.IsActive=1
            JOIN dbo.P11_PersonDesignatation pd ON pd.P02_PersonId=u.PersonId AND pd.IsActive=1
            JOIN dbo.O12_Designatation d ON d.DesignationId=pd.O12_DesignationId AND d.IsActive=1
            JOIN dbo.O10_Post post ON post.PostId=d.O10_PostId AND post.IsActive=1
            WHERE u.UserId=@User AND u.IsActive=1 AND post.PostId=4",("@User",userId)).Rows.Count>0;
    }
    public HashSet<int> VisibleInwardIds(int userId,bool weightReport,bool supervisorReport=false)
    {
        using var c=Connect();
        return Read(c,null,@"SELECT h.InwardId FROM dbo.t_InwardHeader h JOIN dbo.sa05_user owner ON owner.UserId=h.CreatedBy
            JOIN dbo.sa05_user actor ON actor.UserId=@User AND actor.IsActive=1
            WHERE (@SupervisorReport=1 AND owner.OfficeId=actor.OfficeId AND EXISTS(
                SELECT 1 FROM dbo.t_GateEntry g JOIN dbo.t_UnloadTransaction u ON u.RSTNumber=g.RSTNumber
                WHERE g.InwardNo=h.InwardNo AND g.TargetOfficeId=actor.OfficeId AND u.SupervisorId=actor.PersonId))
                OR (@SupervisorReport=0 AND ((@WeightReport=1 AND owner.OfficeId=actor.OfficeId) OR h.CreatedBy=@User))",
            ("@User",userId),("@WeightReport",weightReport),("@SupervisorReport",supervisorReport)).AsEnumerable().Select(r=>Convert.ToInt32(r[0])).ToHashSet();
    }
    private SqlConnection Connect() { var c = new SqlConnection(connectionString); RecordEditContext.Open(c); return c; }
    private static SqlCommand Cmd(SqlConnection c, SqlTransaction? tx, string sql, params (string name, object? value)[] args)
    {
        var cmd = new SqlCommand(sql, c, tx);
        foreach (var (name, value) in args) cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return cmd;
    }
    private static DataTable Read(SqlConnection c, SqlTransaction? tx, string sql, params (string, object?)[] args)
    { using var cmd = Cmd(c, tx, sql, args); using var da = new SqlDataAdapter(cmd); var t = new DataTable(); da.Fill(t); return t; }
    private static int Run(SqlConnection c, SqlTransaction? tx, string sql, params (string, object?)[] args)
    { using var cmd = Cmd(c, tx, sql, args); return cmd.ExecuteNonQuery(); }
    private static string S(DataRow r, string name) => Convert.ToString(r[name], CultureInfo.InvariantCulture) ?? "";
    private static RecordEditRow Row(DataRow r) => new()
    {
        ModuleKey = S(r, "ModuleKey"), RecordId = S(r, "RecordId"), ReferenceNo = S(r, "ReferenceNo"),
        DriverName = r.Table.Columns.Contains("DriverName") ? S(r,"DriverName") : "", VehicleNumber = S(r, "VehicleNumber"), PartyName = S(r, "PartyName"), EntryAt = Convert.ToDateTime(r["OriginalEntryAt"]),
        ExpiresAt = r.IsNull("ExpiresAt") ? null : Convert.ToDateTime(r["ExpiresAt"]), CanEdit = Convert.ToBoolean(r["CanEdit"]),
        IsFinalized = Convert.ToBoolean(r["IsFinalized"]), OutwardComplete = Convert.ToBoolean(r["OutwardComplete"]),
        IsAdmin = Convert.ToBoolean(r["IsAdmin"]), Version = Convert.ToBase64String((byte[])r["Version"]),
        PolicyId = Convert.ToInt32(r["PolicyId"]), PolicyName = S(r, "PolicyName")
    };
    public List<RecordEditRow> List(int userId, string? module = null, DateTime? from = null, DateTime? to = null)
    {
        using var c = Connect();
        return Read(c, null, @"SELECT a.*,CASE WHEN a.ModuleKey='GateInward' THEN (SELECT h.DriverName FROM dbo.t_InwardHeader h WHERE h.InwardId=TRY_CONVERT(int,a.RecordId)) ELSE N'' END DriverName FROM dbo.t_RecordEditState s
            CROSS APPLY dbo.fn_RecordEditAccess(@User,s.ModuleKey,s.RecordId) a
            WHERE (@Module IS NULL OR a.ModuleKey=@Module)
              AND (@From IS NULL OR a.OriginalEntryAt>=@From)
              AND (@To IS NULL OR a.OriginalEntryAt<DATEADD(day,1,@To))
            ORDER BY a.OriginalEntryAt DESC,a.RecordId DESC", ("@User", userId), ("@Module", module), ("@From", from?.Date), ("@To", to?.Date))
            .AsEnumerable().Select(Row).ToList();
    }
    public RecordEditRow? Access(int userId, string module, string id)
    {
        using var c = Connect();
        return Read(c, null, "SELECT * FROM dbo.fn_RecordEditAccess(@User,@Module,@Id)", ("@User", userId), ("@Module", module), ("@Id", id))
            .AsEnumerable().Select(Row).FirstOrDefault();
    }
    public bool Exists(string module, string id)
    {
        using var c = Connect();
        return Read(c, null, "SELECT 1 FROM dbo.t_RecordEditState WHERE ModuleKey=@Module AND RecordId=@Id", ("@Module", module), ("@Id", id)).Rows.Count > 0;
    }
    private static (string table, string key, EditField[] fields, string? detailTable, string? detailKey, EditField[] detailFields) Spec(string module) => module switch
    {
        "GateInward" => ("t_InwardHeader", "InwardId", [new("GateId", "Gate", "select", true),new("InwardTypeId", "Inward type", "select", true),
            new("PartyId", "Party / supplier", "select", true),new("VehicleNo", "Vehicle number", "text", true, 50),new("VehicleTypeId", "Vehicle type", "select", true),
            new("DriverName", "Driver name", "text", true),new("DriverMobile", "Driver mobile", "tel", false, 15),new("TransporterName", "Transporter"),
            new("ChallanNo", "Challan number", "text", false, 100),new("ApproxNoOfBags", "Approximate bags", "integer", true),
            new("ApproxWeight", "Approximate weight (kg)", "number", true),new("PurposeRemarks", "Remarks", "text", false, 250)], null, null, []),
        "Weight" => ("t_GateEntry", "RSTNumber", [new("GrossWeight", "Gross weight (kg)", "number", true),new("TareWeight", "Tare weight (kg)", "number"),
            new("WeighmentCharge", "Weighment charge", "number", true),new("PartyId", "Party / supplier", "select", true),new("VehicleId", "Vehicle", "select", true),
            new("DriverId", "Driver", "select", true),new("CategoryId", "Item category", "select", true)],null,null,[]),
        "Assignment" => ("t_UnloadTransaction", "UnloadId", [new("MethId", "Assigned Meth", "select", true),new("LocationId", "Default unloading location", "select", true)],null,null,[]),
        "Supervisor" => ("t_SupervisorUnloadAction", "UnloadId", [],"t_SupervisorUnloadDetail","SupervisorActionId",
            [new("CategoryId", "Category", "select", true),new("ItemId", "Item / variety", "select", true),new("LocationId", "Location", "select", true),
             new("BagTypeId", "Bag type", "select", true),new("BagCount", "Bags", "integer", true),new("ActualWeightKg", "Actual weight (kg)", "number")]),
        "Labour" => ("t_UnloadTransaction", "UnloadId", [],"t_WorkerAllocation","UnloadId",
            [new("WorkerId", "Worker", "select", true),new("WorkType", "Work type", "select", true),new("BagTypeId", "Bag type", "select", true),
             new("BagCount", "Bags", "integer", true),new("PerBagCharge", "Charge / bag", "number", true)]),
        _ => throw new ArgumentException("Unknown record category.")
    };
    public RecordEditForm Load(int userId, string module, string id)
    {
        var access = Access(userId, module, id) ?? throw new UnauthorizedAccessException();
        var spec = Spec(module);
        using var c = Connect();
        var table = Read(c, null, $"SELECT * FROM dbo.[{spec.table}] WHERE [{spec.key}]=@Id", ("@Id", id));
        if (table.Rows.Count != 1) throw new ArgumentException("Record not found.");
        var form = new RecordEditForm { ModuleKey=module, RecordId=id, Version=access.Version, Access=access,Fields=spec.fields.ToList(),DetailFields=spec.detailFields.ToList() };
        foreach (var f in spec.fields) form.Values[f.Name]=S(table.Rows[0],f.Name);
        if(module=="Weight")
        {
            form.InwardNo=S(table.Rows[0],"InwardNo");
            form.HasRecordedTare=!table.Rows[0].IsNull("TareWeight") && !table.Rows[0].IsNull("NetWeight");
            form.NetWeight=table.Rows[0].IsNull("NetWeight")?null:Convert.ToDecimal(table.Rows[0]["NetWeight"]);
            if(!form.HasRecordedTare){form.Fields.RemoveAll(f=>f.Name=="TareWeight");form.Values.Remove("TareWeight");}
        }
        if (spec.detailTable != null)
        {
            var parent = module == "Supervisor" ? table.Rows[0]["SupervisorActionId"] : (object)id;
            var details = Read(c,null,$"SELECT * FROM dbo.[{spec.detailTable}] WHERE [{spec.detailKey}]=@Id",("@Id",parent));
            foreach (DataRow row in details.Rows) form.Details.Add(spec.detailFields.ToDictionary(f=>f.Name,f=> (string?)(f.Name=="WorkType" ? S(row,f.Name) switch {"Load"=>"Loading","Unload"=>"Unloading",var x=>x}:S(row,f.Name))));
        }
        PopulateOptions(c,form);
        return form;
    }
    private void PopulateOptions(SqlConnection c, RecordEditForm form)
    {
        var office = Read(c,null,"SELECT OfficeId FROM dbo.v_RecordEditRows WHERE ModuleKey=@Module AND RecordId=@Id",("@Module",form.ModuleKey),("@Id",form.RecordId)).Rows[0][0];
        foreach(var f in form.Fields.Concat(form.DetailFields).Where(f=>f.Type=="select"))
        {
            if(f.Name=="WorkType") { f.Options=[new("Loading","Loading"),new("Unloading","Unloading")];continue; }
            string sql=f.Name switch {
                "GateId"=>"SELECT GateId Id,GateName Name FROM dbo.m_GateMaster WHERE IsActive=1",
                "InwardTypeId"=>"SELECT InwardTypeId Id,InwardTypeName Name FROM dbo.m_InwardTypeMaster WHERE IsActive=1",
                "VehicleTypeId"=>"SELECT VehicleTypeId Id,VehicleTypeName Name FROM dbo.m_VehicleTypeMaster WHERE IsActive=1",
                "VehicleId"=>"SELECT VehicleId Id,VehicleNumber Name FROM dbo.m_Vehicle WHERE IsActive=1",
                "CategoryId"=>"SELECT CategoryId Id,CategoryName Name FROM dbo.m_ItemCategory WHERE IsActive=1",
                "ItemId"=>"SELECT ItemId Id,ItemName Name FROM dbo.m_Item WHERE IsActive=1",
                "BagTypeId"=>"SELECT BagTypeId Id,BagTypeName Name FROM dbo.m_BagType WHERE IsActive=1",
                "LocationId"=>"SELECT l.LocationId Id,l.LocationName Name FROM dbo.m_LocationMaster l JOIN dbo.t_OfficeLocationMapping m ON m.LocationId=l.LocationId AND m.IsActive=1 WHERE l.IsActive=1 AND m.OfficeId=@Office AND ISNULL(m.UnloadingAllowed,1)=1",
                "PartyId" or "DriverId" or "MethId"=>@"SELECT DISTINCT p.PersonId Id,p.PersonName Name FROM dbo.P02_Person p JOIN dbo.P11_PersonDesignatation pd ON pd.P02_PersonId=p.PersonId AND pd.IsActive=1 JOIN dbo.O12_Designatation d ON d.DesignationId=pd.O12_DesignationId AND d.IsActive=1 JOIN dbo.O10_Post post ON post.PostId=d.O10_PostId AND post.IsActive=1 WHERE p.IsActive=1 AND post.PostId=@Post AND (@Post<>6 OR d.O05_Office_Id=@Office)",
                "WorkerId"=>@"SELECT DISTINCT p.PersonId Id,p.PersonName Name FROM dbo.P02_Person p JOIN dbo.P11_PersonDesignatation pd ON pd.P02_PersonId=p.PersonId AND pd.IsActive=1 JOIN dbo.O12_Designatation d ON d.DesignationId=pd.O12_DesignationId AND d.IsActive=1 WHERE p.IsActive=1 AND d.O10_PostId=7",
                _=>throw new ArgumentException("Unknown selection.")
            };
            f.Options=Read(c,null,sql,("@Office",office),("@Post",f.Name=="PartyId"?8:f.Name=="DriverId"?9:6)).AsEnumerable()
                .Select(r=>new SelectListItem(S(r,"Name"),S(r,"Id"))).OrderBy(x=>x.Text).ToList();
        }
    }
    private static Dictionary<string,object?> Validate(List<EditField> fields, Dictionary<string,string?> values)
    {
        if(values.Keys.Any(k=>!fields.Any(f=>f.Name==k))) throw new ArgumentException("Unexpected editable field.");
        var result=new Dictionary<string,object?>();
        foreach(var f in fields)
        {
            values.TryGetValue(f.Name,out var value);value=value?.Trim();
            if(string.IsNullOrEmpty(value)) { if(f.Required)throw new ArgumentException($"{f.Label} is required.");result[f.Name]=null;continue; }
            if(f.Type=="integer" || (f.Type=="select" && f.Name!="WorkType"))
            {
                if(!int.TryParse(value,NumberStyles.None,CultureInfo.InvariantCulture,out var n) || n<0 || (f.Type=="select" && n==0))throw new ArgumentException($"Invalid {f.Label}.");
                if(f.Type=="select" && !f.Options.Any(x=>x.Value==value))throw new ArgumentException($"{f.Label} is inactive or outside the company.");
                result[f.Name]=n;
            }
            else if(f.Type=="number")
            {
                if(!decimal.TryParse(value,NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out var n)||n<0||n>=1000000000000m||decimal.Round(n,2)!=n)throw new ArgumentException($"{f.Label} must be non-negative, with up to two decimal places.");
                result[f.Name]=n;
            }
            else
            {
                if(value.Length>f.MaxLength || (f.Type=="tel" && !System.Text.RegularExpressions.Regex.IsMatch(value,@"^\+?[0-9]{10,15}$")))throw new ArgumentException($"Invalid {f.Label}.");
                if(f.Type=="select" && !f.Options.Any(x=>x.Value==value))throw new ArgumentException($"Invalid {f.Label}.");
                result[f.Name]=value;
            }
        }
        return result;
    }
    private static byte[] Version(string value)
    {
        try { var bytes=Convert.FromBase64String(value);if(bytes.Length==8)return bytes; }catch(FormatException) { }
        throw new ArgumentException("Invalid record version. Reload the page.");
    }
    public void Save(int userId,RecordEditForm posted)
    {
        if(string.IsNullOrWhiteSpace(posted.Reason)||posted.Reason.Length>500)throw new ArgumentException("Enter a correction reason (up to 500 characters).");
        var current=Load(userId,posted.ModuleKey,posted.RecordId);
        var values=Validate(current.Fields,posted.Values);
        if(posted.Details.Count>200)throw new ArgumentException("At most 200 detail rows are allowed.");
        var details=posted.Details.Select(d=>Validate(current.DetailFields,d)).ToList();
        if(current.DetailFields.Count>0 && details.Count==0)throw new ArgumentException("At least one complete detail row is required.");
        using var c=Connect();using var tx=c.BeginTransaction();
        try
        {
            Run(c,tx,"EXEC dbo.sp_RecordEditAssert @User,@Module,@Id,@Version",("@User",userId),("@Module",posted.ModuleKey),("@Id",posted.RecordId),("@Version",Version(posted.Version)));
            Run(c,tx,"EXEC sys.sp_set_session_context @key=N'RecordEditReason',@value=@Reason",("@Reason",posted.Reason.Trim()));
            var spec=Spec(posted.ModuleKey);
            var original=Read(c,tx,$"SELECT * FROM dbo.[{spec.table}] WITH(UPDLOCK,HOLDLOCK) WHERE [{spec.key}]=@Id",("@Id",posted.RecordId)).Rows[0];
            switch(posted.ModuleKey)
            {
                case "GateInward":
                    ValidateInward(c,tx,posted.RecordId,values,original);
                    values["PartyName"]=Read(c,tx,"SELECT PersonName FROM dbo.P02_Person WHERE PersonId=@Id",("@Id",values["PartyId"])).Rows[0][0];
                    break;
                case "Weight": ValidateWeight(c,tx,posted.RecordId,values,original);break;
                case "Assignment": ValidateAssignment(c,tx,posted.RecordId,values,original);break;
                case "Supervisor": SaveSupervisor(c,tx,posted.RecordId,details,original);break;
                case "Labour": SaveLabour(c,tx,posted.RecordId,details);break;
            }
            if(values.Count>0)
            {
                var args=values.Select((v,i)=>("@p"+i,v.Value)).Append(("@Id",(object?)posted.RecordId)).ToArray();
                Run(c,tx,$"UPDATE dbo.[{spec.table}] SET {string.Join(',',values.Keys.Select((k,i)=>$"[{k}]=@p{i}"))} WHERE [{spec.key}]=@Id",args);
            }
            // Ensure even a no-op save has a correction event and invalidates stale forms.
            Run(c,tx,@"INSERT dbo.t_RecordAudit(ModuleKey,RecordId,EntityName,EntityId,EventType,ActorUserId,PolicyId,AfterJson)
                VALUES(@Module,@Id,'correction',@Id,'CORRECTION',@User,TRY_CONVERT(int,SESSION_CONTEXT(N'RecordEditPolicyId')),
                (SELECT @Reason Reason FOR JSON PATH,WITHOUT_ARRAY_WRAPPER));
                UPDATE dbo.t_RecordEditState SET ChangedAt=SYSDATETIME() WHERE ModuleKey=@Module AND RecordId=@Id;",
                ("@Module",posted.ModuleKey),("@Id",posted.RecordId),("@User",userId),("@Reason",posted.Reason.Trim()));
            tx.Commit();
        }
        catch {if(tx.Connection!=null)tx.Rollback();throw;}
    }
    private static void ValidateInward(SqlConnection c,SqlTransaction tx,string id,Dictionary<string,object?> v,DataRow old)
    {
        if(!System.Text.RegularExpressions.Regex.IsMatch((string)v["VehicleNo"]!,@"^[A-Za-z0-9][A-Za-z0-9 -]{2,49}$"))throw new ArgumentException("Enter a valid vehicle number.");
        // Linked weighment identities cannot silently diverge from the gate receipt.
        bool identityChanged=new[]{"PartyId","VehicleNo","DriverName","InwardTypeId","VehicleTypeId","GateId"}.Any(k=>Convert.ToString(v[k],CultureInfo.InvariantCulture)!=S(old,k));
        if(identityChanged && Read(c,tx,"SELECT 1 FROM dbo.t_GateEntry WITH(UPDLOCK,HOLDLOCK) WHERE InwardNo=@Inward",("@Inward",old["InwardNo"])).Rows.Count>0)
            throw new ArgumentException("Linked RSTs already exist. Gate, type, party, vehicle and driver must stay consistent with the RST; other receipt details may be corrected.");
    }
    private static void ValidateWeight(SqlConnection c,SqlTransaction tx,string id,Dictionary<string,object?> v,DataRow old)
    {
        var gross=(decimal)v["GrossWeight"]!;
        var tare=v.TryGetValue("TareWeight",out var postedTare) ? postedTare as decimal? : old.IsNull("TareWeight") ? null : (decimal?)Convert.ToDecimal(old["TareWeight"]);
        if(gross<=0 || (tare.HasValue && (tare<=0 || tare>=gross)))throw new ArgumentException("Gross must be positive and tare must be positive and below gross.");
        if(!old.IsNull("TareWeight") && !tare.HasValue)throw new ArgumentException("An existing tare weight cannot be removed.");
        if(old.IsNull("TareWeight") && tare.HasValue)throw new ArgumentException("Record the first tare through End RST so the outward workflow is completed correctly.");
        if(Read(c,tx,"SELECT 1 FROM dbo.t_GateEntry WITH(UPDLOCK,HOLDLOCK) WHERE ParentRSTNumber=@Id",("@Id",id)).Rows.Count>0 && (gross!=Convert.ToDecimal(old["GrossWeight"]) || tare!= (old.IsNull("TareWeight")?null:Convert.ToDecimal(old["TareWeight"]))))
            throw new ArgumentException("A later RST depends on these weights. Chain weights cannot be changed independently.");
        if(!old.IsNull("ParentRSTNumber") && gross!=Convert.ToDecimal(old["GrossWeight"]))throw new ArgumentException("This gross weight was carried from the previous RST and cannot be changed independently.");
        if(new[]{"PartyId","VehicleId","DriverId"}.Any(k=>Convert.ToInt32(v[k])!=Convert.ToInt32(old[k])))
            throw new ArgumentException("Party, vehicle and driver are linked to the original inward. They cannot be replaced on an existing RST.");
        if(Convert.ToInt32(v["CategoryId"])!=(old.IsNull("CategoryId")?0:Convert.ToInt32(old["CategoryId"])) && Read(c,tx,"SELECT 1 FROM dbo.t_UnloadTransaction WHERE RSTNumber=@Id",("@Id",id)).Rows.Count>0)
            throw new ArgumentException("Material category cannot change after unloading assignment.");
        v["NetWeight"]=tare.HasValue?gross-tare.Value:null;
    }
    private static void ValidateAssignment(SqlConnection c,SqlTransaction tx,string id,Dictionary<string,object?> v,DataRow old)
    {
        if(Convert.ToInt32(v["MethId"])!=Convert.ToInt32(old["MethId"]) && Read(c,tx,"SELECT 1 FROM dbo.t_WorkerAllocation WHERE UnloadId=@Id",("@Id",id)).Rows.Count>0)
            throw new ArgumentException("Labour work exists; the assigned Meth cannot be replaced independently.");
        Run(c,tx,@"IF NOT EXISTS(SELECT 1 FROM dbo.t_UnloadLocation WHERE UnloadId=@Id AND LocationId=@Location AND IsActive=1)
            INSERT dbo.t_UnloadLocation(UnloadId,LocationId,IsActive) VALUES(@Id,@Location,1);",("@Id",id),("@Location",v["LocationId"]));
    }
    private static void SaveSupervisor(SqlConnection c,SqlTransaction tx,string id,List<Dictionary<string,object?>> details,DataRow original)
    {
        if(details.Any(d=>Convert.ToInt32(d["BagCount"])<=0))throw new ArgumentException("Every material bag count must be positive.");
        if(details.GroupBy(d=>string.Join('|',new[]{"CategoryId","ItemId","LocationId","BagTypeId"}.Select(k=>d[k]))).Any(g=>g.Count()>1))throw new ArgumentException("Duplicate material/location/bag-type rows are not allowed.");
        foreach(var d in details)
        {
            if(Read(c,tx,"SELECT 1 FROM dbo.m_Item WHERE ItemId=@Item AND CategoryId=@Category AND IsActive=1",("@Item",d["ItemId"]),("@Category",d["CategoryId"])).Rows.Count==0)throw new ArgumentException("The selected item does not belong to its category.");
            if(Read(c,tx,"SELECT 1 FROM dbo.t_UnloadLocation WHERE UnloadId=@Id AND LocationId=@Location AND IsActive=1",("@Id",id),("@Location",d["LocationId"])).Rows.Count==0)throw new ArgumentException("Material location must be assigned to this unloading.");
        }
        long sum=details.Sum(d=>(long)Convert.ToInt32(d["BagCount"]));if(sum>int.MaxValue)throw new ArgumentException("Bag total is too large.");
        var workers=Read(c,tx,"SELECT SUM(BagCount) Total FROM dbo.t_WorkerAllocation WHERE UnloadId=@Id",("@Id",id)).Rows[0][0];
        if(workers!=DBNull.Value && Convert.ToInt64(workers)!=sum)throw new ArgumentException("Material total must match the recorded worker bag total.");
        // Saved lab/billing results cannot silently become attached to another item.
        var saved=Read(c,tx,@"SELECT DISTINCT r.CategoryId,r.ItemId FROM dbo.t_LabReport h JOIN dbo.t_LabReportResult r ON r.ReportId=h.ReportId
            JOIN dbo.t_UnloadTransaction u ON u.RSTNumber=h.RSTNumber WHERE u.UnloadId=@Id",("@Id",id));
        if(saved.AsEnumerable().Any(r=>!details.Any(d=>Convert.ToInt32(d["CategoryId"])==Convert.ToInt32(r["CategoryId"]) && Convert.ToInt32(d["ItemId"])==Convert.ToInt32(r["ItemId"]))))
            throw new ArgumentException("An item with saved lab results cannot be removed from the unloading.");
        var actionId=original["SupervisorActionId"];
        Run(c,tx,"DELETE dbo.t_SupervisorUnloadDetail WHERE SupervisorActionId=@Id; DELETE dbo.t_SupervisorUnloadLocationDetail WHERE SupervisorActionId=@Id;",("@Id",actionId));
        foreach(var d in details)
            Run(c,tx,@"INSERT dbo.t_SupervisorUnloadDetail(SupervisorActionId,CategoryId,ItemId,LocationId,BagTypeId,BagCount,ActualWeightKg,CreatedAt)
                VALUES(@Id,@Category,@Item,@Location,@Bag,@Count,@Weight,CONVERT(datetime2,SYSDATETIMEOFFSET() AT TIME ZONE 'India Standard Time'))",
                ("@Id",actionId),("@Category",d["CategoryId"]),("@Item",d["ItemId"]),("@Location",d["LocationId"]),("@Bag",d["BagTypeId"]),("@Count",d["BagCount"]),("@Weight",d["ActualWeightKg"]));
        Run(c,tx,@"INSERT dbo.t_SupervisorUnloadLocationDetail(SupervisorActionId,LocationId,BagTypeId,BagCount)
            SELECT SupervisorActionId,LocationId,BagTypeId,SUM(BagCount) FROM dbo.t_SupervisorUnloadDetail WHERE SupervisorActionId=@Id GROUP BY SupervisorActionId,LocationId,BagTypeId;
            UPDATE dbo.t_SupervisorUnloadAction SET SupervisorTotalBags=@Total,UpdatedAt=SYSDATETIME() WHERE SupervisorActionId=@Id;",("@Id",actionId),("@Total",(int)sum));
    }
    private static void SaveLabour(SqlConnection c,SqlTransaction tx,string id,List<Dictionary<string,object?>> details)
    {
        if(details.Any(d=>Convert.ToInt32(d["BagCount"])<=0))throw new ArgumentException("Every worker bag count must be positive.");
        if(details.GroupBy(d=>string.Join('|',new[]{"WorkerId","WorkType","BagTypeId"}.Select(k=>d[k]))).Any(g=>g.Count()>1))throw new ArgumentException("Duplicate worker/work/bag-type rows are not allowed.");
        var meth=Read(c,tx,"SELECT MethId FROM dbo.t_UnloadTransaction WHERE UnloadId=@Id",("@Id",id)).Rows[0][0];
        var cmd=Cmd(c,tx,"sp_GetWorkersByMeth",("@MethPersonId",meth));cmd.CommandType=CommandType.StoredProcedure;
        var allowed=new DataTable();using(var da=new SqlDataAdapter(cmd)) da.Fill(allowed);
        foreach(var d in details)if(!allowed.AsEnumerable().Any(r=>Convert.ToInt32(r["PersonId"])==Convert.ToInt32(d["WorkerId"])))throw new ArgumentException("Worker must belong to the assigned Meth.");
        long sum=details.Sum(d=>(long)Convert.ToInt32(d["BagCount"]));if(sum>int.MaxValue)throw new ArgumentException("Bag total is too large.");
        var sup=Read(c,tx,"SELECT SupervisorTotalBags FROM dbo.t_SupervisorUnloadAction WHERE UnloadId=@Id",("@Id",id));
        if(sup.Rows.Count>0 && Convert.ToInt64(sup.Rows[0][0])!=sum)throw new ArgumentException("Worker bags must match supervisor material bags.");
        // Preserve the original allocation timestamp even when rows are replaced.
        var entry=Read(c,tx,"SELECT EntryAt FROM dbo.t_RecordEditState WHERE ModuleKey='Labour' AND RecordId=@Id",("@Id",id)).Rows[0][0];
        Run(c,tx,"DELETE dbo.t_WorkerAllocation WHERE UnloadId=@Id",("@Id",id));
        foreach(var d in details)
        {
            var amount=Convert.ToInt32(d["BagCount"])*Convert.ToDecimal(d["PerBagCharge"]);
            if(amount>=10000000000000000m)throw new ArgumentException("Worker amount is too large.");
            Run(c,tx,@"INSERT dbo.t_WorkerAllocation(UnloadId,WorkerId,WorkType,BagTypeId,BagCount,PerBagCharge,PalledariAmount,AllocationTime)
                VALUES(@Id,@Worker,@Work,@Bag,@Count,@Charge,@Amount,@Entry)",("@Id",id),("@Worker",d["WorkerId"]),("@Work",d["WorkType"]),("@Bag",d["BagTypeId"]),("@Count",d["BagCount"]),("@Charge",d["PerBagCharge"]),("@Amount",amount),("@Entry",entry));
        }
        Run(c,tx,"UPDATE dbo.t_UnloadTransaction SET NumberOfBags=@Total WHERE UnloadId=@Id; UPDATE dbo.t_SupervisorUnloadAction SET MethTotalBags=@Total WHERE UnloadId=@Id",("@Total",(int)sum),("@Id",id));
    }
    public void Finalize(int userId,string module,string id,string version)
    { using var c=Connect();Run(c,null,"EXEC dbo.sp_RecordFinalize @User,@Module,@Id,@Version",("@User",userId),("@Module",module),("@Id",id),("@Version",Version(version))); }
    public DataTable History(int userId,string module,string id)
    {
        if(Access(userId,module,id)==null)throw new UnauthorizedAccessException();
        using var c=Connect();
        return Read(c,null,"SELECT AuditId,EventType,EntityName,ActorUserId,PolicyId,OccurredAt,BeforeJson,AfterJson FROM dbo.t_RecordAudit WHERE ModuleKey=@Module AND RecordId=@Id ORDER BY AuditId DESC",("@Module",module),("@Id",id));
    }
    public DataTable Policies()
    {
        using var c=Connect();var table=Read(c,null,"SELECT * FROM dbo.m_RecordEditPolicy ORDER BY PolicyId");
        table.Columns.Add("EditVersion",typeof(string));
        foreach(DataRow row in table.Rows)row["EditVersion"]=Convert.ToHexString((byte[])row["Version"]);
        return table;
    }
    public DataTable Bindings()
    {using var c=Connect();return Read(c,null,"SELECT b.ModuleKey,b.PostId,p.PostName,b.PolicyId FROM dbo.m_RecordEditBinding b JOIN dbo.o10_post p ON p.PostId=b.PostId ORDER BY b.ModuleKey,b.PostId");}
    public void SavePolicy(int userId,EditPolicyForm model)
    {
        using var c=Connect();using var tx=c.BeginTransaction();
        try {
            var before=Read(c,tx,"SELECT * FROM dbo.m_RecordEditPolicy WITH(UPDLOCK,HOLDLOCK) WHERE PolicyId=@Id",("@Id",model.PolicyId));
            if(before.Rows.Count!=1 || !Convert.ToHexString((byte[])before.Rows[0]["Version"]).Equals(model.Version, StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Policy changed. Reload before saving.");
            Run(c,tx,@"UPDATE dbo.m_RecordEditPolicy SET PolicyName=@Name,WindowMinutes=@Minutes,StopAtOutward=@Stop,IsActive=@Active WHERE PolicyId=@Id;
                INSERT dbo.t_RecordAudit(ModuleKey,RecordId,EntityName,EntityId,EventType,ActorUserId,BeforeJson,AfterJson)
                VALUES('Policy',CONVERT(nvarchar(100),@Id),'m_RecordEditPolicy',CONVERT(nvarchar(100),@Id),'UPDATE',@User,@Before,
                (SELECT PolicyId,PolicyName,WindowMinutes,StopAtOutward,IsActive FROM dbo.m_RecordEditPolicy WHERE PolicyId=@Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER));",
                ("@Name",model.PolicyName.Trim()),("@Minutes",model.WindowMinutes),("@Stop",model.StopAtOutward),("@Active",model.IsActive),("@Id",model.PolicyId),("@User",userId),
                ("@Before",System.Text.Json.JsonSerializer.Serialize(new {PolicyId=model.PolicyId,PolicyName=S(before.Rows[0],"PolicyName"),WindowMinutes=before.Rows[0].IsNull("WindowMinutes")?null:(int?)Convert.ToInt32(before.Rows[0]["WindowMinutes"]),StopAtOutward=Convert.ToBoolean(before.Rows[0]["StopAtOutward"]),IsActive=Convert.ToBoolean(before.Rows[0]["IsActive"])})));
            tx.Commit();
        }catch{if(tx.Connection!=null)tx.Rollback();throw;}
    }
    public void BindPolicy(int userId,string module,int post,int policy)
    {
        if(!Modules.Contains(module))throw new ArgumentException("Invalid module.");
        using var c=Connect();using var tx=c.BeginTransaction();
        try {
            Run(c,tx,@"IF NOT EXISTS(SELECT 1 FROM dbo.m_RecordEditPolicy WHERE PolicyId=@Policy) THROW 50001,'Policy not found.',1;
                IF NOT EXISTS(SELECT 1 FROM dbo.m_RecordEditBinding WHERE ModuleKey=@Module AND PostId=@Post) THROW 50001,'Role binding not found.',1;
                DECLARE @Old int;SELECT @Old=PolicyId FROM dbo.m_RecordEditBinding WITH(UPDLOCK,HOLDLOCK) WHERE ModuleKey=@Module AND PostId=@Post;
                UPDATE dbo.m_RecordEditBinding SET PolicyId=@Policy WHERE ModuleKey=@Module AND PostId=@Post;
                INSERT dbo.t_RecordAudit(ModuleKey,RecordId,EntityName,EntityId,EventType,ActorUserId,BeforeJson,AfterJson)
                VALUES('Policy',@Module,'m_RecordEditBinding',CONVERT(nvarchar(100),@Post),'REBIND',@User,
                (SELECT @Old PolicyId FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),(SELECT @Policy PolicyId FOR JSON PATH,WITHOUT_ARRAY_WRAPPER));",
                ("@Policy",policy),("@Module",module),("@Post",post),("@User",userId));tx.Commit();
        }catch{if(tx.Connection!=null)tx.Rollback();throw;}
    }
}
