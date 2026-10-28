SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @OfficeId INT=(SELECT TOP(1) OfficeId FROM dbo.o05_office ORDER BY OfficeId);
DECLARE @UnloadId INT,@AvailableBags INT;
SELECT TOP(1) @UnloadId=u.UnloadId,@AvailableBags=x.AvailableBags
FROM dbo.t_UnloadTransaction u
JOIN dbo.t_GateEntry g ON g.RSTNumber=u.RSTNumber
OUTER APPLY(SELECT TOP(1) SupervisorTotalBags FROM dbo.t_SupervisorUnloadAction s WHERE s.UnloadId=u.UnloadId ORDER BY s.SupervisorActionId DESC)a
OUTER APPLY(SELECT SUM(BagCount) WorkerBags FROM dbo.t_WorkerAllocation wa WHERE wa.UnloadId=u.UnloadId)w
CROSS APPLY(SELECT COALESCE(a.SupervisorTotalBags,u.NumberOfBags,w.WorkerBags) AvailableBags)x
WHERE g.TargetOfficeId=@OfficeId AND ISNULL(x.AvailableBags,0)>=2
  AND NOT EXISTS(SELECT 1 FROM dbo.tbl_OrderReturn r WHERE r.UnloadId=u.UnloadId AND r.IsActive=1 AND r.Status NOT IN('Completed','Rejected','Cancelled'))
ORDER BY u.UnloadId DESC;
DECLARE @UserId INT=(SELECT TOP(1) UserId FROM dbo.sa05_user WHERE OfficeId=@OfficeId AND IsActive=1 ORDER BY UserId);
DECLARE @WorkerId INT=(SELECT TOP(1) p.PersonId FROM dbo.P02_Person p JOIN dbo.P11_PersonDesignatation pd ON pd.P02_PersonId=p.PersonId AND pd.IsActive=1 JOIN dbo.O12_Designatation d ON d.DesignationId=pd.O12_DesignationId AND d.O10_PostId=7 AND d.O05_Office_Id=@OfficeId WHERE p.IsActive=1);
DECLARE @LocationId INT=(SELECT TOP(1) LocationId FROM dbo.t_OfficeLocationMapping WHERE OfficeId=@OfficeId AND IsActive=1);
DECLARE @BagTypeId INT=(SELECT TOP(1) BagTypeId FROM dbo.m_BagType WHERE IsActive=1);
DECLARE @VehicleId INT=(SELECT TOP(1) VehicleId FROM dbo.m_Vehicle WHERE IsActive=1);
IF @UnloadId IS NULL OR @UserId IS NULL OR @WorkerId IS NULL OR @LocationId IS NULL OR @BagTypeId IS NULL OR @VehicleId IS NULL THROW 51999,'Verification masters or an eligible completed unload are unavailable.',1;

DECLARE @RequestedBags INT=CASE WHEN @AvailableBags>100 THEN 100 ELSE @AvailableBags END;
DECLARE @FirstDispatchBags INT=@RequestedBags/2;
DECLARE @FinalDispatchBags INT=@RequestedBags-@FirstDispatchBags;

BEGIN TRAN;
DECLARE @Return TABLE(Id INT,Number NVARCHAR(30));
INSERT @Return EXEC dbo.sp_CreateOrderReturn @UnloadId,@RequestedBags,@LocationId,N'Rollback-only integration verification',@UserId,@OfficeId;
DECLARE @ReturnId INT=(SELECT Id FROM @Return);
EXEC dbo.sp_DecideOrderReturn @ReturnId,'Approved',@RequestedBags,N'Approved for verification',@UserId,@OfficeId;

DECLARE @D1 TABLE(Id INT,Number NVARCHAR(35));
INSERT @D1 EXEC dbo.sp_CreateOrderReturnDispatch @ReturnId,@FirstDispatchBags,@VehicleId,N'First partial dispatch',@UserId,@OfficeId;
DECLARE @Dispatch1 INT=(SELECT Id FROM @D1);
DECLARE @WorkerJson60 NVARCHAR(MAX)=CONCAT('[{"WorkerId":',@WorkerId,',"BagsHandled":',@FirstDispatchBags,',"PerBagRate":2.50}]');
EXEC dbo.sp_SaveOrderReturnMovement @ReturnId,@Dispatch1,'ReturnTruckLoading',@LocationId,NULL,@BagTypeId,@FirstDispatchBags,@WorkerJson60,N'First loading',@UserId,@OfficeId;
EXEC dbo.sp_SaveOrderReturnWeight @Dispatch1,5000,1000,@VehicleId,@UserId,@OfficeId;
EXEC dbo.sp_CompleteOrderReturnOutward @Dispatch1,@UserId,@OfficeId;

DECLARE @D2 TABLE(Id INT,Number NVARCHAR(35));
INSERT @D2 EXEC dbo.sp_CreateOrderReturnDispatch @ReturnId,@FinalDispatchBags,@VehicleId,N'Final partial dispatch',@UserId,@OfficeId;
DECLARE @Dispatch2 INT=(SELECT Id FROM @D2);
DECLARE @WorkerJson NVARCHAR(MAX)=CONCAT('[{"WorkerId":',@WorkerId,',"BagsHandled":',@FinalDispatchBags,',"PerBagRate":2.50}]');
EXEC dbo.sp_SaveOrderReturnMovement @ReturnId,@Dispatch2,'ReturnTruckLoading',@LocationId,NULL,@BagTypeId,@FinalDispatchBags,@WorkerJson,N'Final loading',@UserId,@OfficeId;
EXEC dbo.sp_SaveOrderReturnWeight @Dispatch2,4000,1000,@VehicleId,@UserId,@OfficeId;
EXEC dbo.sp_CompleteOrderReturnOutward @Dispatch2,@UserId,@OfficeId;

IF NOT EXISTS(SELECT 1 FROM dbo.tbl_OrderReturn WHERE OrderReturnId=@ReturnId AND Status='Completed' AND IsActive=0) THROW 51998,'Final return state was not completed.',1;
SELECT 'PASS' Result,@ReturnId ReturnId,(SELECT Status FROM dbo.tbl_OrderReturn WHERE OrderReturnId=@ReturnId) FinalStatus,
       (SELECT SUM(DispatchBags) FROM dbo.tbl_OrderReturnDispatch WHERE OrderReturnId=@ReturnId) DispatchBags,
       (SELECT SUM(BagsHandled) FROM dbo.tbl_OrderReturnWorker w JOIN dbo.tbl_OrderReturnMovement m ON m.MovementId=w.MovementId WHERE m.OrderReturnId=@ReturnId) PaidHandlingBags;
ROLLBACK;
