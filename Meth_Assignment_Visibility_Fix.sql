SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetMethAssignments
    @MethPersonId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.UnloadId,u.RSTNumber,u.LocationId,u.SupervisorId,u.MethId,u.ItemId,u.NumberOfBags,u.UnloadTime,u.Status,
        ISNULL(s.PersonName,'') SupervisorName,ISNULL(m.PersonName,'') MethName,ISNULL(i.ItemName,'') ItemName,
        COALESCE(assigned.LocationName,l.LocationName,'') LocationName,ISNULL(v.VehicleNumber,'') VehicleNumber,
        ISNULL(p.PersonName,'') PartyName,ISNULL(g.GrossWeight,0) GrossWeight,ISNULL(g.TareWeight,0) TareWeight,
        ISNULL(g.NetWeight,0) NetWeight,ISNULL(o.OfficeName,'') OfficeName,
        CAST(CASE WHEN EXISTS(SELECT 1 FROM dbo.t_WorkerAllocation wa WHERE wa.UnloadId=u.UnloadId AND ISNULL(wa.BagCount,0)>0) THEN 1 ELSE 0 END AS bit) IsWorkerWorkCompleted
    FROM dbo.t_UnloadTransaction u
    INNER JOIN dbo.t_GateEntry g ON g.RSTNumber=u.RSTNumber
    LEFT JOIN dbo.P02_Person s ON s.PersonId=u.SupervisorId
    LEFT JOIN dbo.P02_Person m ON m.PersonId=u.MethId
    LEFT JOIN dbo.m_Item i ON i.ItemId=u.ItemId
    LEFT JOIN dbo.m_LocationMaster l ON l.LocationId=u.LocationId
    OUTER APPLY(SELECT STRING_AGG(CONVERT(nvarchar(max),lm.LocationName),', ') WITHIN GROUP(ORDER BY lm.LocationName) LocationName FROM dbo.t_UnloadLocation ul INNER JOIN dbo.m_LocationMaster lm ON lm.LocationId=ul.LocationId WHERE ul.UnloadId=u.UnloadId AND ul.IsActive=1) assigned
    LEFT JOIN dbo.m_Vehicle v ON v.VehicleId=g.VehicleId
    LEFT JOIN dbo.P02_Person p ON p.PersonId=g.PartyId
    LEFT JOIN dbo.o05_office o ON o.OfficeId=g.TargetOfficeId
    WHERE u.MethId=@MethPersonId
    ORDER BY CASE WHEN u.Status='Assigned' AND NOT EXISTS(SELECT 1 FROM dbo.t_WorkerAllocation wa WHERE wa.UnloadId=u.UnloadId AND ISNULL(wa.BagCount,0)>0) THEN 0 ELSE 1 END,u.UnloadId DESC;
END;
GO

IF OBJECT_ID('dbo.tbl_OrderReturnBagDetail','U') IS NULL
BEGIN
 CREATE TABLE dbo.tbl_OrderReturnBagDetail
 (
  OrderReturnBagDetailId INT IDENTITY(1,1) PRIMARY KEY,
  OrderReturnId INT NOT NULL,
  BagTypeId INT NOT NULL,
  BagCount INT NOT NULL,
  CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_OrderReturnBagDetail_CreatedAt DEFAULT(SYSDATETIME()),
  CONSTRAINT FK_OrderReturnBagDetail_Return FOREIGN KEY(OrderReturnId) REFERENCES dbo.tbl_OrderReturn(OrderReturnId),
  CONSTRAINT FK_OrderReturnBagDetail_BagType FOREIGN KEY(BagTypeId) REFERENCES dbo.m_BagType(BagTypeId),
  CONSTRAINT CK_OrderReturnBagDetail_Count CHECK(BagCount>0),
  CONSTRAINT UQ_OrderReturnBagDetail UNIQUE(OrderReturnId,BagTypeId)
 );
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_MethCompleteUnloadWithReturn
 @UnloadId INT,@MethPersonId INT,@WorkerRows NVARCHAR(MAX),@ReturnRows NVARCHAR(MAX)=N'[]',@ReturnReason NVARCHAR(500)=NULL
AS
BEGIN
 SET NOCOUNT ON;SET XACT_ABORT ON;
 IF ISJSON(@ReturnRows)<>1 THROW 51201,'Invalid damaged/return bag details.',1;
 DECLARE @ReturnTotal INT=ISNULL((SELECT SUM(BagCount) FROM OPENJSON(@ReturnRows) WITH(BagTypeId INT,BagCount INT)),0);
 DECLARE @WorkerTotal INT=ISNULL((SELECT SUM(BagCount) FROM OPENJSON(@WorkerRows) WITH(BagCount INT)),0);
 IF @ReturnTotal>@WorkerTotal THROW 51202,'Damaged/return bags cannot exceed total handled bags.',1;
 IF @ReturnTotal>0 AND NULLIF(LTRIM(RTRIM(@ReturnReason)),'') IS NULL THROW 51203,'Return reason is required.',1;
 IF EXISTS(SELECT 1 FROM OPENJSON(@ReturnRows) WITH(BagTypeId INT,BagCount INT) WHERE BagTypeId<=0 OR BagCount<=0) THROW 51204,'Return bag type and quantity are invalid.',1;
 IF EXISTS(SELECT 1 FROM OPENJSON(@ReturnRows) WITH(BagTypeId INT,BagCount INT) GROUP BY BagTypeId HAVING COUNT(*)>1) THROW 51205,'Same return bag type cannot be entered twice.',1;
 BEGIN TRAN;
 EXEC dbo.sp_MethCompleteUnload @UnloadId=@UnloadId,@MethPersonId=@MethPersonId,@WorkerRows=@WorkerRows;
 IF @ReturnTotal>0
 BEGIN
  DECLARE @OfficeId INT,@LocationId INT,@ReturnId INT,@MethUserId INT;
  SELECT @OfficeId=g.TargetOfficeId,@LocationId=COALESCE(u.LocationId,(SELECT TOP(1) LocationId FROM dbo.t_UnloadLocation WHERE UnloadId=u.UnloadId AND IsActive=1 ORDER BY UnloadLocationId)) FROM dbo.t_UnloadTransaction u JOIN dbo.t_GateEntry g ON g.RSTNumber=u.RSTNumber WHERE u.UnloadId=@UnloadId;
  SELECT TOP(1) @MethUserId=UserId FROM dbo.sa05_user WHERE PersonId=@MethPersonId AND IsActive=1 ORDER BY UserId;
  IF @MethUserId IS NULL BEGIN ROLLBACK; THROW 51206,'Active Meth login was not found.',1; END;
  DECLARE @Created TABLE(OrderReturnId INT,ReturnNumber NVARCHAR(30));
  INSERT @Created EXEC dbo.sp_CreateOrderReturn @UnloadId=@UnloadId,@RequestedBags=@ReturnTotal,@SourceLocationId=@LocationId,@Reason=@ReturnReason,@UserId=@MethUserId,@OfficeId=@OfficeId;
  SELECT @ReturnId=OrderReturnId FROM @Created;
  INSERT dbo.tbl_OrderReturnBagDetail(OrderReturnId,BagTypeId,BagCount) SELECT @ReturnId,BagTypeId,BagCount FROM OPENJSON(@ReturnRows) WITH(BagTypeId INT,BagCount INT);
 END;
 COMMIT;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetMethDashboard @MethPersonId INT
AS
BEGIN
 SET NOCOUNT ON;
 DECLARE @OfficeId INT=(SELECT TOP(1) d.O05_Office_Id FROM dbo.P11_PersonDesignatation pd JOIN dbo.O12_Designatation d ON d.DesignationId=pd.O12_DesignationId AND d.IsActive=1 WHERE pd.P02_PersonId=@MethPersonId AND pd.IsActive=1 AND d.O10_PostId=6);
 SELECT ISNULL(p.PersonName,'Meth') MethName,ISNULL(o.OfficeName,'Company') CompanyName,
 (SELECT COUNT(*) FROM dbo.t_UnloadTransaction u WHERE u.MethId=@MethPersonId AND u.Status='Assigned' AND NOT EXISTS(SELECT 1 FROM dbo.t_WorkerAllocation w WHERE w.UnloadId=u.UnloadId AND ISNULL(w.BagCount,0)>0)) PendingWork,
 (SELECT COUNT(*) FROM dbo.t_UnloadTransaction u WHERE u.MethId=@MethPersonId AND EXISTS(SELECT 1 FROM dbo.t_WorkerAllocation w WHERE w.UnloadId=u.UnloadId AND ISNULL(w.BagCount,0)>0)) CompletedWork,
 (SELECT ISNULL(SUM(w.BagCount),0) FROM dbo.t_WorkerAllocation w JOIN dbo.t_UnloadTransaction u ON u.UnloadId=w.UnloadId WHERE u.MethId=@MethPersonId AND CAST(w.AllocationTime AS date)=CAST(GETDATE() AS date)) TodayHandledBags,
 (SELECT ISNULL(SUM(w.PalledariAmount),0) FROM dbo.t_WorkerAllocation w JOIN dbo.t_UnloadTransaction u ON u.UnloadId=w.UnloadId WHERE u.MethId=@MethPersonId) TotalPayable,
 (SELECT COUNT(*) FROM dbo.t_GateEntry g WHERE g.TargetOfficeId=@OfficeId AND g.Status='WaitingForSupervisor' AND NOT EXISTS(SELECT 1 FROM dbo.t_UnloadTransaction u WHERE u.RSTNumber=g.RSTNumber)) AwaitingAssignment
 FROM dbo.P02_Person p LEFT JOIN dbo.o05_office o ON o.OfficeId=@OfficeId WHERE p.PersonId=@MethPersonId;
 SELECT 'Pending' Name,CONVERT(decimal(18,2),COUNT(*)) Value FROM dbo.t_UnloadTransaction u WHERE u.MethId=@MethPersonId AND u.Status='Assigned' AND NOT EXISTS(SELECT 1 FROM dbo.t_WorkerAllocation w WHERE w.UnloadId=u.UnloadId)
 UNION ALL SELECT 'Completed',CONVERT(decimal(18,2),COUNT(*)) FROM dbo.t_UnloadTransaction u WHERE u.MethId=@MethPersonId AND EXISTS(SELECT 1 FROM dbo.t_WorkerAllocation w WHERE w.UnloadId=u.UnloadId);
 ;WITH d AS(SELECT CAST(DATEADD(day,-6,CAST(GETDATE() AS date)) AS date) Dt UNION ALL SELECT DATEADD(day,1,Dt) FROM d WHERE Dt<CAST(GETDATE() AS date)) SELECT FORMAT(d.Dt,'dd-MMM') DateLabel,ISNULL(SUM(CASE WHEN u.MethId=@MethPersonId THEN w.BagCount ELSE 0 END),0) Bags FROM d LEFT JOIN dbo.t_WorkerAllocation w ON CAST(w.AllocationTime AS date)=d.Dt LEFT JOIN dbo.t_UnloadTransaction u ON u.UnloadId=w.UnloadId GROUP BY d.Dt ORDER BY d.Dt OPTION(MAXRECURSION 7);
 SELECT ISNULL(NULLIF(w.WorkType,''),'Other') Name,CONVERT(decimal(18,2),SUM(w.BagCount)) Value FROM dbo.t_WorkerAllocation w JOIN dbo.t_UnloadTransaction u ON u.UnloadId=w.UnloadId WHERE u.MethId=@MethPersonId GROUP BY ISNULL(NULLIF(w.WorkType,''),'Other');
 SELECT g.RSTNumber,ISNULL(v.VehicleNumber,'') VehicleNumber,ISNULL(p.PersonName,'') PartyName,g.GateEntryTime FROM dbo.t_GateEntry g LEFT JOIN dbo.m_Vehicle v ON v.VehicleId=g.VehicleId LEFT JOIN dbo.P02_Person p ON p.PersonId=g.PartyId WHERE g.TargetOfficeId=@OfficeId AND g.Status='WaitingForSupervisor' AND NOT EXISTS(SELECT 1 FROM dbo.t_UnloadTransaction u WHERE u.RSTNumber=g.RSTNumber) ORDER BY g.GateEntryTime DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_SupervisorCompleteWorkerWork
    @UnloadId INT,
    @SupervisorId INT,
    @WorkerRows NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF ISJSON(@WorkerRows)<>1 THROW 51101,'Invalid worker work details.',1;

    DECLARE @Rows TABLE(WorkerId INT,WorkType NVARCHAR(20),BagTypeId INT,BagCount INT,PerBagCharge DECIMAL(18,2));
    INSERT @Rows SELECT WorkerId,WorkType,BagTypeId,BagCount,PerBagCharge
    FROM OPENJSON(@WorkerRows) WITH(WorkerId INT,WorkType NVARCHAR(20),BagTypeId INT,BagCount INT,PerBagCharge DECIMAL(18,2));
    IF NOT EXISTS(SELECT 1 FROM @Rows) THROW 51102,'At least one worker entry is required.',1;
    IF EXISTS(SELECT 1 FROM @Rows WHERE WorkerId<=0 OR WorkType NOT IN('Load','Unload','Loading','Unloading') OR BagTypeId<=0 OR BagCount<=0 OR PerBagCharge<0) THROW 51103,'One or more worker entries are invalid.',1;
    IF EXISTS(SELECT 1 FROM @Rows GROUP BY WorkerId,WorkType,BagTypeId HAVING COUNT(*)>1) THROW 51104,'Duplicate worker, work type and bag type is not allowed.',1;

    BEGIN TRAN;
    DECLARE @RSTNumber NVARCHAR(50),@Status NVARCHAR(20),@AssignedSupervisor INT;
    SELECT @RSTNumber=RSTNumber,@Status=Status,@AssignedSupervisor=SupervisorId FROM dbo.t_UnloadTransaction WITH(UPDLOCK,HOLDLOCK) WHERE UnloadId=@UnloadId;
    IF @RSTNumber IS NULL BEGIN ROLLBACK; THROW 51105,'Unload assignment was not found.',1; END;
    IF @AssignedSupervisor<>@SupervisorId BEGIN ROLLBACK; THROW 51106,'This RST is not assigned to the logged-in Supervisor.',1; END;
    IF @Status<>'Assigned' OR EXISTS(SELECT 1 FROM dbo.t_WorkerAllocation WHERE UnloadId=@UnloadId) BEGIN ROLLBACK; THROW 51107,'Worker work is already completed by Meth or Supervisor.',1; END;

    INSERT dbo.t_WorkerAllocation(UnloadId,WorkerId,PalledariAmount,AllocationTime,WorkType,BagTypeId,BagCount,PerBagCharge)
    SELECT @UnloadId,WorkerId,BagCount*PerBagCharge,SYSDATETIME(),WorkType,BagTypeId,BagCount,PerBagCharge FROM @Rows;
    DECLARE @TotalBags INT=(SELECT SUM(BagCount) FROM @Rows);
    UPDATE dbo.t_UnloadTransaction SET NumberOfBags=@TotalBags,Status='Unloaded',UnloadTime=SYSDATETIME() WHERE UnloadId=@UnloadId;
    UPDATE dbo.t_GateEntry SET Status='WaitingForTare' WHERE RSTNumber=@RSTNumber;
    COMMIT;
END;
GO
