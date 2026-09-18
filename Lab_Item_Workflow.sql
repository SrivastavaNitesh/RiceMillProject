-- Incremental lab workflow. IDs are validated by procedures; no relationship constraints are created.
-- Execute against the intended RiceMillDB (sqlcmd -d RiceMillDB -b -i Lab_Item_Workflow.sql).
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
GO
BEGIN TRANSACTION;
IF OBJECT_ID('dbo.m_LabTestMaster','U') IS NULL
CREATE TABLE dbo.m_LabTestMaster (
 TestId int IDENTITY PRIMARY KEY, TestName nvarchar(120) NOT NULL,
 Unit nvarchar(30) NULL, ResultType varchar(10) NOT NULL,
 Description nvarchar(500) NULL, IsActive bit NOT NULL DEFAULT 1,
 CreatedBy int NOT NULL, CreatedAt datetime2 NOT NULL DEFAULT SYSDATETIME(),
 ModifiedBy int NULL, ModifiedAt datetime2 NULL
);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.m_LabTestMaster') AND name='UX_LabTest_ActiveName')
CREATE UNIQUE INDEX UX_LabTest_ActiveName ON dbo.m_LabTestMaster(TestName) WHERE IsActive=1;
IF OBJECT_ID('dbo.m_ItemLabTest','U') IS NULL
CREATE TABLE dbo.m_ItemLabTest (
 MappingId int IDENTITY PRIMARY KEY, CategoryId int NOT NULL, ItemId int NOT NULL, TestId int NOT NULL,
 IsActive bit NOT NULL DEFAULT 1, CreatedBy int NOT NULL, CreatedAt datetime2 NOT NULL DEFAULT SYSDATETIME(),
 ModifiedBy int NULL, ModifiedAt datetime2 NULL
);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.m_ItemLabTest') AND name='UX_ItemLabTest_Active')
CREATE UNIQUE INDEX UX_ItemLabTest_Active ON dbo.m_ItemLabTest(CategoryId,ItemId,TestId) WHERE IsActive=1;
IF OBJECT_ID('dbo.t_UnloadItem','U') IS NULL
CREATE TABLE dbo.t_UnloadItem (
 UnloadItemId int IDENTITY PRIMARY KEY, UnloadId int NOT NULL, CategoryId int NOT NULL, ItemId int NOT NULL,
 CreatedAt datetime2 NOT NULL DEFAULT SYSDATETIME(), UNIQUE(UnloadId,ItemId)
);
IF OBJECT_ID('dbo.t_LabReport','U') IS NULL
CREATE TABLE dbo.t_LabReport (
 ReportId int IDENTITY PRIMARY KEY, SubmissionId uniqueidentifier NOT NULL UNIQUE,
 RSTNumber nvarchar(50) NOT NULL, TestedByUserId int NOT NULL,
 TechnicianName nvarchar(200) NOT NULL, TestedAt datetime2 NOT NULL DEFAULT SYSDATETIME(), Remarks nvarchar(1000) NULL
);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.t_LabReport') AND name='IX_LabReport_RST')
CREATE INDEX IX_LabReport_RST ON dbo.t_LabReport(RSTNumber,TestedAt);
IF OBJECT_ID('dbo.t_LabReportResult','U') IS NULL
CREATE TABLE dbo.t_LabReportResult (
 ResultId int IDENTITY PRIMARY KEY, ReportId int NOT NULL, CategoryId int NOT NULL, ItemId int NOT NULL, TestId int NOT NULL,
 CategoryName nvarchar(100) NOT NULL, ItemName nvarchar(100) NOT NULL, TestName nvarchar(120) NOT NULL,
 Unit nvarchar(30) NULL, ResultType varchar(10) NOT NULL, ResultValue nvarchar(500) NOT NULL,
 UNIQUE(ReportId,ItemId,TestId)
);
-- Assignment happens before the operator selects the actual unloading location.
IF EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.t_UnloadTransaction') AND name='LocationId' AND is_nullable=0)
ALTER TABLE dbo.t_UnloadTransaction ALTER COLUMN LocationId int NULL;
COMMIT;
GO
CREATE OR ALTER PROCEDURE dbo.sp_LabTestList
AS
BEGIN
 SET NOCOUNT ON;
 SELECT TestId,TestName,Unit,ResultType,Description,IsActive FROM dbo.m_LabTestMaster WHERE IsActive=1 ORDER BY TestName;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_LabTestSave
 @TestId int=0,@TestName nvarchar(120),@Unit nvarchar(30)=NULL,@ResultType varchar(10),@Description nvarchar(500)=NULL,@UserId int
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF NULLIF(LTRIM(RTRIM(@TestName)),'') IS NULL OR @ResultType NOT IN ('Number','Text') THROW 50001,'Enter a test name and valid result type.',1;
 IF NOT EXISTS(SELECT 1 FROM dbo.sa05_user WHERE UserId=@UserId AND IsActive=1 AND o10_postid IN (1,5)) THROW 50001,'Lab access is required.',1;
 IF EXISTS(SELECT 1 FROM dbo.m_LabTestMaster WHERE TestName=LTRIM(RTRIM(@TestName)) AND IsActive=1 AND TestId<>@TestId) THROW 50001,'An active test with this name already exists.',1;
 IF @TestId=0
 BEGIN
  INSERT dbo.m_LabTestMaster(TestName,Unit,ResultType,Description,CreatedBy) VALUES(LTRIM(RTRIM(@TestName)),@Unit,@ResultType,@Description,@UserId);
  SELECT CAST(SCOPE_IDENTITY() AS int) AS TestId;
 END
 ELSE
 BEGIN
  UPDATE dbo.m_LabTestMaster SET TestName=LTRIM(RTRIM(@TestName)),Unit=@Unit,ResultType=@ResultType,Description=@Description,ModifiedBy=@UserId,ModifiedAt=SYSDATETIME() WHERE TestId=@TestId AND IsActive=1;
  IF @@ROWCOUNT=0 THROW 50001,'Test is no longer active. Refresh the page.',1;
  SELECT @TestId AS TestId;
 END
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_LabTestRemove @TestId int,@UserId int
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF NOT EXISTS(SELECT 1 FROM dbo.sa05_user WHERE UserId=@UserId AND IsActive=1 AND o10_postid IN (1,5)) THROW 50001,'Lab access is required.',1;
 BEGIN TRANSACTION;
 UPDATE dbo.m_LabTestMaster SET IsActive=0,ModifiedBy=@UserId,ModifiedAt=SYSDATETIME() WHERE TestId=@TestId;
 UPDATE dbo.m_ItemLabTest SET IsActive=0,ModifiedBy=@UserId,ModifiedAt=SYSDATETIME() WHERE TestId=@TestId AND IsActive=1;
 COMMIT;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_LabMappingList
AS
BEGIN
 SET NOCOUNT ON;
 SELECT m.MappingId,m.CategoryId,m.ItemId,m.TestId,c.CategoryName,i.ItemName,t.TestName
 FROM dbo.m_ItemLabTest m JOIN dbo.m_ItemCategory c ON c.CategoryId=m.CategoryId
 JOIN dbo.m_Item i ON i.ItemId=m.ItemId JOIN dbo.m_LabTestMaster t ON t.TestId=m.TestId
 WHERE m.IsActive=1 AND t.IsActive=1 ORDER BY c.CategoryName,i.ItemName,t.TestName;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_LabMappingSave @MappingId int=0,@CategoryId int,@ItemId int,@TestId int,@UserId int
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF NOT EXISTS(SELECT 1 FROM dbo.sa05_user WHERE UserId=@UserId AND IsActive=1 AND o10_postid IN (1,5)) THROW 50001,'Lab access is required.',1;
 BEGIN TRANSACTION;
 IF NOT EXISTS(SELECT 1 FROM dbo.m_Item i JOIN dbo.m_ItemCategory c ON c.CategoryId=i.CategoryId WHERE i.ItemId=@ItemId AND i.CategoryId=@CategoryId AND i.IsActive=1 AND c.IsActive=1)
 THROW 50001,'Select an active item from the selected category.',1;
 IF NOT EXISTS(SELECT 1 FROM dbo.m_LabTestMaster WITH(UPDLOCK,HOLDLOCK) WHERE TestId=@TestId AND IsActive=1) THROW 50001,'Select an active test.',1;
 IF EXISTS(SELECT 1 FROM dbo.m_ItemLabTest WITH(UPDLOCK,HOLDLOCK) WHERE CategoryId=@CategoryId AND ItemId=@ItemId AND TestId=@TestId AND IsActive=1 AND MappingId<>@MappingId) THROW 50001,'This test is already mapped to this item.',1;
 IF @MappingId=0
  INSERT dbo.m_ItemLabTest(CategoryId,ItemId,TestId,CreatedBy) VALUES(@CategoryId,@ItemId,@TestId,@UserId);
 ELSE
 BEGIN
  UPDATE dbo.m_ItemLabTest SET CategoryId=@CategoryId,ItemId=@ItemId,TestId=@TestId,ModifiedBy=@UserId,ModifiedAt=SYSDATETIME() WHERE MappingId=@MappingId AND IsActive=1;
  IF @@ROWCOUNT=0 THROW 50001,'Mapping is no longer active. Refresh the page.',1;
 END
 COMMIT;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_LabMappingRemove @MappingId int,@UserId int
AS
BEGIN
 SET NOCOUNT ON;
 IF NOT EXISTS(SELECT 1 FROM dbo.sa05_user WHERE UserId=@UserId AND IsActive=1 AND o10_postid IN (1,5)) THROW 50001,'Lab access is required.',1;
 UPDATE dbo.m_ItemLabTest SET IsActive=0,ModifiedBy=@UserId,ModifiedAt=SYSDATETIME() WHERE MappingId=@MappingId;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_LabItemOptions @RSTNumber nvarchar(50)=NULL
AS
BEGIN
 SET NOCOUNT ON;
 SELECT i.ItemId,i.ItemName,c.CategoryId,c.CategoryName
 FROM dbo.m_Item i JOIN dbo.m_ItemCategory c ON c.CategoryId=i.CategoryId
 WHERE (@RSTNumber IS NULL AND i.IsActive=1 AND c.IsActive=1)
 OR EXISTS(SELECT 1 FROM dbo.t_UnloadItem ui JOIN dbo.t_UnloadTransaction u ON u.UnloadId=ui.UnloadId
 WHERE u.RSTNumber=@RSTNumber AND u.Status IN ('Unloaded','Verified') AND ui.ItemId=i.ItemId AND ui.CategoryId=c.CategoryId)
 ORDER BY c.CategoryName,i.ItemName;
 SELECT m.CategoryId,m.ItemId,t.TestId,t.TestName,t.Unit,t.ResultType,t.Description,t.IsActive
 FROM dbo.m_ItemLabTest m JOIN dbo.m_LabTestMaster t ON t.TestId=m.TestId
 WHERE m.IsActive=1 AND t.IsActive=1 AND (@RSTNumber IS NULL OR EXISTS(
 SELECT 1 FROM dbo.t_UnloadItem ui JOIN dbo.t_UnloadTransaction u ON u.UnloadId=ui.UnloadId
 WHERE u.RSTNumber=@RSTNumber AND u.Status IN ('Unloaded','Verified') AND ui.ItemId=m.ItemId AND ui.CategoryId=m.CategoryId));
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_LabRstList
AS
BEGIN
 SET NOCOUNT ON;
 WITH items AS (
 SELECT DISTINCT u.RSTNumber,ui.CategoryId,ui.ItemId FROM dbo.t_UnloadTransaction u JOIN dbo.t_UnloadItem ui ON ui.UnloadId=u.UnloadId WHERE u.Status IN ('Unloaded','Verified')
 ), required AS (
 SELECT x.RSTNumber,x.CategoryId,x.ItemId,m.TestId FROM items x JOIN dbo.m_ItemLabTest m ON m.CategoryId=x.CategoryId AND m.ItemId=x.ItemId AND m.IsActive=1 JOIN dbo.m_LabTestMaster t ON t.TestId=m.TestId AND t.IsActive=1
 )
 SELECT g.RSTNumber,ISNULL(v.VehicleNumber,'') AS VehicleNumber,ISNULL(p.PersonName,'') AS PartyName,
 (SELECT COUNT(*) FROM items x WHERE x.RSTNumber=g.RSTNumber) AS ItemCount,
 (SELECT COUNT(*) FROM required x WHERE x.RSTNumber=g.RSTNumber) AS RequiredTests,
 (SELECT COUNT(*) FROM required x WHERE x.RSTNumber=g.RSTNumber AND EXISTS(SELECT 1 FROM dbo.t_LabReport h JOIN dbo.t_LabReportResult r ON r.ReportId=h.ReportId WHERE h.RSTNumber=x.RSTNumber AND r.ItemId=x.ItemId AND r.CategoryId=x.CategoryId AND r.TestId=x.TestId)) AS CompletedTests,
 (SELECT COUNT(*) FROM items x WHERE x.RSTNumber=g.RSTNumber AND NOT EXISTS(SELECT 1 FROM required r WHERE r.RSTNumber=x.RSTNumber AND r.ItemId=x.ItemId AND r.CategoryId=x.CategoryId)) AS UnmappedItems
 FROM dbo.t_GateEntry g LEFT JOIN dbo.m_Vehicle v ON v.VehicleId=g.VehicleId LEFT JOIN dbo.P02_Person p ON p.PersonId=g.PartyId
 WHERE EXISTS(SELECT 1 FROM items x WHERE x.RSTNumber=g.RSTNumber) ORDER BY g.GateEntryTime DESC;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_LabReportSave
 @RSTNumber nvarchar(50),@SubmissionId uniqueidentifier,@SelectedItems nvarchar(max),@Results nvarchar(max),@Remarks nvarchar(1000)=NULL,@UserId int
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF ISJSON(@SelectedItems)<>1 OR ISJSON(@Results)<>1 THROW 50001,'Invalid test results.',1;
 BEGIN TRANSACTION;
 DECLARE @Technician nvarchar(200),@ReportId int;
 SELECT @Technician=p.PersonName FROM dbo.sa05_user u JOIN dbo.P02_Person p ON p.PersonId=u.PersonId WHERE u.UserId=@UserId AND u.IsActive=1 AND p.IsActive=1 AND u.o10_postid IN (1,5);
 IF @Technician IS NULL THROW 50001,'An active lab technician login is required.',1;
 SELECT @ReportId=ReportId FROM dbo.t_LabReport WITH(UPDLOCK,HOLDLOCK) WHERE SubmissionId=@SubmissionId AND RSTNumber=@RSTNumber AND TestedByUserId=@UserId;
 IF @ReportId IS NOT NULL BEGIN COMMIT; SELECT @ReportId AS ReportId; RETURN; END;
 IF NOT EXISTS(SELECT 1 FROM dbo.t_GateEntry WHERE RSTNumber=@RSTNumber) THROW 50001,'RST number does not exist.',1;
 DECLARE @Selected TABLE(ItemId int PRIMARY KEY);
 INSERT @Selected SELECT DISTINCT TRY_CONVERT(int,value) FROM OPENJSON(@SelectedItems) WHERE TRY_CONVERT(int,value)>0;
 IF NOT EXISTS(SELECT 1 FROM @Selected) THROW 50001,'Select at least one unloaded item.',1;
 IF EXISTS(SELECT 1 FROM @Selected s WHERE NOT EXISTS(SELECT 1 FROM dbo.t_UnloadItem ui JOIN dbo.t_UnloadTransaction u ON u.UnloadId=ui.UnloadId WHERE u.RSTNumber=@RSTNumber AND u.Status IN ('Unloaded','Verified') AND ui.ItemId=s.ItemId)) THROW 50001,'Only items unloaded against this RST can be tested.',1;
 DECLARE @Expected TABLE(CategoryId int,ItemId int,TestId int,CategoryName nvarchar(100),ItemName nvarchar(100),TestName nvarchar(120),Unit nvarchar(30),ResultType varchar(10),PRIMARY KEY(ItemId,TestId));
 INSERT @Expected
 SELECT DISTINCT m.CategoryId,m.ItemId,m.TestId,c.CategoryName,i.ItemName,t.TestName,t.Unit,t.ResultType
 FROM @Selected s JOIN dbo.m_ItemLabTest m WITH(HOLDLOCK) ON m.ItemId=s.ItemId AND m.IsActive=1
 JOIN dbo.m_LabTestMaster t WITH(HOLDLOCK) ON t.TestId=m.TestId AND t.IsActive=1
 JOIN dbo.m_Item i ON i.ItemId=m.ItemId AND i.CategoryId=m.CategoryId JOIN dbo.m_ItemCategory c ON c.CategoryId=m.CategoryId
 WHERE EXISTS(SELECT 1 FROM dbo.t_UnloadItem ui JOIN dbo.t_UnloadTransaction u ON u.UnloadId=ui.UnloadId WHERE u.RSTNumber=@RSTNumber AND u.Status IN ('Unloaded','Verified') AND ui.ItemId=m.ItemId AND ui.CategoryId=m.CategoryId);
 IF EXISTS(SELECT 1 FROM @Selected s WHERE NOT EXISTS(SELECT 1 FROM @Expected e WHERE e.ItemId=s.ItemId)) THROW 50001,'One or more selected items have no active tests. Configure their test mappings first.',1;
 DECLARE @Input TABLE(CategoryId int,ItemId int,TestId int,Value nvarchar(max));
 INSERT @Input SELECT CategoryId,ItemId,TestId,Value FROM OPENJSON(@Results) WITH(CategoryId int,ItemId int,TestId int,Value nvarchar(max));
 IF EXISTS(SELECT ItemId,TestId FROM @Input GROUP BY ItemId,TestId HAVING COUNT(*)>1) THROW 50001,'Duplicate test results are not allowed.',1;
 IF EXISTS(SELECT 1 FROM @Input r WHERE NOT EXISTS(SELECT 1 FROM @Expected e WHERE e.CategoryId=r.CategoryId AND e.ItemId=r.ItemId AND e.TestId=r.TestId))
 OR EXISTS(SELECT 1 FROM @Expected e WHERE NOT EXISTS(SELECT 1 FROM @Input r WHERE e.CategoryId=r.CategoryId AND e.ItemId=r.ItemId AND e.TestId=r.TestId)) THROW 50001,'Test mappings changed or results are incomplete. Reload the RST and fill every selected item test.',1;
 IF EXISTS(SELECT 1 FROM @Input r JOIN @Expected e ON e.ItemId=r.ItemId AND e.TestId=r.TestId WHERE NULLIF(LTRIM(RTRIM(r.Value)),'') IS NULL OR LEN(r.Value)>500 OR (e.ResultType='Number' AND TRY_CONVERT(decimal(18,6),r.Value) IS NULL)) THROW 50001,'Enter a valid value for every test (numeric results support up to 6 decimal places).',1;
 INSERT dbo.t_LabReport(SubmissionId,RSTNumber,TestedByUserId,TechnicianName,Remarks) VALUES(@SubmissionId,@RSTNumber,@UserId,@Technician,@Remarks);
 SET @ReportId=SCOPE_IDENTITY();
 INSERT dbo.t_LabReportResult(ReportId,CategoryId,ItemId,TestId,CategoryName,ItemName,TestName,Unit,ResultType,ResultValue)
 SELECT @ReportId,e.CategoryId,e.ItemId,e.TestId,e.CategoryName,e.ItemName,e.TestName,e.Unit,e.ResultType,LTRIM(RTRIM(r.Value)) FROM @Expected e JOIN @Input r ON e.ItemId=r.ItemId AND e.TestId=r.TestId;
 COMMIT;
 SELECT @ReportId AS ReportId;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_LabReportList @RSTNumber nvarchar(50)=NULL,@From date=NULL,@To date=NULL
AS
BEGIN
 SET NOCOUNT ON;
 SELECT h.ReportId,h.RSTNumber,h.TechnicianName,h.TestedAt,h.Remarks,COUNT(DISTINCT r.ItemId) AS ItemCount,COUNT(r.ResultId) AS ResultCount
 FROM dbo.t_LabReport h JOIN dbo.t_LabReportResult r ON r.ReportId=h.ReportId
 WHERE (@RSTNumber IS NULL OR h.RSTNumber=@RSTNumber) AND (@From IS NULL OR h.TestedAt>=@From) AND (@To IS NULL OR CAST(h.TestedAt AS date)<=@To)
 GROUP BY h.ReportId,h.RSTNumber,h.TechnicianName,h.TestedAt,h.Remarks ORDER BY h.TestedAt DESC,h.ReportId DESC;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_LabReportGet @ReportId int
AS
BEGIN
 SET NOCOUNT ON;
 SELECT ReportId,RSTNumber,TechnicianName,TestedAt,Remarks FROM dbo.t_LabReport WHERE ReportId=@ReportId;
 SELECT CategoryId,ItemId,CategoryName,ItemName,TestName,Unit,ResultValue FROM dbo.t_LabReportResult WHERE ReportId=@ReportId ORDER BY CategoryName,ItemName,TestName;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_CompleteUnloadWithItems
 @UnloadId int,@GateManId int,@BagTypeId int,@NumberOfBags int,@LocationId int,@Shift varchar(10),@ItemIds nvarchar(max),@WorkerIds nvarchar(max)
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @NumberOfBags<=0 OR @Shift NOT IN ('Day','Night') OR ISJSON(@ItemIds)<>1 OR ISJSON(@WorkerIds)<>1 THROW 50001,'Enter valid unloading details.',1;
 BEGIN TRANSACTION;
 IF NOT EXISTS(SELECT 1 FROM dbo.t_UnloadTransaction WITH(UPDLOCK,HOLDLOCK) WHERE UnloadId=@UnloadId AND Status='Assigned') THROW 50001,'This unloading is no longer awaiting submission.',1;
 IF NOT EXISTS(SELECT 1 FROM dbo.P02_Person WHERE PersonId=@GateManId AND IsActive=1) OR NOT EXISTS(SELECT 1 FROM dbo.m_BagType WHERE BagTypeId=@BagTypeId AND IsActive=1) OR NOT EXISTS(SELECT 1 FROM dbo.o_OfficeLocation WHERE LocationId=@LocationId AND IsActive=1) THROW 50001,'Select an active witness, bag type and location.',1;
 DECLARE @Items TABLE(ItemId int PRIMARY KEY);
 INSERT @Items SELECT DISTINCT TRY_CONVERT(int,value) FROM OPENJSON(@ItemIds) WHERE TRY_CONVERT(int,value)>0;
 IF NOT EXISTS(SELECT 1 FROM @Items) THROW 50001,'Select the items actually unloaded.',1;
 IF EXISTS(SELECT 1 FROM @Items x WHERE NOT EXISTS(SELECT 1 FROM dbo.m_Item i JOIN dbo.m_ItemCategory c ON c.CategoryId=i.CategoryId WHERE i.ItemId=x.ItemId AND i.IsActive=1 AND c.IsActive=1)) THROW 50001,'Select active unloaded items.',1;
 DECLARE @Workers TABLE(WorkerId int PRIMARY KEY);
 INSERT @Workers SELECT DISTINCT TRY_CONVERT(int,value) FROM OPENJSON(@WorkerIds) WHERE TRY_CONVERT(int,value)>0;
 IF EXISTS(SELECT 1 FROM @Workers w WHERE NOT EXISTS(SELECT 1 FROM dbo.P02_Person p WHERE p.PersonId=w.WorkerId AND p.IsActive=1)) THROW 50001,'Select active workers.',1;
 UPDATE dbo.t_UnloadTransaction SET GateManId=@GateManId,BagTypeId=@BagTypeId,NumberOfBags=@NumberOfBags,LocationId=@LocationId,UnloadTime=GETDATE(),Status='Unloaded' WHERE UnloadId=@UnloadId;
 INSERT dbo.t_UnloadItem(UnloadId,CategoryId,ItemId) SELECT @UnloadId,i.CategoryId,i.ItemId FROM @Items x JOIN dbo.m_Item i ON i.ItemId=x.ItemId;
 INSERT dbo.t_UnloadLocation(UnloadId,LocationId) VALUES(@UnloadId,@LocationId);
 INSERT dbo.t_WorkerAllocation(UnloadId,WorkerId,PalledariAmount) SELECT @UnloadId,WorkerId,CASE WHEN @Shift='Night' THEN 15 ELSE 10 END FROM @Workers;
 COMMIT;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_UnloadPeople
AS
BEGIN
 SET NOCOUNT ON;
 SELECT p.PersonId,p.PersonName,COALESCE(post.PostName,p.PersonType) AS RoleName
 FROM dbo.P02_Person p LEFT JOIN dbo.o10_post post ON post.PostId=TRY_CONVERT(int,p.PersonType)
 WHERE p.IsActive=1 ORDER BY p.PersonName;
END;
GO
