using System.Data;
using System.Diagnostics;
using System.Net;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using RiceMillProject.DAL;
using RiceMillProject.Models;

var root=args.Length>0?Path.GetFullPath(args[0]):Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../"));
var source=Directory.Exists(Path.Combine(root,"src/Controllers"))?Path.Combine(root,"src"):root;
var fixtures=File.Exists(Path.Combine(root,"test-schema.sql"))?root:Path.Combine(root,"analysis/record-edit-tests");
var database="RiceMillEditTest_"+Guid.NewGuid().ToString("N");
var builder=new SqlConnectionStringBuilder(@"Server=.\SQLEXPRESS;Database=master;Integrated Security=true;TrustServerCertificate=true");
using var master=new SqlConnection(builder.ConnectionString);master.Open();
void Exec(SqlConnection c,string sql){using var cmd=new SqlCommand(sql,c){CommandTimeout=60};cmd.ExecuteNonQuery();}
void Batch(SqlConnection c,string sql){foreach(var b in Regex.Split(sql,@"(?im)^GO\s*$"))if(!string.IsNullOrWhiteSpace(b))Exec(c,b);}
int assertions=0;
void Check(bool yes,string label){if(!yes)throw new Exception("FAIL: "+label);assertions++;Console.WriteLine("PASS: "+label);}
void Denied(Action action,string label){bool denied=false;try{action();}catch(Exception ex) when(ex is SqlException or ArgumentException or UnauthorizedAccessException){denied=true;}Check(denied,label);}
Exec(master,$"CREATE DATABASE [{database}]");
builder.InitialCatalog=database;
using var c=new SqlConnection(builder.ConnectionString);
Process? app=null;
try {
c.Open();
Batch(c,File.ReadAllText(Path.Combine(fixtures,"test-schema.sql")));
Batch(c,File.ReadAllText(Path.Combine(source,"Inward_Update_Metadata.sql")));
Batch(c,File.ReadAllText(Path.Combine(Environment.GetEnvironmentVariable("RecordEditSqlRoot")??source,"Record_Edit_Workflow.sql")));
Batch(c,File.ReadAllText(Path.Combine(Environment.GetEnvironmentVariable("RecordEditSqlRoot")??source,"Record_Edit_Workflow.sql")));
Batch(c,File.ReadAllText(Path.Combine(fixtures,"sp_GetWorkersByMeth.sql")));
var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"ConnectionStrings:DefaultConnection",builder.ConnectionString}}).Build();
var dal=new RecordEditDAL(config);
object Scalar(string sql){using var cmd=new SqlCommand(sql,c);return cmd.ExecuteScalar()!;}
void Actor(int id){RecordEditContext.UserId=id;Exec(c,$"EXEC sys.sp_set_session_context @key=N'RecordActorUserId',@value={id}");}
void Insert(string table,params (string key,object? value)[] args)
{
    var values=args.ToDictionary(x=>x.key,x=>x.value);
    using var metadata=new SqlCommand(@"SELECT col.name,typ.name TypeName,col.is_identity,col.is_nullable,col.default_object_id
        FROM sys.columns col JOIN sys.types typ ON typ.user_type_id=col.user_type_id WHERE col.object_id=OBJECT_ID(@Table)",c);
    metadata.Parameters.AddWithValue("@Table","dbo."+table);var t=new DataTable();using(var da=new SqlDataAdapter(metadata))da.Fill(t);
    foreach(DataRow col in t.Rows)if(!Convert.ToBoolean(col["is_identity"])&&!Convert.ToBoolean(col["is_nullable"])&&Convert.ToInt32(col["default_object_id"])==0&&!values.ContainsKey((string)col["name"]))
        values[(string)col["name"]]=(string)col["TypeName"] switch {"nvarchar" or "varchar" or "char" or "nchar"=>"Test","datetime" or "date" or "datetime2"=>DateTime.Now,"bit"=>true,_=>1};
    bool identity=t.AsEnumerable().Any(r=>Convert.ToBoolean(r["is_identity"])&&values.ContainsKey((string)r["name"]));
    using var cmd=new SqlCommand((identity?$"SET IDENTITY_INSERT dbo.[{table}] ON;":"")+$"INSERT dbo.[{table}] ({string.Join(',',values.Keys.Select(k=>$"[{k}]"))}) VALUES({string.Join(',',values.Keys.Select((k,i)=>"@p"+i))});"+(identity?$"SET IDENTITY_INSERT dbo.[{table}] OFF;":""),c);
    int n=0;foreach(var v in values.Values)cmd.Parameters.AddWithValue("@p"+n++,v??DBNull.Value);cmd.ExecuteNonQuery();
}
DateTime serverNow=(DateTime)Scalar("SELECT SYSDATETIME()");
DateTime now=(DateTime)Scalar("SELECT CONVERT(datetime2,SYSDATETIMEOFFSET() AT TIME ZONE 'India Standard Time')");
foreach(var post in new[]{1,2,3,4,6,7,8,9})Insert("o10_post",("PostId",post),("PostName",post switch{1=>"Gateman",2=>"Weightman",3=>"Supervisor",4=>"Admin",6=>"Meth",7=>"Worker",8=>"Party",_=>"Driver"}),("IsActive",true));
foreach(var (user,person,post,office) in new[]{(10,101,1,1),(11,111,1,1),(20,102,2,1),(30,103,3,1),(40,104,4,1),(41,114,4,2),(60,106,6,1)})
{
    Insert("P02_Person",("PersonId",person),("PersonName","Person "+person),("PersonType",post.ToString()),("IsActive",true));
    Insert("sa05_user",("UserId",user),("Username","u"+user),("PasswordHash","testpass"),("PersonId",person),("o10_postid",post),("sa10_usertypeid",post),("OfficeId",office),("IsActive",true));
    Insert("O12_Designatation",("DesignationId",person),("DesignationName","Post "+post),("O10_PostId",post),("O05_Office_Id",office),("IsActive",true));
    Insert("P11_PersonDesignatation",("P02_PersonId",person),("O12_DesignationId",person),("IsActive",true));
}
Insert("P02_Person",("PersonId",200),("PersonName","Supplier Test"),("PersonType","8"),("MobileNumber","9999999999"),("IsActive",true));
Insert("O12_Designatation",("DesignationId",200),("O10_PostId",8),("O05_Office_Id",1),("IsActive",true));
Insert("P11_PersonDesignatation",("P02_PersonId",200),("O12_DesignationId",200),("IsActive",true));
Insert("P02_Person",("PersonId",201),("PersonName","Driver Test"),("PersonType","9"),("IsActive",true));
Insert("O12_Designatation",("DesignationId",201),("O10_PostId",9),("O05_Office_Id",1),("IsActive",true));
Insert("P11_PersonDesignatation",("P02_PersonId",201),("O12_DesignationId",201),("IsActive",true));
Insert("P02_Person",("PersonId",207),("PersonName","Worker Test"),("PersonType","7"),("IsActive",true));
Insert("O12_Designatation",("DesignationId",207),("O10_PostId",7),("O05_Office_Id",1),("IsActive",true));
Insert("P11_PersonDesignatation",("P02_PersonId",207),("O12_DesignationId",207),("IsActive",true));
Insert("o_designatationreletation",("o12_parentid",106),("o12_childid",207),("IsActive",true));
Insert("m_GateMaster",("GateId",1),("GateName","Main gate"),("IsActive",true));
Insert("m_InwardTypeMaster",("InwardTypeId",1),("InwardTypeName","With RST"),("IsActive",true));
Insert("m_VehicleTypeMaster",("VehicleTypeId",1),("VehicleTypeName","Truck"),("IsActive",true));
Insert("m_Vehicle",("VehicleId",1),("VehicleNumber","UP63AT1234"),("IsActive",true));
Insert("m_ItemCategory",("CategoryId",1),("CategoryName","Paddy"),("IsActive",true));
Insert("m_Item",("ItemId",1),("ItemName","Paddy A"),("CategoryId",1),("IsActive",true));
Insert("m_BagType",("BagTypeId",1),("BagTypeName","Jute"),("IsActive",true));
Insert("m_LocationMaster",("LocationId",1),("LocationName","Stack 1"),("IsActive",true));
Insert("t_OfficeLocationMapping",("OfficeId",1),("LocationId",1),("IsActive",true),("UnloadingAllowed",true));
void Inward(int id,int owner,DateTime at){Actor(owner);Insert("t_InwardHeader",("InwardId",id),("GateEntryNo","GE-"+id),("InwardNo","INW-"+id),("InwardDate",at.Date),("InwardTime",at.ToString("HH:mm")),("GateInDateTime",at),("CreatedBy",owner),("GateId",1),("InwardTypeId",1),("PartyId",200),("PartyName","Supplier Test"),("VehicleNo","UP63AT1234"),("VehicleTypeId",1),("DriverName","Driver Test"),("ApproxNoOfBags",10),("ApproxWeight",100));}
Insert("m_InwardTypeMaster",("InwardTypeId",2),("InwardTypeName","Without RST"),("RSTRequired",false),("IsActive",true));
Inward(1,10,now.AddHours(-1));Inward(2,11,now.AddHours(-1));Inward(3,10,now.AddHours(-13));Inward(4,10,now.AddDays(-8));Inward(5,10,now.AddHours(-11));
Check(dal.List(10,"GateInward").All(r=>r.RecordId!="2"),"Gateman report shows only that Gateman's own entries");
Check(dal.Access(10,"GateInward","1")!.CanEdit,"Gateman edit inside 12 hours");
Check(!dal.Access(10,"GateInward","3")!.CanEdit,"Gateman edit closes after 12 hours");
Check(dal.Access(40,"GateInward","3")!.CanEdit,"Admin can correct after Gateman window");
Check(!dal.Access(40,"GateInward","4")!.CanEdit,"Admin edit closes after 7 days");
Check(dal.Access(41,"GateInward","1")==null,"Admin from another company cannot view/edit records");
Actor(10);var form=dal.Load(10,"GateInward","1");var oldVersion=form.Version;var entry=form.Access.EntryAt;form.Reason="Correct challan";form.Values["ChallanNo"]="C-UPDATED";dal.Save(10,form);
Check((string)Scalar("SELECT ChallanNo FROM dbo.t_InwardHeader WHERE InwardId=1")=="C-UPDATED","Valid Gateman correction saved");
Check(Convert.ToInt32(Scalar("SELECT UpdatedBy FROM dbo.t_InwardHeader WHERE InwardId=1"))==10 && Scalar("SELECT UpdatedOn FROM dbo.t_InwardHeader WHERE InwardId=1")!=DBNull.Value,"Gate update records authenticated UpdatedBy and backend UpdatedOn");
Check(dal.Access(10,"GateInward","1")!.EntryAt==entry,"Editing does not reset original entry time");
Denied(()=>dal.Save(10,form),"Stale edit version cannot overwrite a newer correction");
var fresh=dal.Load(10,"GateInward","1");fresh.Reason="Bad input";fresh.Values["ApproxWeight"]="-10";Denied(()=>dal.Save(10,fresh),"Negative approximate weight rejected");
fresh=dal.Load(10,"GateInward","1");fresh.Reason="Unexpected field";fresh.Values["CreatedBy"]="11";Denied(()=>dal.Save(10,fresh),"Posted owner field rejected");
Check(Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.t_RecordAudit WHERE ModuleKey='GateInward' AND RecordId='1' AND EventType='INSERT'"))==1,"Insertion history recorded once");
Check(Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.t_RecordAudit WHERE ModuleKey='GateInward' AND RecordId='1' AND EventType='UPDATE'"))==1,"Edit history captures before and after");
dal.Finalize(10,"GateInward","1",dal.Access(10,"GateInward","1")!.Version);
Check(!dal.Access(10,"GateInward","1")!.CanEdit,"Finalization removes operator edit permission");
Denied(()=>Exec(c,"UPDATE dbo.t_InwardHeader SET ChallanNo='BYPASS' WHERE InwardId=1"),"Database blocks direct update of finalized entry");
Actor(40);fresh=dal.Load(40,"GateInward","1");fresh.Reason="Admin correction after finalize";fresh.Values["ChallanNo"]="ADMIN-C";dal.Save(40,fresh);
Check(dal.Access(40,"GateInward","1")!.IsFinalized,"Admin correction does not reopen finalized entry");
Denied(()=>Exec(c,"UPDATE dbo.t_InwardHeader SET GateInDateTime=GETDATE() WHERE InwardId=1"),"Original entry clock cannot be changed even by admin");
Actor(20);Insert("t_GateEntry",("RSTNumber","TEST-RST"),("InwardNo","INW-1"),("VehicleId",1),("PartyId",200),("DriverId",201),("GrossWeight",1000m),("TareWeight",200m),("NetWeight",800m),("TargetOfficeId",1),("CreatedBy",20),("GateEntryTime",serverNow.AddHours(-1)),("CategoryId",1),("IsFinalRST",true));
Check(dal.Access(20,"Weight","TEST-RST")!.CanEdit,"Weightman can edit before outward");
var weight=dal.Load(20,"Weight","TEST-RST");weight.Reason="Correct gross";weight.Values["GrossWeight"]="1100";dal.Save(20,weight);
Check(Convert.ToDecimal(Scalar("SELECT NetWeight FROM dbo.t_GateEntry WHERE RSTNumber='TEST-RST'"))==900,"Weight correction recalculates net weight");
weight=dal.Load(20,"Weight","TEST-RST");weight.Reason="Bad tare";weight.Values["TareWeight"]="1200";Denied(()=>dal.Save(20,weight),"Tare above gross is rejected");
Actor(10);Exec(c,"UPDATE dbo.t_GateEntry SET Status='Exited',GateExitTime=GETDATE() WHERE RSTNumber='TEST-RST'");
Check(!dal.Access(20,"Weight","TEST-RST")!.CanEdit,"Outward immediately closes Weightman editing");
Actor(20);Denied(()=>Exec(c,"UPDATE dbo.t_GateEntry SET GrossWeight=1200 WHERE RSTNumber='TEST-RST'"),"Database also rejects Weightman correction after outward");
Check(dal.Access(40,"Weight","TEST-RST")!.CanEdit,"Admin retains 7-day correction after outward");
Actor(30);Insert("t_UnloadTransaction",("UnloadId",1),("RSTNumber","TEST-RST"),("SupervisorId",103),("MethId",106),("LocationId",1),("UnloadTime",serverNow.AddHours(-1)));
Insert("t_UnloadLocation",("UnloadId",1),("LocationId",1),("IsActive",true));
Insert("t_SupervisorUnloadAction",("SupervisorActionId",1),("UnloadId",1),("SupervisorId",103),("SupervisorTotalBags",10),("Status","Completed"),("CreatedAt",serverNow.AddHours(-1)));
Insert("t_SupervisorUnloadDetail",("SupervisorActionId",1),("CategoryId",1),("ItemId",1),("LocationId",1),("BagTypeId",1),("BagCount",10),("CreatedAt",now));
Insert("t_SupervisorUnloadLocationDetail",("SupervisorActionId",1),("LocationId",1),("BagTypeId",1),("BagCount",10));
Actor(60);Insert("t_WorkerAllocation",("UnloadId",1),("WorkerId",207),("BagTypeId",1),("BagCount",10),("WorkType","Unload"),("PerBagCharge",2m),("PalledariAmount",20m),("AllocationTime",serverNow.AddHours(-1)));
Check(dal.Access(30,"Supervisor","1")!.CanEdit&&dal.Access(60,"Labour","1")!.CanEdit,"Supervisor and Meth can edit inside 24 hours");
Actor(60);var labour=dal.Load(60,"Labour","1");labour.Reason="Correct worker charge";labour.Details[0]["PerBagCharge"]="3";dal.Save(60,labour);
Check(Convert.ToDecimal(Scalar("SELECT SUM(PalledariAmount) FROM dbo.t_WorkerAllocation WHERE UnloadId=1"))==30,"Labour correction saves and recalculates worker amount");
var before=Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.t_RecordAudit"));labour=dal.Load(60,"Labour","1");labour.Reason="Bad bag total";labour.Details[0]["BagCount"]="11";Denied(()=>dal.Save(60,labour),"Worker bags must match Supervisor bags");
Check(Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.t_RecordAudit"))==before,"Failed correction does not create partial history");
Actor(30);var supervisor=dal.Load(30,"Supervisor","1");supervisor.Reason="Material count check";supervisor.Details[0]["BagCount"]="11";Denied(()=>dal.Save(30,supervisor),"Supervisor bags must match workers");
supervisor=dal.Load(30,"Supervisor","1");supervisor.Reason="Confirm same material";dal.Save(30,supervisor);
Check((string)Scalar("SELECT Status FROM dbo.t_GateEntry WHERE RSTNumber='TEST-RST'")=="Exited","Supervisor correction preserves downstream outward status");
dal.Finalize(30,"Supervisor","1",dal.Access(30,"Supervisor","1")!.Version);
Check(!dal.Access(30,"Supervisor","1")!.CanEdit&&dal.Access(40,"Supervisor","1")!.CanEdit,"Finalized Supervisor entry becomes admin-only");
Actor(60);dal.Finalize(60,"Labour","1",dal.Access(60,"Labour","1")!.Version);
Check(!dal.Access(60,"Labour","1")!.CanEdit,"Meth finalization closes labour edits");
Actor(40);
var historyBefore=Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.t_RecordAudit"));
var stateBefore=dal.Access(40,"Labour","1")!.Version;
Exec(c,"CREATE OR ALTER TRIGGER dbo.FailLabourSave ON dbo.t_WorkerAllocation AFTER INSERT AS THROW 51001,'Simulated save failure',1;");
var broken=dal.Load(40,"Labour","1");broken.Reason="Rollback test";broken.Details[0]["PerBagCharge"]="4";
Denied(()=>dal.Save(40,broken),"A failure during detail replacement rejects the whole correction");
Check(Convert.ToDecimal(Scalar("SELECT SUM(PalledariAmount) FROM dbo.t_WorkerAllocation WHERE UnloadId=1"))==30
 && Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.t_RecordAudit"))==historyBefore
 && dal.Access(40,"Labour","1")!.Version==stateBefore,"Failure rolls back details, history and concurrency version together");
Exec(c,"DROP TRIGGER dbo.FailLabourSave");
Insert("t_UnloadTransaction",("UnloadId",2),("RSTNumber","TEST-RST"),("SupervisorId",103),("MethId",106),("LocationId",1),("UnloadTime",serverNow.AddHours(-25)));
Insert("t_SupervisorUnloadAction",("SupervisorActionId",2),("UnloadId",2),("SupervisorId",103),("SupervisorTotalBags",10),("Status","Completed"),("CreatedAt",serverNow.AddHours(-25)));
Insert("t_WorkerAllocation",("UnloadId",2),("WorkerId",207),("BagTypeId",1),("BagCount",10),("WorkType","Unload"),("PerBagCharge",2m),("PalledariAmount",20m),("AllocationTime",serverNow.AddHours(-25)));
Check(!dal.Access(30,"Supervisor","2")!.CanEdit&&!dal.Access(60,"Labour","2")!.CanEdit,"Supervisor and Meth editing closes after 24 hours");
Actor(40);var policies=dal.Policies();var p=policies.AsEnumerable().Single(r=>Convert.ToInt32(r["PolicyId"])==1);

dal.SavePolicy(40,new EditPolicyForm{PolicyId=1,PolicyName="Gateman 14 hours",WindowMinutes=840,IsActive=true,Version=(string)p["EditVersion"]});
Check(dal.Access(10,"GateInward","3")!.CanEdit,"Changing a policy parameter immediately changes eligibility");
dal.BindPolicy(40,"GateInward",1,3);
Check(dal.Access(10,"GateInward","3")!.PolicyId==3,"Changing assigned PolicyId reuses another time-limit category");
var sensitive=new GateEntry{PartyMobile="9999999999",DriverMobile="8888888888"};
RecordWorkflowFilter.Scrub(sensitive,new HashSet<object>(ReferenceEqualityComparer.Instance));
Check(sensitive.PartyMobile==""&&sensitive.DriverMobile=="8888888888","Party mobile is removed without removing driver contact");
Console.WriteLine($"PASS: {assertions} database/service assertions; test database is isolated and removed on completion.");
Inward(6,10,now.AddHours(-1));Actor(20);
Insert("t_GateEntry",("RSTNumber","NO-TARE"),("InwardNo","INW-6"),("VehicleId",1),("PartyId",200),("DriverId",201),("GrossWeight",1000m),("TargetOfficeId",1),("CreatedBy",20),("GateEntryTime",serverNow.AddHours(-1)),("CategoryId",1),("IsFinalRST",true));
var noTare=dal.Load(20,"Weight","NO-TARE");
Check(!noTare.HasRecordedTare&&!noTare.Fields.Any(f=>f.Name=="TareWeight"),"Tare field is unavailable before first tare/net entry");
noTare.Reason="Attempt first tare in edit";noTare.Values["TareWeight"]="100";
Denied(()=>dal.Save(20,noTare),"Forged first tare through edit is rejected");
noTare=dal.Load(20,"Weight","NO-TARE");noTare.Reason="Gross correction before tare";noTare.Values["GrossWeight"]="1100";dal.Save(20,noTare);
Check(Scalar("SELECT TareWeight FROM dbo.t_GateEntry WHERE RSTNumber='NO-TARE'")==DBNull.Value,"Gross-only correction preserves empty tare");
// Location report scoping, empty-location cards and drill-down data.
Insert("m_LocationMaster",("LocationId",2),("LocationName","Empty Stack"),("IsActive",true));
Insert("t_OfficeLocationMapping",("OfficeId",1),("LocationId",2),("IsActive",true),("UnloadingAllowed",true));
var reports=new SupervisorReportDAL(config);
var report=reports.Load(103,1,serverNow.AddDays(-3).Date,serverNow.Date,null,null,null);
Check(report.Locations.Rows.Count==2&&report.Entries.Rows.Count==2,"Location report includes empty locations and own assignments");
Check(report.Materials.AsEnumerable().Sum(r=>r.Field<decimal?>("WeightKg")??0m)==900m,"Location item weights reconcile to RST net without multiplying across assignments");
Check(report.Materials.Rows.Count>0&&report.LocationBags.Rows.Count>0&&report.Workers.Rows.Count>0,"Location drill-down includes material, bag and workforce details");
Check(reports.Load(999,1,serverNow.AddDays(-3).Date,serverNow.Date,null,null,null).Entries.Rows.Count==0,"Location report excludes another supervisor's assignments");
Check(reports.Load(103,2,serverNow.AddDays(-3).Date,serverNow.Date,null,null,null).Entries.Rows.Count==0,"Location report excludes another company");
Check(reports.Load(103,1,serverNow.AddDays(1).Date,serverNow.AddDays(2).Date,null,null,null).Entries.Rows.Count==0,"Location report respects date range");
Check(reports.Load(103,1,serverNow.AddDays(-3).Date,serverNow.Date,null,null,"no-match").Entries.Rows.Count==0,"Location report respects search");
Check(reports.Load(103,1,serverNow.AddDays(-3).Date,serverNow.Date,null,"Pending",null).Entries.Rows.Count==0,"Completed transactions excluded by pending filter");
Check(dal.VisibleInwardIds(30,false,true).Contains(1)&&!dal.VisibleInwardIds(60,false,true).Contains(1),"Supervisor inward report is scoped to assigned person");
Batch(c,@"CREATE OR ALTER PROCEDURE dbo.sp_GetActiveItemCategories AS SELECT CategoryId,CategoryName FROM dbo.m_ItemCategory WHERE IsActive=1;");
Batch(c,@"CREATE OR ALTER PROCEDURE dbo.sp_GetActiveBagTypes AS SELECT BagTypeId,BagTypeName FROM dbo.m_BagType WHERE IsActive=1;");
// Unfinalized current workforce fixture for native correction forms.
Actor(30);
Insert("t_UnloadTransaction",("UnloadId",3),("RSTNumber","NO-TARE"),("SupervisorId",103),("MethId",106),("LocationId",1),("Status","Unloaded"),("UnloadTime",serverNow.AddHours(-1)));
Insert("t_UnloadLocation",("UnloadId",3),("LocationId",1),("IsActive",true));
Insert("t_SupervisorUnloadAction",("SupervisorActionId",3),("UnloadId",3),("SupervisorId",103),("SupervisorTotalBags",10),("Status","Completed"),("CreatedAt",serverNow.AddHours(-1)));
Insert("t_SupervisorUnloadDetail",("SupervisorActionId",3),("CategoryId",1),("ItemId",1),("LocationId",1),("BagTypeId",1),("BagCount",10),("CreatedAt",serverNow));
Insert("t_SupervisorUnloadLocationDetail",("SupervisorActionId",3),("LocationId",1),("BagTypeId",1),("BagCount",10));
Insert("t_WorkerAllocation",("UnloadId",3),("WorkerId",207),("BagTypeId",1),("BagCount",10),("WorkType","Unloading"),("PerBagCharge",2m),("PalledariAmount",20m),("AllocationTime",serverNow.AddHours(-1)));
Batch(c,@"CREATE OR ALTER PROCEDURE dbo.sp_GetAllBagTypes AS SELECT * FROM dbo.m_BagType;");
foreach(var batch in Regex.Split(File.ReadAllText(Path.Combine(Environment.GetEnvironmentVariable("RecordEditSqlRoot")??source,"Supervisor_Field_Workflow_Update.sql")),@"(?im)^GO\s*$"))
    if(batch.Contains("CREATE OR ALTER PROCEDURE dbo.sp_GetSupervisorMethWorkRegister"))Exec(c,batch);

Batch(c,@"CREATE OR ALTER PROCEDURE dbo.sp_CreateGateEntry @RSTNumber nvarchar(100),@InwardNo nvarchar(50),@PartyId int,@VehicleId int,@DriverId int,@GrossWeight decimal(18,2),@TargetOfficeId int=NULL,@ItemId int=NULL,@CategoryId int,@WeightCharge decimal(18,2),@CreatedBy int AS
BEGIN SET NOCOUNT ON;INSERT dbo.t_GateEntry(RSTNumber,InwardNo,PartyId,VehicleId,DriverId,GrossWeight,TargetOfficeId,ItemId,CategoryId,WeighmentCharge,CreatedBy,GateEntryTime)
VALUES(@RSTNumber,@InwardNo,@PartyId,@VehicleId,@DriverId,@GrossWeight,1,@ItemId,@CategoryId,@WeightCharge,@CreatedBy,GETDATE());SELECT @RSTNumber;END;");

Batch(c,@"CREATE OR ALTER PROCEDURE dbo.sp_GetMethAssignments @MethPersonId int AS
SELECT u.*,s.PersonName SupervisorName,m.PersonName MethName,i.ItemName,l.LocationName,v.VehicleNumber,p.PersonName PartyName,o.OfficeName,g.GrossWeight,g.TareWeight,g.NetWeight
FROM dbo.t_UnloadTransaction u JOIN dbo.t_GateEntry g ON g.RSTNumber=u.RSTNumber
LEFT JOIN dbo.P02_Person s ON s.PersonId=u.SupervisorId LEFT JOIN dbo.P02_Person m ON m.PersonId=u.MethId
LEFT JOIN dbo.m_Item i ON i.ItemId=u.ItemId LEFT JOIN dbo.m_LocationMaster l ON l.LocationId=u.LocationId
LEFT JOIN dbo.m_Vehicle v ON v.VehicleId=g.VehicleId LEFT JOIN dbo.P02_Person p ON p.PersonId=g.PartyId LEFT JOIN dbo.o05_office o ON o.OfficeId=g.TargetOfficeId
WHERE u.MethId=@MethPersonId;");
Insert("t_UnloadTransaction",("UnloadId",4),("RSTNumber","NO-TARE"),("SupervisorId",103),("MethId",106),("LocationId",1),("Status","Assigned"),("UnloadTime",serverNow.AddHours(-1)));
// Browser-level form and authorization checks against the same isolated database.
Exec(c,"CREATE TABLE dbo.m_StatusMaster(StatusId int PRIMARY KEY,StatusName nvarchar(100)); INSERT dbo.m_StatusMaster VALUES(1,'Pending');");
Batch(c,@"CREATE OR ALTER PROCEDURE dbo.sp_ManageGateInwardEntry @Action varchar(20) AS
SELECT h.*,CAST(1 AS bit) RSTRequired,'Main gate' GateName,'With RST' InwardTypeName,'Truck' VehicleTypeName,
'Gateman' GateManName,'Pending' StatusName FROM dbo.t_InwardHeader h;");
Batch(c,@"CREATE OR ALTER PROCEDURE dbo.sp_ValidateUser @Username nvarchar(100),@Password nvarchar(100) AS
SELECT u.UserId,u.Username,u.PersonId,p.PersonName,u.o10_postid,u.sa10_usertypeid,u.OfficeId,
CASE u.o10_postid WHEN 1 THEN 'Gateman' WHEN 2 THEN 'Weightman' WHEN 3 THEN 'Supervisor' WHEN 4 THEN 'Admin' WHEN 6 THEN 'Meth' END Role,
'_Layout' LayoutName FROM dbo.sa05_user u JOIN dbo.P02_Person p ON p.PersonId=u.PersonId WHERE u.Username=@Username AND @Password='testpass';");
var psi=new ProcessStartInfo("dotnet",typeof(GateEntry).Assembly.Location){WorkingDirectory=source,UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
psi.Environment["ASPNETCORE_URLS"]="http://127.0.0.1:17936";psi.Environment["ConnectionStrings__DefaultConnection"]=builder.ConnectionString;psi.Environment["ASPNETCORE_ENVIRONMENT"]="Development";
app=Process.Start(psi)!;app.OutputDataReceived+=(_,_)=>{};app.ErrorDataReceived+=(_,_)=>{};app.BeginOutputReadLine();app.BeginErrorReadLine();
async Task<HttpClient> Login(int user)
{
    var h=new HttpClientHandler{CookieContainer=new CookieContainer(),AllowAutoRedirect=false};var http=new HttpClient(h){BaseAddress=new Uri("http://127.0.0.1:17936")};
    HttpResponseMessage? page=null;for(int i=0;i<40;i++){try{page=await http.GetAsync("/Account/Login");break;}catch(HttpRequestException){await Task.Delay(250);}}
    if(page==null)throw new Exception("Test application did not start.");
    var html=await page.Content.ReadAsStringAsync();var token=Regex.Match(html,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
    var response=await http.PostAsync("/Account/Login",new FormUrlEncodedContent(new Dictionary<string,string>{{"Username","u"+user},{"Password","testpass"},{"returnUrl","/RecordEdit/Index"},{"__RequestVerificationToken",WebUtility.HtmlDecode(token)}}));
    Check(response.StatusCode==HttpStatusCode.Redirect,"HTTP login for user "+user);return http;
}
using(var gate=await Login(10))
{
    var response=await gate.GetAsync("/Gateman/GateInwardReport");var html=await response.Content.ReadAsStringAsync();
    Check(response.StatusCode==HttpStatusCode.OK&&html.Contains("Gate Inward Report")&&!html.Contains(">INW-2<"),"Gate Inward Report renders and excludes another Gateman's entry");
    var table=html[html.IndexOf("<table")..];
    Check(table.Contains("<th>Driver</th>")&&table.Contains("<th>Entry time</th>")&&!table.Contains("Edit until")&&!table.Contains("Finalize")&&!table.Contains(">History<"),"Gateman report has requested columns and only Edit action");
    Check(Regex.IsMatch(table,@"INW-4[\s\S]*?<button[^>]*disabled[^>]*>Edit</button>"),"Expired Gateman entry retains a disabled Edit button");
    var menu=html[html.IndexOf("<ul class=\"flex-grow-1\">")..];menu=menu[..menu.IndexOf("</ul>")];
    Check(menu.IndexOf("Dashboard")<menu.IndexOf("Inward Registration")&&menu.IndexOf("Inward Registration")<menu.IndexOf("Gate Inward Report")&&menu.IndexOf("Gate Inward Report")<menu.LastIndexOf("Report"),"Gateman sidebar menu order matches request");
    response=await gate.GetAsync("/RecordEdit/Edit?module=GateInward&id=5");html=await response.Content.ReadAsStringAsync();
    Check(response.StatusCode==HttpStatusCode.OK&&html.Contains("Values[ChallanNo]"),"Correction form renders with named editable fields");
    Check(html.Contains("Vehicle Inward Entry Form")&&html.Contains("field-VehicleNo")&&html.Contains("field-DriverName")&&html.Contains("type=\"radio\""),"Gate correction matches original entry form layout and selection controls");
    var invalidVehicle=dal.Load(10,"GateInward","5");invalidVehicle.Reason="Reject unregistered vehicle";invalidVehicle.Values["VehicleNo"]="ZZ99NOTREGISTERED";Denied(()=>dal.Save(10,invalidVehicle),"Gate correction rejects a vehicle absent from the active master");
    var invalidDriver=dal.Load(10,"GateInward","5");invalidDriver.Reason="Reject unregistered driver";invalidDriver.Values["DriverName"]="Unregistered Driver";Denied(()=>dal.Save(10,invalidDriver),"Gate correction rejects an unknown driver");
    var token=WebUtility.HtmlDecode(Regex.Match(html,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
    var httpForm=dal.Load(10,"GateInward","5");
    var body=new Dictionary<string,string>{{"ModuleKey","GateInward"},{"RecordId","5"},{"Version",httpForm.Version},{"Reason","Browser correction"},{"__RequestVerificationToken",token}};
    foreach(var v in httpForm.Values)body["Values["+v.Key+"]"]=v.Value??"";body["Values[ChallanNo]"]="HTTP-CORRECTED";
    response=await gate.PostAsync("/RecordEdit/Edit",new FormUrlEncodedContent(body));
    Check(response.StatusCode==HttpStatusCode.Redirect&&(string)Scalar("SELECT ChallanNo FROM dbo.t_InwardHeader WHERE InwardId=5")=="HTTP-CORRECTED","Browser correction POST binds fields and saves atomically");
    response=await gate.GetAsync("/RecordEdit/Policies");Check(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Forbidden,"Non-admin cannot open edit parameters");
    response=await gate.PostAsync("/RecordEdit/FinalizeRecord",new FormUrlEncodedContent(new Dictionary<string,string>{{"module","GateInward"},{"id","5"},{"version",dal.Access(10,"GateInward","5")!.Version}}));
    Check(response.StatusCode==HttpStatusCode.BadRequest,"Mutation without antiforgery token is rejected");
}
using(var admin=await Login(40))
{
    var response=await admin.GetAsync("/RecordEdit/Policies");Check(response.StatusCode==HttpStatusCode.OK,"Admin edit-parameter page renders");
    Check(!(await response.Content.ReadAsStringAsync()).Contains(">Entry Corrections<"),"Admin correction navigation tab is absent");
    response=await admin.GetAsync("/RecordEdit/Edit?module=Weight&id=TEST-RST");var html=await response.Content.ReadAsStringAsync();
    Check(response.StatusCode==HttpStatusCode.OK&&html.Contains("field-TareWeight")&&html.Contains("edit-net-weight"),"Saved tare and net allow tare correction");
}
using(var weightClient=await Login(20))
{
    var response=await weightClient.GetAsync("/Gateman/Report");var html=await response.Content.ReadAsStringAsync();
    Check(response.StatusCode==HttpStatusCode.OK&&html.Contains("Supplier Test")&&!html.Contains("9999999999")&&!html.Contains("Party Mobile Number"),"Weightman report contains no party mobile value or column");
    Check(html.Contains("RST Transaction Report")&&!html.Contains("Inbound Details"),"Weightman menu label updated in its original location");
    response=await weightClient.GetAsync("/RecordEdit/Edit?module=Weight&id=NO-TARE");html=await response.Content.ReadAsStringAsync();
    Check(response.StatusCode==HttpStatusCode.OK&&!html.Contains("id=\"field-TareWeight\"")&&!html.Contains("Correct entry")&&html.Contains("erp-page-title\">NO-TARE"),"Weight edit uses RST heading and entry form styling with no first-tare option");


    response=await weightClient.GetAsync("/GateEntry/WeighmentSlip?id=TEST-RST&stage=first");html=await response.Content.ReadAsStringAsync();
    Check(response.StatusCode==HttpStatusCode.OK&&html.Contains("FIRST WEIGHMENT SLIP")&&html.Contains("Item Category")&&!html.Contains("Tare Weight (KG)")&&!html.Contains("Total Bags"),"First Slip contains entry fields without final weights or unloading details");
    response=await weightClient.GetAsync("/GateEntry/WeighmentSlip?id=TEST-RST&stage=final");html=await response.Content.ReadAsStringAsync();
    Check(response.StatusCode==HttpStatusCode.OK&&html.Contains("FINAL WEIGHMENT SLIP")&&html.Contains("Tare Weight (KG)")&&html.Contains("Net Weight (KG)")&&html.Contains("Total Bags")&&html.Contains("Unload Date / Time")&&!html.Contains("9999999999"),"Final Slip contains report fields and bag totals without party mobile");
    response=await weightClient.GetAsync("/GateEntry/WeighmentSlip?id=NO-TARE&stage=final");Check(response.StatusCode==HttpStatusCode.BadRequest,"Final Slip unavailable until final weight is recorded");
    response=await weightClient.GetAsync("/Gateman/Report");html=await response.Content.ReadAsStringAsync();
    Check(html.Contains("Unload Date / Time")&&html.Contains("First Slip")&&html.Contains("Final Slip"),"Weightman report exposes unload datetime and both available print stages");
    response=await weightClient.GetAsync("/GateEntry/Create?inwardId=6");html=await response.Content.ReadAsStringAsync();
    Check(response.StatusCode==HttpStatusCode.OK&&html.Contains("name=\"ItemCategoryId\"")&&!html.Contains("Product Category")&&!html.Contains("name=\"CategoryId\""),"Gate Entry shows only Item Category");
    var categoryToken=WebUtility.HtmlDecode(Regex.Match(html,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
    var categoryBody=new Dictionary<string,string>{{"RSTNumber","CATEGORY-TEST"},{"InwardNo","INW-6"},{"VehicleId","1"},{"PartyId","200"},{"DriverId","201"},{"GrossWeight","1000"},{"ItemCategoryId","1"},{"CategoryId","999"},{"__RequestVerificationToken",categoryToken}};
    response=await weightClient.PostAsync("/GateEntry/Create",new FormUrlEncodedContent(categoryBody));
    Check(response.StatusCode==HttpStatusCode.Redirect&&Convert.ToInt32(Scalar("SELECT CategoryId FROM dbo.t_GateEntry WHERE RSTNumber='CATEGORY-TEST'"))==1,"Item Category ID saves to existing CategoryId and ignores the separate posted Product Category");
}
using(var supervisorClient=await Login(30))
{
    var response=await supervisorClient.GetAsync("/Unload/SupervisorAction?id=1");var html=await response.Content.ReadAsStringAsync();
    Check(response.StatusCode==HttpStatusCode.OK&&html.Contains("supervisorActionForm"),"Supervisor Field Details renders without dropdown exception");
    response=await supervisorClient.GetAsync("/Unload/LocationReport");html=await response.Content.ReadAsStringAsync();
    Check(response.StatusCode==HttpStatusCode.OK&&html.Contains("Empty Stack")&&html.Contains("Workforce Management"),"Supervisor location cards and renamed menu render");
    response=await supervisorClient.GetAsync("/Unload/LocationCategories?locationId=1&categoryId=1");html=await response.Content.ReadAsStringAsync();
    Check(response.StatusCode==HttpStatusCode.OK&&html.Contains("Item Summary")&&html.Contains("Total weight (KG)")&&html.Contains("Total bags"),"Supervisor location drill-down renders details");
    response=await supervisorClient.GetAsync("/Unload/LocationReport?locationId=999");Check(response.StatusCode==HttpStatusCode.BadRequest,"Unknown location rejected");
    response=await supervisorClient.GetAsync("/Unload/LocationReport?fromDate=2026-10-25&toDate=2026-10-01");Check(response.StatusCode==HttpStatusCode.BadRequest,"Reversed report dates rejected");
    response=await supervisorClient.GetAsync("/Gateman/Report");html=await response.Content.ReadAsStringAsync();
    Check(response.StatusCode==HttpStatusCode.OK&&!html.Contains("Edit / Finalize / History")&&html.Contains("data-edit-mode=\"disable\""),"Supervisor inward report has Edit only with expiry disabling");

    response=await supervisorClient.GetAsync("/Unload/AssignmentRegister");html=await response.Content.ReadAsStringAsync();
    Check(response.StatusCode==HttpStatusCode.OK&&html.Contains("Assignment Register")&&html.Contains("Location &amp; Meth Assignment")&&html.Contains("data-edit-mode=\"disable\""),"Assignment Register has scoped Edit buttons and renamed menu");
    Check(!dal.Access(30,"Assignment","2")!.CanEdit&&dal.Access(30,"Assignment","3")!.CanEdit,"Assignment editing closes after 24 hours");
    response=await supervisorClient.GetAsync("/Unload/MethWorkRegister");html=await response.Content.ReadAsStringAsync();
    Check(response.StatusCode==HttpStatusCode.OK&&html.Contains("editModal-3")&&html.Contains("edit=True")&&!html.Contains("Saved entries")&&!html.Contains(">History<"),"Workforce Edit dialog provides saved Worker and Field Details without history section");
    response=await supervisorClient.GetAsync("/Unload/SupervisorMethWork?id=3&edit=true");html=await response.Content.ReadAsStringAsync();
    Check(response.StatusCode==HttpStatusCode.OK&&html.Contains("EditVersion")&&html.Contains("WorkerId")&&html.Contains("const saved="),"Worker correction reuses the native form with saved details");
    string Csrf(string markup)=>WebUtility.HtmlDecode(Regex.Match(markup,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
    var workerBody=new Dictionary<string,string>{{"UnloadId","3"},{"EditVersion",dal.Access(30,"Labour","3")!.Version},{"CorrectionReason","Native worker correction"},{"__RequestVerificationToken",Csrf(html)},{"WorkerRows[0].WorkerId","207"},{"WorkerRows[0].WorkType","Unloading"},{"WorkerRows[0].BagTypeId","1"},{"WorkerRows[0].BagCount","10"},{"WorkerRows[0].PerBagCharge","3"}};
    response=await supervisorClient.PostAsync("/Unload/SupervisorMethWork",new FormUrlEncodedContent(workerBody));
    Check(response.StatusCode==HttpStatusCode.Redirect&&Convert.ToDecimal(Scalar("SELECT SUM(PalledariAmount) FROM dbo.t_WorkerAllocation WHERE UnloadId=3"))==30,"Native worker correction saves atomically and recalculates amount");
    response=await supervisorClient.GetAsync("/Unload/SupervisorAction?id=3&edit=true");html=await response.Content.ReadAsStringAsync();
    Check(response.StatusCode==HttpStatusCode.OK&&html.Contains("EditVersion")&&html.Contains("const saved =")&&html.Contains("value=\"10\""),"Field correction reuses the native form with saved material and location values");
    var fieldBody=new Dictionary<string,string>{{"UnloadId","3"},{"EditVersion",dal.Access(30,"Supervisor","3")!.Version},{"CorrectionReason","Native field correction"},{"__RequestVerificationToken",Csrf(html)},{"CategoryRows[0].CategoryId","1"},{"CategoryRows[0].ItemId","1"},{"CategoryRows[0].LocationId","1"},{"CategoryRows[0].BagTypeId","1"},{"CategoryRows[0].BagCount","10"},{"LocationRows[0].LocationId","1"},{"LocationRows[0].BagRows[0].BagTypeId","1"},{"LocationRows[0].BagRows[0].BagCount","10"}};
    var beforeNative=Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.t_RecordAudit WHERE ModuleKey='Supervisor' AND RecordId='3'"));
    response=await supervisorClient.PostAsync("/Unload/SupervisorAction",new FormUrlEncodedContent(fieldBody));
    Check(response.StatusCode==HttpStatusCode.Redirect&&Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.t_RecordAudit WHERE ModuleKey='Supervisor' AND RecordId='3'"))>beforeNative,"Native field correction saves with audit history retained privately");
    fieldBody["EditVersion"]=dal.Access(30,"Supervisor","3")!.Version;fieldBody["LocationRows[0].BagRows[0].BagCount"]="9";
    beforeNative=Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.t_RecordAudit"));
    await supervisorClient.PostAsync("/Unload/SupervisorAction",new FormUrlEncodedContent(fieldBody));
    Check(Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.t_RecordAudit"))==beforeNative,"Native field correction rejects mismatched location bags without partial save");
    response=await supervisorClient.GetAsync("/Unload/SupervisorAction?id=2&edit=true");Check(response.StatusCode==HttpStatusCode.Forbidden,"Expired native correction is blocked on server");

    response=await supervisorClient.GetAsync("/Unload/MethWorkRegister");html=await response.Content.ReadAsStringAsync();
    Check(html.Contains("viewModal-3")&&html.Contains("viewOnly=true")&&html.Contains("Item Category"),"Workforce has a View chooser and actual item-category label");
    var auditBeforeView=Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.t_RecordAudit"));
    response=await supervisorClient.GetAsync("/Unload/SupervisorMethWork?id=2&viewOnly=true");html=await response.Content.ReadAsStringAsync();
    Check(response.StatusCode==HttpStatusCode.OK&&html.Contains("data-readonly=\"true\"")&&html.Contains("<fieldset disabled")&&!html.Contains("name=\"CorrectionReason\""),"Expired Worker Details remain viewable in disabled original form");
    response=await supervisorClient.GetAsync("/Unload/SupervisorAction?id=1&viewOnly=true");html=await response.Content.ReadAsStringAsync();
    Check(response.StatusCode==HttpStatusCode.OK&&html.Contains("data-readonly=\"true\"")&&html.Contains("value=\"10\""),"Finalized Field Details remain viewable with saved location bags");
    Check(Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.t_RecordAudit"))==auditBeforeView,"Viewing worker/field details never writes records or audit events");
    response=await supervisorClient.GetAsync("/Unload/LocationCategories?locationId=1&categoryId=999");Check(response.StatusCode==HttpStatusCode.NotFound,"Category drill-down rejects unknown category at location");
}

using(var otherOfficeAdmin=await Login(41)){
var response=await otherOfficeAdmin.GetAsync("/GateEntry/WeighmentSlip?id=TEST-RST&stage=final");Check((response.StatusCode==HttpStatusCode.Forbidden||(response.StatusCode==HttpStatusCode.Redirect&&response.Headers.Location?.ToString().Contains("AccessDenied")==true)),"Slip printing excludes other offices");
}
using(var methClient=await Login(60))
{
    var response=await methClient.GetAsync("/Meth");var html=await response.Content.ReadAsStringAsync();
    Check(response.StatusCode==HttpStatusCode.OK&&html.Contains("Item Category")&&html.Contains("Paddy")&&!html.Contains("Entry Corrections"),"Meth dashboard shows category names and removes correction navigation");
    Check(html.Contains("/Meth/WorkReport?status=pending")&&html.Contains("/Meth/WorkReport?status=completed"),"Meth summary cards link to filtered reports");
    response=await methClient.GetAsync("/Meth/WorkReport?status=pending");html=await response.Content.ReadAsStringAsync();
    Check(response.StatusCode==HttpStatusCode.OK&&html.Contains("/Meth/ExecuteUnload/4")&&!html.Contains("viewOnly=true"),"Pending Meth report has no active edit/view before details are saved");
    response=await methClient.GetAsync("/Meth/WorkReport?status=completed");html=await response.Content.ReadAsStringAsync();
    Check(response.StatusCode==HttpStatusCode.OK&&html.Contains("viewOnly=true")&&html.Contains("edit=true")&&!html.Contains("/Meth/ExecuteUnload/4"),"Completed Meth report provides View and eligible Edit only");
    response=await methClient.GetAsync("/Meth/ExecuteUnload?id=3&edit=true");html=await response.Content.ReadAsStringAsync();
    Check(response.StatusCode==HttpStatusCode.OK&&html.Contains("EditVersion")&&html.Contains("const saved=")&&html.Contains("worker-work-type"),"Meth edit uses its original worker form with saved work and bag details");
    var token=WebUtility.HtmlDecode(Regex.Match(html,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
    var body=new Dictionary<string,string>{{"UnloadId","3"},{"EditVersion",dal.Access(60,"Labour","3")!.Version},{"CorrectionReason","Meth native correction"},{"__RequestVerificationToken",token},{"WorkerRows[0].WorkerId","207"},{"WorkerRows[0].WorkType","Unload"},{"WorkerRows[0].BagTypeId","1"},{"WorkerRows[0].BagCount","10"},{"WorkerRows[0].PerBagCharge","4"}};
    response=await methClient.PostAsync("/Meth/ExecuteUnload",new FormUrlEncodedContent(body));
    Check(response.StatusCode==HttpStatusCode.Redirect&&Convert.ToDecimal(Scalar("SELECT SUM(PalledariAmount) FROM dbo.t_WorkerAllocation WHERE UnloadId=3"))==40,"Meth native edit saves and recalculates payment");
    response=await methClient.GetAsync("/Meth/ExecuteUnload?id=2&edit=true");Check(response.StatusCode==HttpStatusCode.Forbidden,"Meth native edit closes after 24 hours");
    response=await methClient.GetAsync("/Meth/ExecuteUnload?id=2&viewOnly=true");html=await response.Content.ReadAsStringAsync();
    Check(response.StatusCode==HttpStatusCode.OK&&html.Contains("data-readonly=\"true\"")&&html.Contains("<fieldset disabled"),"Meth can view expired saved details in original form");
    response=await methClient.GetAsync("/Meth/ExecuteUnload?id=4&viewOnly=true");Check(response.StatusCode==HttpStatusCode.NotFound,"View is unavailable before Meth work details are saved");
    response=await methClient.GetAsync("/Meth/WorkReport?status=invalid");Check(response.StatusCode==HttpStatusCode.BadRequest,"Invalid Meth report status is rejected");
}
Console.WriteLine($"ALL PASS: {assertions} assertions.");
}
finally {
    if(app!=null&&!app.HasExited){app.Kill(true);app.WaitForExit();app.Dispose();}
    c.Close();SqlConnection.ClearAllPools();
    Exec(master,$"ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}]");
}


