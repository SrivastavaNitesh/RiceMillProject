SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID('dbo.tbl_OrderReturn','U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_OrderReturn
    (
        OrderReturnId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_OrderReturn PRIMARY KEY,
        ReturnNumber NVARCHAR(30) NOT NULL,
        UnloadId INT NOT NULL,
        RSTNumber NVARCHAR(50) NOT NULL,
        OfficeId INT NOT NULL,
        SourceLocationId INT NULL,
        RequestedBags INT NOT NULL,
        ApprovedBags INT NULL,
        ReturnReason NVARCHAR(500) NOT NULL,
        QualityRemarks NVARCHAR(1000) NULL,
        QualityDecision VARCHAR(20) NOT NULL CONSTRAINT DF_OrderReturn_Quality DEFAULT('Pending'),
        Status VARCHAR(30) NOT NULL CONSTRAINT DF_OrderReturn_Status DEFAULT('PendingQuality'),
        RequestedBy INT NOT NULL,
        RequestedAt DATETIME2(0) NOT NULL CONSTRAINT DF_OrderReturn_RequestedAt DEFAULT(SYSDATETIME()),
        ApprovedBy INT NULL,
        ApprovedAt DATETIME2(0) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_OrderReturn_Active DEFAULT(1),
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_tbl_OrderReturn_ReturnNumber UNIQUE(ReturnNumber),
        CONSTRAINT FK_OrderReturn_Unload FOREIGN KEY(UnloadId) REFERENCES dbo.t_UnloadTransaction(UnloadId),
        CONSTRAINT FK_OrderReturn_Office FOREIGN KEY(OfficeId) REFERENCES dbo.o05_office(OfficeId),
        CONSTRAINT FK_OrderReturn_Location FOREIGN KEY(SourceLocationId) REFERENCES dbo.m_LocationMaster(LocationId),
        CONSTRAINT CK_OrderReturn_Bags CHECK(RequestedBags > 0 AND (ApprovedBags IS NULL OR (ApprovedBags >= 0 AND ApprovedBags <= RequestedBags))),
        CONSTRAINT CK_OrderReturn_Decision CHECK(QualityDecision IN('Pending','Approved','Rejected')),
        CONSTRAINT CK_OrderReturn_Status CHECK(Status IN('PendingQuality','Approved','PartiallyProcessed','Completed','Rejected','Cancelled'))
    );
    CREATE UNIQUE INDEX UX_OrderReturn_ActiveUnload ON dbo.tbl_OrderReturn(UnloadId) WHERE IsActive=1;
END;

IF OBJECT_ID('dbo.tbl_OrderReturnDispatch','U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_OrderReturnDispatch
    (
        DispatchId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrderReturnDispatch PRIMARY KEY,
        OrderReturnId INT NOT NULL,
        DispatchNumber NVARCHAR(35) NOT NULL,
        DispatchBags INT NOT NULL,
        VehicleId INT NULL,
        GrossWeight DECIMAL(18,3) NULL,
        TareWeight DECIMAL(18,3) NULL,
        NetWeight AS (CASE WHEN GrossWeight IS NOT NULL AND TareWeight IS NOT NULL THEN GrossWeight-TareWeight END) PERSISTED,
        Status VARCHAR(30) NOT NULL CONSTRAINT DF_ReturnDispatch_Status DEFAULT('HandlingPending'),
        CreatedBy INT NOT NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_ReturnDispatch_CreatedAt DEFAULT(SYSDATETIME()),
        WeighedBy INT NULL,
        WeighedAt DATETIME2(0) NULL,
        GateOutBy INT NULL,
        GateOutAt DATETIME2(0) NULL,
        Remarks NVARCHAR(500) NULL,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_OrderReturnDispatch_Number UNIQUE(DispatchNumber),
        CONSTRAINT FK_ReturnDispatch_Return FOREIGN KEY(OrderReturnId) REFERENCES dbo.tbl_OrderReturn(OrderReturnId),
        CONSTRAINT FK_ReturnDispatch_Vehicle FOREIGN KEY(VehicleId) REFERENCES dbo.m_Vehicle(VehicleId),
        CONSTRAINT CK_ReturnDispatch_Bags CHECK(DispatchBags>0),
        CONSTRAINT CK_ReturnDispatch_Weight CHECK((GrossWeight IS NULL AND TareWeight IS NULL) OR (GrossWeight>0 AND TareWeight>=0 AND GrossWeight>=TareWeight)),
        CONSTRAINT CK_ReturnDispatch_Status CHECK(Status IN('HandlingPending','ReadyForWeighment','ReadyForGateOut','OutwardCompleted','Cancelled'))
    );
END;

IF OBJECT_ID('dbo.tbl_OrderReturnMovement','U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_OrderReturnMovement
    (
        MovementId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrderReturnMovement PRIMARY KEY,
        OrderReturnId INT NOT NULL,
        DispatchId INT NULL,
        MovementType VARCHAR(30) NOT NULL,
        FromLocationId INT NULL,
        ToLocationId INT NULL,
        BagTypeId INT NOT NULL,
        Bags INT NOT NULL,
        Status VARCHAR(20) NOT NULL CONSTRAINT DF_ReturnMovement_Status DEFAULT('Completed'),
        MovementAt DATETIME2(0) NOT NULL CONSTRAINT DF_ReturnMovement_At DEFAULT(SYSDATETIME()),
        RecordedBy INT NOT NULL,
        Remarks NVARCHAR(500) NULL,
        CONSTRAINT FK_ReturnMovement_Return FOREIGN KEY(OrderReturnId) REFERENCES dbo.tbl_OrderReturn(OrderReturnId),
        CONSTRAINT FK_ReturnMovement_Dispatch FOREIGN KEY(DispatchId) REFERENCES dbo.tbl_OrderReturnDispatch(DispatchId),
        CONSTRAINT FK_ReturnMovement_FromLocation FOREIGN KEY(FromLocationId) REFERENCES dbo.m_LocationMaster(LocationId),
        CONSTRAINT FK_ReturnMovement_ToLocation FOREIGN KEY(ToLocationId) REFERENCES dbo.m_LocationMaster(LocationId),
        CONSTRAINT FK_ReturnMovement_BagType FOREIGN KEY(BagTypeId) REFERENCES dbo.m_BagType(BagTypeId),
        CONSTRAINT CK_ReturnMovement_Bags CHECK(Bags>0),
        CONSTRAINT CK_ReturnMovement_Type CHECK(MovementType IN('ReturnTruckLoading','InternalShiftLoading','InternalShiftUnloading')),
        CONSTRAINT CK_ReturnMovement_Status CHECK(Status IN('Completed','Reversed'))
    );
END;

IF OBJECT_ID('dbo.tbl_OrderReturnWorker','U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_OrderReturnWorker
    (
        ReturnWorkerId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrderReturnWorker PRIMARY KEY,
        MovementId INT NOT NULL,
        WorkerId INT NOT NULL,
        BagsHandled INT NOT NULL,
        PerBagRate DECIMAL(18,2) NOT NULL,
        Amount AS (CONVERT(DECIMAL(18,2),BagsHandled*PerBagRate)) PERSISTED,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_ReturnWorker_CreatedAt DEFAULT(SYSDATETIME()),
        CONSTRAINT FK_ReturnWorker_Movement FOREIGN KEY(MovementId) REFERENCES dbo.tbl_OrderReturnMovement(MovementId),
        CONSTRAINT FK_ReturnWorker_Person FOREIGN KEY(WorkerId) REFERENCES dbo.P02_Person(PersonId),
        CONSTRAINT CK_ReturnWorker_Bags CHECK(BagsHandled>0),
        CONSTRAINT CK_ReturnWorker_Rate CHECK(PerBagRate>=0),
        CONSTRAINT UQ_ReturnWorker_MovementWorker UNIQUE(MovementId,WorkerId)
    );
END;

IF OBJECT_ID('dbo.tbl_OrderReturnStatusHistory','U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_OrderReturnStatusHistory
    (
        HistoryId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrderReturnHistory PRIMARY KEY,
        OrderReturnId INT NOT NULL,
        DispatchId INT NULL,
        OldStatus VARCHAR(30) NULL,
        NewStatus VARCHAR(30) NOT NULL,
        Remarks NVARCHAR(500) NULL,
        ChangedBy INT NOT NULL,
        ChangedAt DATETIME2(0) NOT NULL CONSTRAINT DF_ReturnHistory_At DEFAULT(SYSDATETIME()),
        CONSTRAINT FK_ReturnHistory_Return FOREIGN KEY(OrderReturnId) REFERENCES dbo.tbl_OrderReturn(OrderReturnId),
        CONSTRAINT FK_ReturnHistory_Dispatch FOREIGN KEY(DispatchId) REFERENCES dbo.tbl_OrderReturnDispatch(DispatchId)
    );
END;

IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.tbl_OrderReturnMovement') AND name='UX_ReturnMovement_ActiveTruckLoading')
    CREATE UNIQUE INDEX UX_ReturnMovement_ActiveTruckLoading ON dbo.tbl_OrderReturnMovement(DispatchId)
    WHERE DispatchId IS NOT NULL AND MovementType='ReturnTruckLoading' AND Status='Completed';
GO

CREATE OR ALTER PROCEDURE dbo.sp_CreateOrderReturn
 @UnloadId INT,@RequestedBags INT,@SourceLocationId INT=NULL,@Reason NVARCHAR(500),@UserId INT,@OfficeId INT
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @RequestedBags<=0 THROW 51001,'Return bags must be greater than zero.',1;
 IF NULLIF(LTRIM(RTRIM(@Reason)),'') IS NULL THROW 51002,'Return reason is required.',1;
 DECLARE @RST NVARCHAR(50),@ScopeOffice INT,@Available INT;
 SELECT @RST=u.RSTNumber,@ScopeOffice=g.TargetOfficeId,
        @Available=COALESCE(a.SupervisorTotalBags,u.NumberOfBags,w.WorkerBags)
 FROM dbo.t_UnloadTransaction u JOIN dbo.t_GateEntry g ON g.RSTNumber=u.RSTNumber
 OUTER APPLY(SELECT TOP(1) SupervisorTotalBags FROM dbo.t_SupervisorUnloadAction x WHERE x.UnloadId=u.UnloadId ORDER BY x.SupervisorActionId DESC)a
 OUTER APPLY(SELECT SUM(BagCount) WorkerBags FROM dbo.t_WorkerAllocation x WHERE x.UnloadId=u.UnloadId)w
 WHERE u.UnloadId=@UnloadId;
 IF @RST IS NULL THROW 51003,'Unloading/RST was not found.',1;
 IF @ScopeOffice<>@OfficeId THROW 51004,'RST does not belong to the logged-in company.',1;
 IF ISNULL(@Available,0)<=0 THROW 51005,'Recorded unloaded bags are required before return request.',1;
 IF @RequestedBags>@Available THROW 51006,'Return bags cannot exceed recorded unloaded bags.',1;
 IF @SourceLocationId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.t_OfficeLocationMapping WHERE OfficeId=@OfficeId AND LocationId=@SourceLocationId AND IsActive=1) THROW 51007,'Selected source location is not mapped to this company.',1;
 IF EXISTS(SELECT 1 FROM dbo.tbl_OrderReturn WHERE UnloadId=@UnloadId AND IsActive=1 AND Status NOT IN('Completed','Rejected','Cancelled')) THROW 51008,'An active return request already exists for this RST.',1;
 BEGIN TRAN;
 DECLARE @Id INT,@Number NVARCHAR(30);
 INSERT dbo.tbl_OrderReturn(ReturnNumber,UnloadId,RSTNumber,OfficeId,SourceLocationId,RequestedBags,ReturnReason,RequestedBy)
 VALUES(CONCAT('TMP-',RIGHT(REPLACE(CONVERT(VARCHAR(36),NEWID()),'-',''),20)),@UnloadId,@RST,@OfficeId,@SourceLocationId,@RequestedBags,LTRIM(RTRIM(@Reason)),@UserId);
 SET @Id=SCOPE_IDENTITY(); SET @Number=CONCAT('RET-',YEAR(GETDATE()),'-',RIGHT('000000'+CONVERT(VARCHAR(10),@Id),6));
 UPDATE dbo.tbl_OrderReturn SET ReturnNumber=@Number WHERE OrderReturnId=@Id;
 INSERT dbo.tbl_OrderReturnStatusHistory(OrderReturnId,NewStatus,Remarks,ChangedBy) VALUES(@Id,'PendingQuality','Return request created',@UserId);
 COMMIT; SELECT @Id OrderReturnId,@Number ReturnNumber;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_DecideOrderReturn
 @OrderReturnId INT,@Decision VARCHAR(20),@ApprovedBags INT=NULL,@Remarks NVARCHAR(1000)=NULL,@UserId INT,@OfficeId INT
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @Decision NOT IN('Approved','Rejected') THROW 51020,'Invalid quality decision.',1;
 DECLARE @Requested INT,@Old VARCHAR(30);
 SELECT @Requested=RequestedBags,@Old=Status FROM dbo.tbl_OrderReturn WHERE OrderReturnId=@OrderReturnId AND OfficeId=@OfficeId AND IsActive=1;
 IF @Requested IS NULL THROW 51021,'Return request not found for this company.',1;
 IF @Old<>'PendingQuality' THROW 51022,'Only pending quality requests can be decided.',1;
 IF @Decision='Approved' AND (ISNULL(@ApprovedBags,0)<=0 OR @ApprovedBags>@Requested) THROW 51023,'Approved bags must be between 1 and requested bags.',1;
 UPDATE dbo.tbl_OrderReturn SET QualityDecision=@Decision,ApprovedBags=CASE WHEN @Decision='Approved' THEN @ApprovedBags ELSE 0 END,
 QualityRemarks=@Remarks,Status=CASE WHEN @Decision='Approved' THEN 'Approved' ELSE 'Rejected' END,ApprovedBy=@UserId,ApprovedAt=SYSDATETIME(),IsActive=CASE WHEN @Decision='Rejected' THEN 0 ELSE 1 END WHERE OrderReturnId=@OrderReturnId;
 INSERT dbo.tbl_OrderReturnStatusHistory(OrderReturnId,OldStatus,NewStatus,Remarks,ChangedBy) VALUES(@OrderReturnId,@Old,CASE WHEN @Decision='Approved' THEN 'Approved' ELSE 'Rejected' END,@Remarks,@UserId);
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_CreateOrderReturnDispatch
 @OrderReturnId INT,@DispatchBags INT,@VehicleId INT=NULL,@Remarks NVARCHAR(500)=NULL,@UserId INT,@OfficeId INT
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @DispatchBags<=0 THROW 51030,'Dispatch bags must be greater than zero.',1;
 BEGIN TRAN;
 DECLARE @Approved INT,@Already INT,@Id INT,@Number NVARCHAR(35);
 SELECT @Approved=ApprovedBags FROM dbo.tbl_OrderReturn WITH(UPDLOCK,HOLDLOCK) WHERE OrderReturnId=@OrderReturnId AND OfficeId=@OfficeId AND Status IN('Approved','PartiallyProcessed');
 IF @Approved IS NULL BEGIN ROLLBACK; THROW 51031,'Approved return request was not found.',1; END;
 SELECT @Already=ISNULL(SUM(DispatchBags),0) FROM dbo.tbl_OrderReturnDispatch WHERE OrderReturnId=@OrderReturnId AND Status<>'Cancelled';
 IF @DispatchBags>@Approved-@Already BEGIN ROLLBACK; THROW 51032,'Dispatch bags cannot exceed remaining approved bags.',1; END;
 IF @VehicleId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.m_Vehicle WHERE VehicleId=@VehicleId AND IsActive=1) BEGIN ROLLBACK; THROW 51033,'Selected vehicle is not active.',1; END;
 INSERT dbo.tbl_OrderReturnDispatch(OrderReturnId,DispatchNumber,DispatchBags,VehicleId,CreatedBy,Remarks) VALUES(@OrderReturnId,CONCAT('TMP-',RIGHT(REPLACE(CONVERT(VARCHAR(36),NEWID()),'-',''),24)),@DispatchBags,@VehicleId,@UserId,@Remarks);
 SET @Id=SCOPE_IDENTITY(); SET @Number=CONCAT((SELECT ReturnNumber FROM dbo.tbl_OrderReturn WHERE OrderReturnId=@OrderReturnId),'-D',RIGHT('00'+CONVERT(VARCHAR(5),(SELECT COUNT(*) FROM dbo.tbl_OrderReturnDispatch WHERE OrderReturnId=@OrderReturnId)),2));
 UPDATE dbo.tbl_OrderReturnDispatch SET DispatchNumber=@Number WHERE DispatchId=@Id;
 UPDATE dbo.tbl_OrderReturn SET Status=CASE WHEN @Already+@DispatchBags<@Approved THEN 'PartiallyProcessed' ELSE Status END WHERE OrderReturnId=@OrderReturnId;
 INSERT dbo.tbl_OrderReturnStatusHistory(OrderReturnId,DispatchId,OldStatus,NewStatus,Remarks,ChangedBy) VALUES(@OrderReturnId,@Id,'Approved','HandlingPending','Partial return dispatch created',@UserId);
 COMMIT; SELECT @Id DispatchId,@Number DispatchNumber;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_SaveOrderReturnMovement
 @OrderReturnId INT,@DispatchId INT=NULL,@MovementType VARCHAR(30),@FromLocationId INT=NULL,@ToLocationId INT=NULL,@BagTypeId INT,@Bags INT,@WorkersJson NVARCHAR(MAX),@Remarks NVARCHAR(500)=NULL,@UserId INT,@OfficeId INT
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @MovementType NOT IN('ReturnTruckLoading','InternalShiftLoading','InternalShiftUnloading') THROW 51040,'Invalid movement type.',1;
 IF @Bags<=0 THROW 51041,'Movement bags must be greater than zero.',1;
 IF NOT EXISTS(SELECT 1 FROM dbo.tbl_OrderReturn WHERE OrderReturnId=@OrderReturnId AND OfficeId=@OfficeId AND Status IN('Approved','PartiallyProcessed')) THROW 51042,'Active approved return request not found.',1;
 IF @MovementType='ReturnTruckLoading' AND (@DispatchId IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.tbl_OrderReturnDispatch WHERE DispatchId=@DispatchId AND OrderReturnId=@OrderReturnId AND Status='HandlingPending')) THROW 51043,'Active return dispatch is required for truck loading.',1;
 IF @MovementType='ReturnTruckLoading' AND @Bags<>(SELECT DispatchBags FROM dbo.tbl_OrderReturnDispatch WHERE DispatchId=@DispatchId) THROW 51044,'Truck loading bags must equal dispatch bags.',1;
 IF @MovementType='ReturnTruckLoading' AND EXISTS(SELECT 1 FROM dbo.tbl_OrderReturnMovement WHERE DispatchId=@DispatchId AND MovementType='ReturnTruckLoading' AND Status='Completed') THROW 51048,'Truck loading is already recorded for this dispatch.',1;
 IF @MovementType LIKE 'InternalShift%' AND (@FromLocationId IS NULL OR @ToLocationId IS NULL OR @FromLocationId=@ToLocationId) THROW 51049,'Different source and destination locations are required for internal shifting.',1;
 IF @MovementType='ReturnTruckLoading' AND @FromLocationId IS NULL THROW 51054,'Source location is required for return truck loading.',1;
 IF EXISTS(SELECT 1 FROM (VALUES(@FromLocationId),(@ToLocationId)) x(LocationId) WHERE x.LocationId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.t_OfficeLocationMapping m WHERE m.OfficeId=@OfficeId AND m.LocationId=x.LocationId AND m.IsActive=1)) THROW 51055,'Movement location is not mapped to this company.',1;
 IF NOT EXISTS(SELECT 1 FROM dbo.m_BagType WHERE BagTypeId=@BagTypeId AND IsActive=1) THROW 51056,'Selected bag type is not active.',1;
 DECLARE @WorkerTotal INT=(SELECT SUM(BagsHandled) FROM OPENJSON(@WorkersJson) WITH(WorkerId INT,BagsHandled INT,PerBagRate DECIMAL(18,2)));
 IF ISNULL(@WorkerTotal,0)<>@Bags THROW 51045,'Worker handled bags total must equal movement bags.',1;
 IF EXISTS(SELECT 1 FROM OPENJSON(@WorkersJson) WITH(WorkerId INT,BagsHandled INT,PerBagRate DECIMAL(18,2)) GROUP BY WorkerId HAVING COUNT(*)>1) THROW 51046,'Same worker cannot be entered twice in one movement.',1;
 IF EXISTS(SELECT 1 FROM OPENJSON(@WorkersJson) WITH(WorkerId INT,BagsHandled INT,PerBagRate DECIMAL(18,2)) WHERE WorkerId<=0 OR BagsHandled<=0 OR PerBagRate<0) THROW 51047,'Invalid worker bags or rate.',1;
 IF EXISTS(SELECT 1 FROM OPENJSON(@WorkersJson) WITH(WorkerId INT) w WHERE NOT EXISTS(
    SELECT 1 FROM dbo.P02_Person p
    JOIN dbo.P11_PersonDesignatation pd ON pd.P02_PersonId=p.PersonId AND pd.IsActive=1
    JOIN dbo.O12_Designatation d ON d.DesignationId=pd.O12_DesignationId AND d.IsActive=1 AND d.O10_PostId=7
    WHERE p.PersonId=w.WorkerId AND p.IsActive=1 AND d.O05_Office_Id=@OfficeId
 )) THROW 51057,'One or more selected workers are inactive or outside this company.',1;
 BEGIN TRAN;
 DECLARE @MovementId INT;
 INSERT dbo.tbl_OrderReturnMovement(OrderReturnId,DispatchId,MovementType,FromLocationId,ToLocationId,BagTypeId,Bags,RecordedBy,Remarks) VALUES(@OrderReturnId,@DispatchId,@MovementType,@FromLocationId,@ToLocationId,@BagTypeId,@Bags,@UserId,@Remarks);
 SET @MovementId=SCOPE_IDENTITY();
 INSERT dbo.tbl_OrderReturnWorker(MovementId,WorkerId,BagsHandled,PerBagRate)
 SELECT @MovementId,WorkerId,BagsHandled,PerBagRate FROM OPENJSON(@WorkersJson) WITH(WorkerId INT,BagsHandled INT,PerBagRate DECIMAL(18,2));
 IF @MovementType='ReturnTruckLoading' UPDATE dbo.tbl_OrderReturnDispatch SET Status='ReadyForWeighment' WHERE DispatchId=@DispatchId;
 COMMIT; SELECT @MovementId MovementId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_SaveOrderReturnWeight
 @DispatchId INT,@GrossWeight DECIMAL(18,3),@TareWeight DECIMAL(18,3),@VehicleId INT,@UserId INT,@OfficeId INT
AS
BEGIN
 SET NOCOUNT ON;
 IF @GrossWeight<=0 OR @TareWeight<0 OR @GrossWeight<@TareWeight THROW 51050,'Valid gross and tare weights are required.',1;
 IF NOT EXISTS(SELECT 1 FROM dbo.m_Vehicle WHERE VehicleId=@VehicleId AND IsActive=1) THROW 51052,'Selected vehicle is not active.',1;
 UPDATE dispatch SET GrossWeight=@GrossWeight,TareWeight=@TareWeight,VehicleId=@VehicleId,Status='ReadyForGateOut',WeighedBy=@UserId,WeighedAt=SYSDATETIME()
 FROM dbo.tbl_OrderReturnDispatch dispatch JOIN dbo.tbl_OrderReturn header ON header.OrderReturnId=dispatch.OrderReturnId
 WHERE dispatch.DispatchId=@DispatchId AND header.OfficeId=@OfficeId AND dispatch.Status='ReadyForWeighment';
 IF @@ROWCOUNT=0 THROW 51051,'Dispatch is not ready for return weighment.',1;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetOrderReturnPage @OfficeId INT
AS
BEGIN
 SET NOCOUNT ON;
 SELECT r.OrderReturnId,r.ReturnNumber,r.UnloadId,r.RSTNumber,ISNULL(v.VehicleNumber,'') VehicleNumber,ISNULL(p.PersonName,'') PartyName,
        ISNULL(l.LocationName,'') SourceLocationName,r.RequestedBags,ISNULL(r.ApprovedBags,0) ApprovedBags,
        ISNULL((SELECT SUM(d.DispatchBags) FROM dbo.tbl_OrderReturnDispatch d WHERE d.OrderReturnId=r.OrderReturnId AND d.Status<>'Cancelled'),0) DispatchedBags,
        r.ReturnReason,ISNULL(r.QualityRemarks,'') QualityRemarks,r.QualityDecision,r.Status,r.RequestedAt
 FROM dbo.tbl_OrderReturn r
 JOIN dbo.t_UnloadTransaction u ON u.UnloadId=r.UnloadId
 LEFT JOIN dbo.t_GateEntry g ON g.RSTNumber=u.RSTNumber
 LEFT JOIN dbo.m_Vehicle v ON v.VehicleId=g.VehicleId
 LEFT JOIN dbo.P02_Person p ON p.PersonId=g.PartyId
 LEFT JOIN dbo.m_LocationMaster l ON l.LocationId=r.SourceLocationId
 WHERE r.OfficeId=@OfficeId ORDER BY r.OrderReturnId DESC;

 SELECT u.UnloadId,u.RSTNumber,ISNULL(v.VehicleNumber,'') VehicleNumber,ISNULL(p.PersonName,'') PartyName,
        COALESCE(a.SupervisorTotalBags,u.NumberOfBags,w.WorkerBags,0) AvailableBags
 FROM dbo.t_UnloadTransaction u
 JOIN dbo.t_GateEntry g ON g.RSTNumber=u.RSTNumber
 LEFT JOIN dbo.m_Vehicle v ON v.VehicleId=g.VehicleId LEFT JOIN dbo.P02_Person p ON p.PersonId=g.PartyId
 OUTER APPLY(SELECT TOP(1) SupervisorTotalBags FROM dbo.t_SupervisorUnloadAction x WHERE x.UnloadId=u.UnloadId ORDER BY x.SupervisorActionId DESC)a
 OUTER APPLY(SELECT SUM(BagCount) WorkerBags FROM dbo.t_WorkerAllocation x WHERE x.UnloadId=u.UnloadId)w
 WHERE g.TargetOfficeId=@OfficeId AND COALESCE(a.SupervisorTotalBags,u.NumberOfBags,w.WorkerBags,0)>0
 AND NOT EXISTS(SELECT 1 FROM dbo.tbl_OrderReturn r WHERE r.UnloadId=u.UnloadId AND r.IsActive=1 AND r.Status NOT IN('Completed','Rejected','Cancelled'))
 ORDER BY u.UnloadId DESC;

 SELECT l.LocationId Id,l.LocationName Name FROM dbo.m_LocationMaster l JOIN dbo.t_OfficeLocationMapping m ON m.LocationId=l.LocationId WHERE m.OfficeId=@OfficeId AND m.IsActive=1 AND l.IsActive=1 ORDER BY l.LocationName;
 SELECT VehicleId Id,VehicleNumber Name FROM dbo.m_Vehicle WHERE IsActive=1 ORDER BY VehicleNumber;
 SELECT BagTypeId Id,BagTypeName Name FROM dbo.m_BagType WHERE IsActive=1 ORDER BY BagTypeName;
 SELECT DISTINCT p.PersonId Id,p.PersonName Name FROM dbo.P02_Person p
 JOIN dbo.P11_PersonDesignatation pd ON pd.P02_PersonId=p.PersonId AND pd.IsActive=1
 JOIN dbo.O12_Designatation d ON d.DesignationId=pd.O12_DesignationId AND d.IsActive=1 AND d.O10_PostId=7
 WHERE p.IsActive=1 AND d.O05_Office_Id=@OfficeId ORDER BY p.PersonName;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetOrderReturnDetails @OrderReturnId INT,@OfficeId INT
AS
BEGIN
 SET NOCOUNT ON;
 SELECT r.OrderReturnId,r.ReturnNumber,r.UnloadId,r.RSTNumber,ISNULL(v.VehicleNumber,'') VehicleNumber,ISNULL(p.PersonName,'') PartyName,
        ISNULL(l.LocationName,'') SourceLocationName,r.RequestedBags,ISNULL(r.ApprovedBags,0) ApprovedBags,
        ISNULL((SELECT SUM(d.DispatchBags) FROM dbo.tbl_OrderReturnDispatch d WHERE d.OrderReturnId=r.OrderReturnId AND d.Status<>'Cancelled'),0) DispatchedBags,
        r.ReturnReason,ISNULL(r.QualityRemarks,'') QualityRemarks,r.QualityDecision,r.Status,r.RequestedAt
 FROM dbo.tbl_OrderReturn r JOIN dbo.t_UnloadTransaction u ON u.UnloadId=r.UnloadId LEFT JOIN dbo.t_GateEntry g ON g.RSTNumber=u.RSTNumber
 LEFT JOIN dbo.m_Vehicle v ON v.VehicleId=g.VehicleId LEFT JOIN dbo.P02_Person p ON p.PersonId=g.PartyId LEFT JOIN dbo.m_LocationMaster l ON l.LocationId=r.SourceLocationId
 WHERE r.OrderReturnId=@OrderReturnId AND r.OfficeId=@OfficeId;
 SELECT d.DispatchId,d.DispatchNumber,d.DispatchBags,d.VehicleId,ISNULL(v.VehicleNumber,'') VehicleNumber,d.GrossWeight,d.TareWeight,d.NetWeight,d.Status,d.CreatedAt
 FROM dbo.tbl_OrderReturnDispatch d JOIN dbo.tbl_OrderReturn r ON r.OrderReturnId=d.OrderReturnId LEFT JOIN dbo.m_Vehicle v ON v.VehicleId=d.VehicleId
 WHERE d.OrderReturnId=@OrderReturnId AND r.OfficeId=@OfficeId ORDER BY d.DispatchId;
 SELECT m.MovementId,m.DispatchId,m.MovementType,ISNULL(fl.LocationName,'') FromLocation,ISNULL(tl.LocationName,'') ToLocation,b.BagTypeName BagType,m.Bags,
        ISNULL((SELECT SUM(w.Amount) FROM dbo.tbl_OrderReturnWorker w WHERE w.MovementId=m.MovementId),0) Amount,m.MovementAt
 FROM dbo.tbl_OrderReturnMovement m JOIN dbo.tbl_OrderReturn r ON r.OrderReturnId=m.OrderReturnId JOIN dbo.m_BagType b ON b.BagTypeId=m.BagTypeId
 LEFT JOIN dbo.m_LocationMaster fl ON fl.LocationId=m.FromLocationId LEFT JOIN dbo.m_LocationMaster tl ON tl.LocationId=m.ToLocationId
 WHERE m.OrderReturnId=@OrderReturnId AND r.OfficeId=@OfficeId AND m.Status='Completed' ORDER BY m.MovementId;
 SELECT h.NewStatus,ISNULL(h.Remarks,'') Remarks,ISNULL(p.PersonName,CONCAT('User #',h.ChangedBy)) ChangedBy,h.ChangedAt
 FROM dbo.tbl_OrderReturnStatusHistory h JOIN dbo.tbl_OrderReturn r ON r.OrderReturnId=h.OrderReturnId LEFT JOIN dbo.sa05_user u ON u.UserId=h.ChangedBy LEFT JOIN dbo.P02_Person p ON p.PersonId=u.PersonId
 WHERE h.OrderReturnId=@OrderReturnId AND r.OfficeId=@OfficeId ORDER BY h.HistoryId DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_CompleteOrderReturnOutward
 @DispatchId INT,@UserId INT,@OfficeId INT
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON; BEGIN TRAN;
 DECLARE @ReturnId INT,@Old VARCHAR(30),@Approved INT,@Completed INT;
 SELECT @ReturnId=d.OrderReturnId,@Old=d.Status FROM dbo.tbl_OrderReturnDispatch d JOIN dbo.tbl_OrderReturn r ON r.OrderReturnId=d.OrderReturnId WHERE d.DispatchId=@DispatchId AND r.OfficeId=@OfficeId;
 IF @Old<>'ReadyForGateOut' BEGIN ROLLBACK; THROW 51060,'Return dispatch is not ready for gate outward.',1; END;
 UPDATE dbo.tbl_OrderReturnDispatch SET Status='OutwardCompleted',GateOutBy=@UserId,GateOutAt=SYSDATETIME() WHERE DispatchId=@DispatchId;
 SELECT @Approved=ApprovedBags FROM dbo.tbl_OrderReturn WHERE OrderReturnId=@ReturnId;
 SELECT @Completed=ISNULL(SUM(DispatchBags),0) FROM dbo.tbl_OrderReturnDispatch WHERE OrderReturnId=@ReturnId AND Status='OutwardCompleted';
 UPDATE dbo.tbl_OrderReturn SET Status=CASE WHEN @Completed>=@Approved THEN 'Completed' ELSE 'PartiallyProcessed' END,IsActive=CASE WHEN @Completed>=@Approved THEN 0 ELSE 1 END WHERE OrderReturnId=@ReturnId;
 INSERT dbo.tbl_OrderReturnStatusHistory(OrderReturnId,DispatchId,OldStatus,NewStatus,Remarks,ChangedBy) VALUES(@ReturnId,@DispatchId,@Old,'OutwardCompleted','Return vehicle gate outward completed',@UserId);
 COMMIT;
END;
GO
