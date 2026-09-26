-- Align report saving with the application's LabAccess policy. No tables or business rows are changed.
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.sp_LabReportSave
 @RSTNumber nvarchar(50),@SubmissionId uniqueidentifier,@SelectedItems nvarchar(max),@Results nvarchar(max),@Remarks nvarchar(1000)=NULL,@UserId int
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF ISJSON(@SelectedItems)<>1 OR ISJSON(@Results)<>1 THROW 50001,'Invalid test results.',1;
 BEGIN TRANSACTION;
 DECLARE @Technician nvarchar(200),@ReportId int;
 SELECT @Technician=p.PersonName FROM dbo.sa05_user u JOIN dbo.P02_Person p ON p.PersonId=u.PersonId WHERE u.UserId=@UserId AND u.IsActive=1 AND p.IsActive=1 AND (u.sa10_usertypeid IN (4,5) OR p.PersonType = N'Admin' OR LOWER(REPLACE(p.PersonType,N' ',N'')) = N'labtechnician');
 IF @Technician IS NULL THROW 50001,'An active lab technician or administrator login is required.',1;
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
