SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetOrderReturnPage
    @OfficeId INT
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
    WHERE r.OfficeId=@OfficeId
    ORDER BY r.OrderReturnId DESC;

    SELECT ISNULL(u.UnloadId,0) UnloadId,rm.RSTNumber,ISNULL(v.VehicleNumber,'') VehicleNumber,ISNULL(p.PersonName,'') PartyName,
           ISNULL(COALESCE(a.SupervisorTotalBags,u.NumberOfBags,w.WorkerBags),0) AvailableBags,
           ISNULL(ret.RequestedBags,0) ReturnBags,ISNULL(ret.OrderReturnId,0) OrderReturnId,
           ISNULL(ret.ReturnNumber,'') ReturnNumber,ISNULL(ret.Status,'') ReturnStatus,
           ISNULL(s.StageCode,'') StageCode,ISNULL(s.StageName,rm.CurrentStatus) StageName,rm.UpdatedAt LastUpdatedAt,
           CONVERT(bit,CASE WHEN ret.OrderReturnId IS NULL
                                 AND u.UnloadId IS NOT NULL
                                 AND ISNULL(COALESCE(a.SupervisorTotalBags,u.NumberOfBags,w.WorkerBags),0)>0
                                 AND s.StageCode IN('RST_GENERATED','SUPERVISOR_ASSIGNMENT','SUPERVISOR_FINAL')
                            THEN 1 ELSE 0 END) CanCreateReturn,
           CASE WHEN ret.OrderReturnId IS NOT NULL THEN 'Return is already recorded.'
                WHEN u.UnloadId IS NOT NULL
                     AND ISNULL(COALESCE(a.SupervisorTotalBags,u.NumberOfBags,w.WorkerBags),0)>0
                     AND s.StageCode IN('RST_GENERATED','SUPERVISOR_ASSIGNMENT','SUPERVISOR_FINAL') THEN 'Return action is available.'
                WHEN s.StageCode IN('LAB_REPORT','LAB_REPORT_PARTIAL','LAB_REPORT_COMPLETED','TARE_WEIGHT','GATE_OUTWARD','COMPLETED','BILL_GENERATED') THEN 'Return window closed after Lab workflow started.'
                WHEN u.UnloadId IS NULL THEN 'Unloading entry is pending.'
                WHEN ISNULL(COALESCE(a.SupervisorTotalBags,u.NumberOfBags,w.WorkerBags),0)<=0 THEN 'Recorded bag details are pending.'
                ELSE 'Return action is not available at this stage.' END ActionMessage
    FROM dbo.t_RSTMaster rm
    JOIN dbo.t_GateEntry g ON g.RSTNumber=rm.RSTNumber AND g.TargetOfficeId=@OfficeId
    LEFT JOIN dbo.m_RSTStageMaster s ON s.StageId=rm.CurrentStageId
    LEFT JOIN dbo.m_Vehicle v ON v.VehicleId=g.VehicleId
    LEFT JOIN dbo.P02_Person p ON p.PersonId=g.PartyId
    OUTER APPLY(SELECT TOP(1) x.UnloadId,x.NumberOfBags FROM dbo.t_UnloadTransaction x WHERE x.RSTNumber=rm.RSTNumber ORDER BY x.UnloadId DESC)u
    OUTER APPLY(SELECT TOP(1) x.SupervisorTotalBags FROM dbo.t_SupervisorUnloadAction x WHERE x.UnloadId=u.UnloadId ORDER BY x.SupervisorActionId DESC)a
    OUTER APPLY(SELECT SUM(x.BagCount) WorkerBags FROM dbo.t_WorkerAllocation x WHERE x.UnloadId=u.UnloadId)w
    OUTER APPLY(SELECT TOP(1) x.OrderReturnId,x.ReturnNumber,x.RequestedBags,x.Status FROM dbo.tbl_OrderReturn x WHERE x.RSTNumber=rm.RSTNumber AND x.IsActive=1 ORDER BY x.OrderReturnId DESC)ret
    ORDER BY rm.UpdatedAt DESC,rm.RSTMasterId DESC;

    SELECT l.LocationId Id,l.LocationName Name
    FROM dbo.m_LocationMaster l
    JOIN dbo.t_OfficeLocationMapping m ON m.LocationId=l.LocationId
    WHERE m.OfficeId=@OfficeId AND m.IsActive=1 AND l.IsActive=1
    ORDER BY l.LocationName;

    SELECT VehicleId Id,VehicleNumber Name FROM dbo.m_Vehicle WHERE IsActive=1 ORDER BY VehicleNumber;
    SELECT BagTypeId Id,BagTypeName Name FROM dbo.m_BagType WHERE IsActive=1 ORDER BY BagTypeName;
    SELECT DISTINCT p.PersonId Id,p.PersonName Name
    FROM dbo.P02_Person p
    JOIN dbo.P11_PersonDesignatation pd ON pd.P02_PersonId=p.PersonId AND pd.IsActive=1
    JOIN dbo.O12_Designatation d ON d.DesignationId=pd.O12_DesignationId AND d.IsActive=1 AND d.O10_PostId=7
    WHERE p.IsActive=1 AND d.O05_Office_Id=@OfficeId
    ORDER BY p.PersonName;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_CreateOrderReturn
    @UnloadId INT,@RequestedBags INT,@SourceLocationId INT=NULL,@Reason NVARCHAR(500),@UserId INT,@OfficeId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @RequestedBags<=0 THROW 51001,'Return bags must be greater than zero.',1;
    IF NULLIF(LTRIM(RTRIM(@Reason)),'') IS NULL THROW 51002,'Return reason is required.',1;

    DECLARE @RST NVARCHAR(50),@ScopeOffice INT,@Available INT,@StageCode VARCHAR(50);
    SELECT @RST=u.RSTNumber,@ScopeOffice=g.TargetOfficeId,
           @Available=COALESCE(a.SupervisorTotalBags,u.NumberOfBags,w.WorkerBags),@StageCode=s.StageCode
    FROM dbo.t_UnloadTransaction u
    JOIN dbo.t_GateEntry g ON g.RSTNumber=u.RSTNumber
    LEFT JOIN dbo.t_RSTMaster rm ON rm.RSTNumber=u.RSTNumber
    LEFT JOIN dbo.m_RSTStageMaster s ON s.StageId=rm.CurrentStageId
    OUTER APPLY(SELECT TOP(1) SupervisorTotalBags FROM dbo.t_SupervisorUnloadAction x WHERE x.UnloadId=u.UnloadId ORDER BY x.SupervisorActionId DESC)a
    OUTER APPLY(SELECT SUM(BagCount) WorkerBags FROM dbo.t_WorkerAllocation x WHERE x.UnloadId=u.UnloadId)w
    WHERE u.UnloadId=@UnloadId;

    IF @RST IS NULL THROW 51003,'Unloading/RST was not found.',1;
    IF @ScopeOffice<>@OfficeId THROW 51004,'RST does not belong to the logged-in company.',1;
    IF ISNULL(@Available,0)<=0 THROW 51005,'Recorded unloaded bags are required before return request.',1;
    IF @RequestedBags>@Available THROW 51006,'Return bags cannot exceed recorded unloaded bags.',1;
    IF @StageCode NOT IN('RST_GENERATED','SUPERVISOR_ASSIGNMENT','SUPERVISOR_FINAL') THROW 51009,'Return window is closed because this RST has entered Lab or final workflow.',1;
    IF @SourceLocationId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.t_OfficeLocationMapping WHERE OfficeId=@OfficeId AND LocationId=@SourceLocationId AND IsActive=1) THROW 51007,'Selected source location is not mapped to this company.',1;
    IF EXISTS(SELECT 1 FROM dbo.tbl_OrderReturn WHERE RSTNumber=@RST AND IsActive=1) THROW 51008,'A return request is already recorded for this RST.',1;

    BEGIN TRAN;
    DECLARE @Id INT,@Number NVARCHAR(30);
    INSERT dbo.tbl_OrderReturn(ReturnNumber,UnloadId,RSTNumber,OfficeId,SourceLocationId,RequestedBags,ReturnReason,RequestedBy)
    VALUES(CONCAT('TMP-',RIGHT(REPLACE(CONVERT(VARCHAR(36),NEWID()),'-',''),20)),@UnloadId,@RST,@OfficeId,@SourceLocationId,@RequestedBags,LTRIM(RTRIM(@Reason)),@UserId);
    SET @Id=SCOPE_IDENTITY();
    SET @Number=CONCAT('RET-',YEAR(GETDATE()),'-',RIGHT('000000'+CONVERT(VARCHAR(10),@Id),6));
    UPDATE dbo.tbl_OrderReturn SET ReturnNumber=@Number WHERE OrderReturnId=@Id;
    INSERT dbo.tbl_OrderReturnStatusHistory(OrderReturnId,NewStatus,Remarks,ChangedBy) VALUES(@Id,'PendingQuality','Return request created',@UserId);
    COMMIT;
    SELECT @Id OrderReturnId,@Number ReturnNumber;
END;
GO
