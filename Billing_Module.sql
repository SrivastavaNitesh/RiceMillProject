-- Billing is separate from settlements; no foreign keys.
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID('dbo.m_BillingLabRule','U') IS NULL
CREATE TABLE dbo.m_BillingLabRule(
 RuleId int IDENTITY PRIMARY KEY, CategoryId int NOT NULL, TestId int NOT NULL,
 Threshold decimal(18,6) NOT NULL, DeductionMode varchar(30) NOT NULL,
 DeductionValue decimal(18,6) NOT NULL, IsActive bit NOT NULL DEFAULT 1,
 ModifiedBy int NOT NULL, ModifiedAt datetime2 NOT NULL DEFAULT sysdatetime(),
 CONSTRAINT UQ_BillingLabRule UNIQUE(CategoryId,TestId),
 CONSTRAINT CK_BillingLabRuleValue CHECK(Threshold>=0 AND DeductionValue>=0));
GO
IF OBJECT_ID('dbo.t_BillingBill','U') IS NULL
CREATE TABLE dbo.t_BillingBill(
 BillId int IDENTITY PRIMARY KEY, RSTNumber nvarchar(50) NOT NULL,
 SubmissionId uniqueidentifier NOT NULL, SnapshotJson nvarchar(max) NOT NULL,
 NetWeight decimal(18,3) NOT NULL, ItemAmount decimal(18,2) NOT NULL,
 LabDeduction decimal(18,2) NOT NULL, WorkerAmount decimal(18,2) NOT NULL,
 GstPercent decimal(5,2) NOT NULL, GstAmount decimal(18,2) NOT NULL,
 TotalAmount decimal(18,2) NOT NULL, CreatedBy int NOT NULL,
 CreatedAt datetime2 NOT NULL DEFAULT sysdatetime(),
 CONSTRAINT UQ_BillingBill_RST UNIQUE(RSTNumber),
 CONSTRAINT UQ_BillingBill_Submission UNIQUE(SubmissionId),
 CONSTRAINT CK_BillingBillJson CHECK(ISJSON(SnapshotJson)=1),
 CONSTRAINT CK_BillingBillTotal CHECK(TotalAmount>=0));
GO
IF OBJECT_ID('dbo.t_BillingPayment','U') IS NULL
CREATE TABLE dbo.t_BillingPayment(
 PaymentId int IDENTITY PRIMARY KEY, BillId int NOT NULL, SubmissionId uniqueidentifier NOT NULL,
 Amount decimal(18,2) NOT NULL CHECK(Amount>0), Method nvarchar(30) NOT NULL,
 Reference nvarchar(100) NULL, PaidAt datetime2 NOT NULL DEFAULT sysdatetime(), RecordedBy int NOT NULL,
 CONSTRAINT UQ_BillingPayment_Submission UNIQUE(SubmissionId));
GO
CREATE OR ALTER PROCEDURE dbo.sp_BillingRstList
AS
BEGIN
 SET NOCOUNT ON;
 DECLARE @Coverage TABLE(RSTNumber nvarchar(50),VehicleNumber nvarchar(100),PartyName nvarchar(500),ItemCount int,RequiredTests int,CompletedTests int,UnmappedItems int);
 INSERT @Coverage EXEC dbo.sp_LabRstList;
 SELECT g.RSTNumber,ISNULL(p.PersonName,'') PartyName,ISNULL(p.MobileNumber,'') PartyMobile,
 ISNULL(d.PersonName,'') DriverName,ISNULL(d.MobileNumber,'') DriverMobile,
 ISNULL(v.VehicleNumber,'') VehicleNumber,g.GrossWeight,g.TareWeight,g.NetWeight,g.GateEntryTime GeneratedAt,
 ISNULL(g.Status,'Pending') GateStatus,ISNULL(s.Supervisors,'') SupervisorName,
 CAST(CASE WHEN c.ItemCount>0 AND c.UnmappedItems=0 AND c.RequiredTests>0 AND c.CompletedTests>=c.RequiredTests THEN 1 ELSE 0 END AS bit) LabComplete,
 ISNULL(c.ItemCount,0) ItemCount,ISNULL(c.RequiredTests,0) RequiredTests,ISNULL(c.CompletedTests,0) CompletedTests,
 b.BillId,b.TotalAmount,ISNULL(pay.PaidAmount,0) PaidAmount
 FROM dbo.t_GateEntry g
 LEFT JOIN dbo.P02_Person p ON p.PersonId=g.PartyId
 LEFT JOIN dbo.P02_Person d ON d.PersonId=g.DriverId
 LEFT JOIN dbo.m_Vehicle v ON v.VehicleId=g.VehicleId
 LEFT JOIN @Coverage c ON c.RSTNumber=g.RSTNumber
 LEFT JOIN dbo.t_BillingBill b ON b.RSTNumber=g.RSTNumber
 OUTER APPLY(SELECT SUM(Amount) PaidAmount FROM dbo.t_BillingPayment WHERE BillId=b.BillId) pay
 OUTER APPLY(SELECT STRING_AGG(x.PersonName,', ') Supervisors FROM
 (SELECT DISTINCT sp.PersonName FROM dbo.t_UnloadTransaction u JOIN dbo.t_SupervisorUnloadAction sa ON sa.UnloadId=u.UnloadId AND sa.Status='Completed' JOIN dbo.P02_Person sp ON sp.PersonId=sa.SupervisorId WHERE u.RSTNumber=g.RSTNumber) x) s
 ORDER BY g.GateEntryTime DESC,g.RSTNumber DESC;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_BillingSource @RSTNumber nvarchar(50)
AS
BEGIN
 SET NOCOUNT ON;
 SELECT g.RSTNumber,ISNULL(p.PersonName,'') PartyName,ISNULL(p.MobileNumber,'') PartyMobile,
 ISNULL(d.PersonName,'') DriverName,ISNULL(d.MobileNumber,'') DriverMobile,
 ISNULL(v.VehicleNumber,'') VehicleNumber,g.GrossWeight,g.TareWeight,g.NetWeight,g.GateEntryTime GeneratedAt,
 ISNULL(g.Status,'Pending') GateStatus,ISNULL(g.InwardNo,'') InwardNo
 FROM dbo.t_GateEntry g LEFT JOIN dbo.P02_Person p ON p.PersonId=g.PartyId
 LEFT JOIN dbo.P02_Person d ON d.PersonId=g.DriverId LEFT JOIN dbo.m_Vehicle v ON v.VehicleId=g.VehicleId WHERE g.RSTNumber=@RSTNumber;
 SELECT sd.SupervisorDetailId DetailId,u.UnloadId,sd.CategoryId,c.CategoryName,sd.ItemId,i.ItemName,
 sd.BagTypeId,b.BagTypeName,b.DeductionWeightGrams,sd.BagCount,sa.SupervisorId,p.PersonName SupervisorName,
 sa.SupervisorTotalBags,sa.MethTotalBags
 FROM dbo.t_UnloadTransaction u JOIN dbo.t_SupervisorUnloadAction sa ON sa.UnloadId=u.UnloadId AND sa.Status='Completed'
 JOIN dbo.t_SupervisorUnloadDetail sd ON sd.SupervisorActionId=sa.SupervisorActionId
 JOIN dbo.m_Item i ON i.ItemId=sd.ItemId JOIN dbo.m_ItemCategory c ON c.CategoryId=sd.CategoryId
 JOIN dbo.m_BagType b ON b.BagTypeId=sd.BagTypeId LEFT JOIN dbo.P02_Person p ON p.PersonId=sa.SupervisorId
 WHERE u.RSTNumber=@RSTNumber AND u.Status IN('Unloaded','Verified') ORDER BY sd.CategoryId,sd.ItemId,sd.SupervisorDetailId;
 SELECT DISTINCT x.UnloadId,x.LocationId,l.LocationName FROM
 (SELECT u.UnloadId,u.LocationId FROM dbo.t_UnloadTransaction u WHERE u.RSTNumber=@RSTNumber AND u.LocationId IS NOT NULL
 UNION SELECT u.UnloadId,ul.LocationId FROM dbo.t_UnloadTransaction u JOIN dbo.t_UnloadLocation ul ON ul.UnloadId=u.UnloadId AND ul.IsActive=1 WHERE u.RSTNumber=@RSTNumber) x
 JOIN dbo.m_LocationMaster l ON l.LocationId=x.LocationId ORDER BY x.UnloadId,x.LocationId;
 SELECT w.AllocationId,w.UnloadId,w.WorkerId,p.PersonName WorkerName,ISNULL(w.WorkType,'Unload') WorkType,
 w.BagTypeId,ISNULL(b.BagTypeName,'') BagTypeName,ISNULL(w.BagCount,0) BagCount,ISNULL(w.PerBagCharge,0) Rate
 FROM dbo.t_WorkerAllocation w JOIN dbo.t_UnloadTransaction u ON u.UnloadId=w.UnloadId
 LEFT JOIN dbo.P02_Person p ON p.PersonId=w.WorkerId LEFT JOIN dbo.m_BagType b ON b.BagTypeId=w.BagTypeId
 WHERE u.RSTNumber=@RSTNumber ORDER BY w.AllocationId;
 SELECT DISTINCT m.CategoryId,m.ItemId,m.TestId,t.TestName,t.Unit,t.Deducations MasterDeduction,un.UnitCode MasterUnit,
 r.ResultValue,r.ReportId,r.TestedAt,
 br.RuleId,br.Threshold,br.DeductionMode,br.DeductionValue
 FROM dbo.m_ItemLabTest m JOIN dbo.m_LabTestMaster t ON t.TestId=m.TestId AND t.IsActive=1
 LEFT JOIN dbo.tbl_Masterofunit un ON un.UnitId=t.Deducationsunitid
 OUTER APPLY(SELECT TOP 1 rr.ResultValue,h.ReportId,h.TestedAt FROM dbo.t_LabReportResult rr JOIN dbo.t_LabReport h ON h.ReportId=rr.ReportId
 WHERE h.RSTNumber=@RSTNumber AND rr.CategoryId=m.CategoryId AND rr.ItemId=m.ItemId AND rr.TestId=m.TestId ORDER BY h.TestedAt DESC,h.ReportId DESC,rr.ResultId DESC) r
 LEFT JOIN dbo.m_BillingLabRule br ON br.CategoryId=m.CategoryId AND br.TestId=m.TestId AND br.IsActive=1
 WHERE m.IsActive=1 AND EXISTS(SELECT 1 FROM dbo.t_UnloadTransaction u JOIN dbo.t_SupervisorUnloadAction sa ON sa.UnloadId=u.UnloadId AND sa.Status='Completed'
 JOIN dbo.t_SupervisorUnloadDetail sd ON sd.SupervisorActionId=sa.SupervisorActionId WHERE u.RSTNumber=@RSTNumber AND u.Status IN('Unloaded','Verified') AND sd.ItemId=m.ItemId AND sd.CategoryId=m.CategoryId)
 ORDER BY m.CategoryId,m.ItemId,m.TestId;
 SELECT UnloadId,Status FROM dbo.t_UnloadTransaction WHERE RSTNumber=@RSTNumber ORDER BY UnloadId;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_BillingSave
 @RSTNumber nvarchar(50),@SubmissionId uniqueidentifier,@SnapshotJson nvarchar(max),
 @NetWeight decimal(18,3),@ItemAmount decimal(18,2),@LabDeduction decimal(18,2),
 @WorkerAmount decimal(18,2),@GstPercent decimal(5,2),@GstAmount decimal(18,2),@TotalAmount decimal(18,2),@UserId int
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF NOT EXISTS(SELECT 1 FROM sa05_user u JOIN P02_Person p ON p.PersonId=u.PersonId WHERE u.UserId=@UserId AND u.IsActive=1 AND p.IsActive=1 AND (u.sa10_usertypeid=4 OR p.PersonType=N'Admin'))
 THROW 50001,'An active administrator login is required.',1;
 IF ISJSON(@SnapshotJson)<>1 OR @TotalAmount<0 OR @NetWeight<=0 THROW 50001,'Invalid bill values.',1;
 DECLARE @Existing int;
 SELECT @Existing=BillId FROM dbo.t_BillingBill WITH(UPDLOCK,HOLDLOCK) WHERE RSTNumber=@RSTNumber;
 IF @Existing IS NOT NULL BEGIN SELECT @Existing; RETURN; END;
 DECLARE @Coverage TABLE(RSTNumber nvarchar(50),VehicleNumber nvarchar(100),PartyName nvarchar(500),ItemCount int,RequiredTests int,CompletedTests int,UnmappedItems int);
 INSERT @Coverage EXEC dbo.sp_LabRstList;
 IF NOT EXISTS(SELECT 1 FROM @Coverage WHERE RSTNumber=@RSTNumber AND ItemCount>0 AND UnmappedItems=0 AND RequiredTests>0 AND CompletedTests>=RequiredTests)
 THROW 50001,'Complete all RST item lab tests before generating a bill.',1;
 INSERT dbo.t_BillingBill(RSTNumber,SubmissionId,SnapshotJson,NetWeight,ItemAmount,LabDeduction,WorkerAmount,GstPercent,GstAmount,TotalAmount,CreatedBy)
 VALUES(@RSTNumber,@SubmissionId,@SnapshotJson,@NetWeight,@ItemAmount,@LabDeduction,@WorkerAmount,@GstPercent,@GstAmount,@TotalAmount,@UserId);
 SELECT CAST(SCOPE_IDENTITY() AS int);
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_BillingRecordPayment
 @BillId int,@SubmissionId uniqueidentifier,@Amount decimal(18,2),@Method nvarchar(30),@Reference nvarchar(100)=NULL,@UserId int
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF NOT EXISTS(SELECT 1 FROM sa05_user u JOIN P02_Person p ON p.PersonId=u.PersonId WHERE u.UserId=@UserId AND u.IsActive=1 AND p.IsActive=1 AND (u.sa10_usertypeid=4 OR p.PersonType=N'Admin'))
 THROW 50001,'An active administrator login is required.',1;
 IF @Amount<=0 OR @Method NOT IN('Cash','Bank transfer','UPI','Cheque') THROW 50001,'Enter a positive payment and valid method.',1;
 BEGIN TRY
 BEGIN TRANSACTION;
 DECLARE @Total decimal(18,2),@Paid decimal(18,2),@Existing int;
 SELECT @Total=TotalAmount FROM dbo.t_BillingBill WITH(UPDLOCK,HOLDLOCK) WHERE BillId=@BillId;
 IF @Total IS NULL THROW 50001,'Bill not found.',1;
 SELECT @Existing=PaymentId FROM dbo.t_BillingPayment WITH(UPDLOCK,HOLDLOCK) WHERE SubmissionId=@SubmissionId;
 IF @Existing IS NOT NULL
 BEGIN
 IF NOT EXISTS(SELECT 1 FROM dbo.t_BillingPayment WHERE PaymentId=@Existing AND BillId=@BillId AND Amount=@Amount AND Method=@Method AND ISNULL(Reference,'')=ISNULL(@Reference,''))
 THROW 50001,'Payment request changed. Reload the bill.',1;
 COMMIT; SELECT @Existing; RETURN;
 END;
 SELECT @Paid=ISNULL(SUM(Amount),0) FROM dbo.t_BillingPayment WHERE BillId=@BillId;
 IF @Amount>@Total-@Paid THROW 50001,'Payment exceeds the outstanding bill balance.',1;
 INSERT dbo.t_BillingPayment(BillId,SubmissionId,Amount,Method,Reference,RecordedBy) VALUES(@BillId,@SubmissionId,@Amount,@Method,@Reference,@UserId);
 SET @Existing=SCOPE_IDENTITY();COMMIT;SELECT @Existing;
 END TRY
 BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK;
 THROW;
 END CATCH;
END;
GO
