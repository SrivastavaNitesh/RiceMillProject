-- Apply after existing Lab and RST migrations. Stage IDs are resolved by code.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO
IF NOT EXISTS (SELECT 1 FROM dbo.m_RSTStageMaster WHERE StageId=7 AND StageCode=N'TARE_WEIGHT' AND IsActive=1)
    THROW 50001,'Active stage 7 (TARE_WEIGHT) is required.',1;
BEGIN TRANSACTION;
IF NOT EXISTS (SELECT 1 FROM dbo.m_RSTStageMaster WITH(UPDLOCK,HOLDLOCK) WHERE StageCode=N'LAB_REPORT_PARTIAL')
    INSERT dbo.m_RSTStageMaster(StageCode,StageName,SequenceNo,IsActive)
    VALUES(N'LAB_REPORT_PARTIAL',N'Lab Report Partially Completed',8,1);
IF NOT EXISTS (SELECT 1 FROM dbo.m_RSTStageMaster WITH(UPDLOCK,HOLDLOCK) WHERE StageCode=N'LAB_REPORT_COMPLETED')
    INSERT dbo.m_RSTStageMaster(StageCode,StageName,SequenceNo,IsActive)
    VALUES(N'LAB_REPORT_COMPLETED',N'Lab Report Fully Completed',9,1);
COMMIT;
GO
-- All lab operations use the same supervisor-confirmed item set.
CREATE OR ALTER FUNCTION dbo.fn_LabRstItems(@RSTNumber nvarchar(50))
RETURNS TABLE
AS RETURN
(
    SELECT DISTINCT u.RSTNumber,sd.CategoryId,sd.ItemId
    FROM dbo.t_UnloadTransaction u
    JOIN dbo.t_SupervisorUnloadAction sa ON sa.UnloadId=u.UnloadId AND sa.Status='Completed'
    JOIN dbo.t_SupervisorUnloadDetail sd ON sd.SupervisorActionId=sa.SupervisorActionId
    WHERE @RSTNumber IS NULL OR u.RSTNumber=@RSTNumber
);
GO
CREATE OR ALTER FUNCTION dbo.fn_LabRstCoverage(@RSTNumber nvarchar(50))
RETURNS TABLE
AS RETURN
(
    WITH items AS (SELECT * FROM dbo.fn_LabRstItems(@RSTNumber)),
    required AS
    (
        SELECT DISTINCT i.RSTNumber,i.CategoryId,i.ItemId,m.TestId
        FROM items i
        JOIN dbo.m_ItemLabTest m ON m.CategoryId=i.CategoryId AND m.ItemId=i.ItemId AND m.IsActive=1
        JOIN dbo.m_LabTestMaster t ON t.TestId=m.TestId AND t.IsActive=1
    )
    SELECT x.RSTNumber,
        (SELECT COUNT(*) FROM items i WHERE i.RSTNumber=x.RSTNumber) ItemCount,
        (SELECT COUNT(*) FROM required r WHERE r.RSTNumber=x.RSTNumber) RequiredTests,
        (SELECT COUNT(*) FROM required r WHERE r.RSTNumber=x.RSTNumber AND EXISTS
            (SELECT 1 FROM dbo.t_LabReport h JOIN dbo.t_LabReportResult d ON d.ReportId=h.ReportId
             WHERE h.RSTNumber=r.RSTNumber AND d.CategoryId=r.CategoryId AND d.ItemId=r.ItemId AND d.TestId=r.TestId)) CompletedTests,
        (SELECT COUNT(*) FROM items i WHERE i.RSTNumber=x.RSTNumber AND NOT EXISTS
            (SELECT 1 FROM required r WHERE r.RSTNumber=i.RSTNumber AND r.CategoryId=i.CategoryId AND r.ItemId=i.ItemId)) UnmappedItems
    FROM (SELECT DISTINCT RSTNumber FROM items) x
);
GO
CREATE OR ALTER PROCEDURE dbo.sp_LabRstList @ForEntry bit=0
AS
BEGIN
    SET NOCOUNT ON;
    -- Keep completed/downstream RSTs visible to billing and saved-report links.
    SELECT g.RSTNumber,ISNULL(v.VehicleNumber,'') VehicleNumber,ISNULL(p.PersonName,'') PartyName,
           c.ItemCount,c.RequiredTests,c.CompletedTests,c.UnmappedItems
    FROM dbo.t_GateEntry g
    JOIN dbo.fn_LabRstCoverage(NULL) c ON c.RSTNumber=g.RSTNumber
    LEFT JOIN dbo.m_Vehicle v ON v.VehicleId=g.VehicleId
    LEFT JOIN dbo.P02_Person p ON p.PersonId=g.PartyId
    WHERE ISNULL(g.IsFinalRST,1)=1
      AND EXISTS(SELECT 1 FROM dbo.t_RSTProgressWork h WHERE h.RSTNumber=g.RSTNumber AND h.StageId IN(7,9,13))
    ORDER BY g.GateEntryTime DESC;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_LabItemOptions @RSTNumber nvarchar(50)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Allowed bit=0;
    IF EXISTS
    (
        SELECT 1 FROM dbo.t_GateEntry g
        WHERE g.RSTNumber=@RSTNumber AND ISNULL(g.IsFinalRST,1)=1
          AND EXISTS(SELECT 1 FROM dbo.t_RSTProgressWork h WHERE h.RSTNumber=g.RSTNumber AND h.StageId IN(7,9,13))
          AND NOT EXISTS(SELECT 1 FROM dbo.t_RSTProgressWork h JOIN dbo.m_RSTStageMaster s ON s.StageId=h.StageId
                         WHERE h.RSTNumber=g.RSTNumber AND s.StageCode=N'LAB_REPORT_COMPLETED')
    ) SET @Allowed=1;
    DECLARE @Items TABLE(CategoryId int,ItemId int,PRIMARY KEY(CategoryId,ItemId));
    INSERT @Items
    SELECT i.CategoryId,i.ItemId FROM dbo.m_Item i JOIN dbo.m_ItemCategory c ON c.CategoryId=i.CategoryId
    WHERE (@RSTNumber IS NULL AND i.IsActive=1 AND c.IsActive=1)
       OR (@Allowed=1 AND EXISTS(SELECT 1 FROM dbo.fn_LabRstItems(@RSTNumber) x WHERE x.CategoryId=i.CategoryId AND x.ItemId=i.ItemId));
    SELECT i.ItemId,i.ItemName,c.CategoryId,c.CategoryName
    FROM @Items x JOIN dbo.m_Item i ON i.ItemId=x.ItemId JOIN dbo.m_ItemCategory c ON c.CategoryId=x.CategoryId
    ORDER BY c.CategoryName,i.ItemName;
    SELECT DISTINCT m.CategoryId,m.ItemId,t.TestId,t.TestName,t.Unit,t.ResultType,t.Description,t.IsActive
    FROM @Items x JOIN dbo.m_ItemLabTest m ON m.CategoryId=x.CategoryId AND m.ItemId=x.ItemId AND m.IsActive=1
    JOIN dbo.m_LabTestMaster t ON t.TestId=m.TestId AND t.IsActive=1
    ORDER BY m.CategoryId,m.ItemId,t.TestName;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_LabReportSave
    @RSTNumber nvarchar(50),@SubmissionId uniqueidentifier,@SelectedItems nvarchar(max),
    @Results nvarchar(max),@Remarks nvarchar(1000)=NULL,@UserId int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF NULLIF(LTRIM(RTRIM(@RSTNumber)),N'') IS NULL OR @SubmissionId IS NULL
        THROW 50001,'RST number and submission ID are required.',1;
    IF ISNULL(ISJSON(@SelectedItems),0)<>1 OR ISNULL(ISJSON(@Results),0)<>1
        THROW 50001,'Invalid test results.',1;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @LockResult int,@LockName nvarchar(255)=N'RSTProgress:'+UPPER(LTRIM(RTRIM(@RSTNumber)));
        EXEC @LockResult=sys.sp_getapplock @Resource=@LockName,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
        IF @LockResult<0 THROW 50001,'This RST is being updated. Please retry.',1;
        DECLARE @Technician nvarchar(200),@ReportId int,@CurrentStageId int,@PartialStageId int,@CompleteStageId int;
        SELECT @Technician=p.PersonName FROM dbo.sa05_user u JOIN dbo.P02_Person p ON p.PersonId=u.PersonId
        WHERE u.UserId=@UserId AND u.IsActive=1 AND p.IsActive=1
          AND (u.sa10_usertypeid IN(4,5) OR p.PersonType=N'Admin' OR LOWER(REPLACE(p.PersonType,N' ',N''))=N'labtechnician');
        IF @Technician IS NULL THROW 50001,'An active lab technician or administrator login is required.',1;
        SELECT @ReportId=ReportId FROM dbo.t_LabReport WITH(UPDLOCK,HOLDLOCK)
        WHERE SubmissionId=@SubmissionId AND RSTNumber=@RSTNumber AND TestedByUserId=@UserId;
        IF @ReportId IS NOT NULL BEGIN COMMIT; SELECT @ReportId AS ReportId; RETURN; END;
        SELECT @PartialStageId=StageId FROM dbo.m_RSTStageMaster WHERE StageCode=N'LAB_REPORT_PARTIAL' AND IsActive=1;
        SELECT @CompleteStageId=StageId FROM dbo.m_RSTStageMaster WHERE StageCode=N'LAB_REPORT_COMPLETED' AND IsActive=1;
        IF @PartialStageId IS NULL OR @CompleteStageId IS NULL THROW 50001,'Lab report stages are not configured.',1;
        SELECT TOP(1) @CurrentStageId=StageId FROM dbo.t_RSTProgressWork WITH(UPDLOCK,HOLDLOCK)
        WHERE RSTNumber=@RSTNumber ORDER BY ProgressId DESC;
        IF EXISTS(SELECT 1 FROM dbo.t_RSTProgressWork WHERE RSTNumber=@RSTNumber AND StageId=@CompleteStageId)
            THROW 50002,'All item tests for this RST are already submitted. Entry is closed; view the saved reports.',1;
        IF NOT EXISTS(SELECT 1 FROM dbo.t_RSTProgressWork WHERE RSTNumber=@RSTNumber AND StageId IN(7,9,13))
            THROW 50001,'Lab entry requires this RST to have reached stage 7, 9 or 13.',1;
        IF NOT EXISTS(SELECT 1 FROM dbo.t_GateEntry WITH(UPDLOCK,HOLDLOCK) WHERE RSTNumber=@RSTNumber AND ISNULL(IsFinalRST,1)=1)
            THROW 50001,'Only the final RST can be sent for lab testing.',1;
        IF EXISTS(SELECT 1 FROM dbo.fn_LabRstCoverage(@RSTNumber) WHERE ItemCount>0 AND UnmappedItems=0 AND RequiredTests>0 AND CompletedTests>=RequiredTests)
            THROW 50002,'All item tests for this RST are already submitted. Entry is closed; view the saved reports.',1;
        DECLARE @Selected TABLE(ItemId int PRIMARY KEY);
        INSERT @Selected SELECT DISTINCT TRY_CONVERT(int,value) FROM OPENJSON(@SelectedItems) WHERE TRY_CONVERT(int,value)>0;
        IF NOT EXISTS(SELECT 1 FROM @Selected) THROW 50001,'Select at least one supervisor-confirmed item.',1;
        IF EXISTS(SELECT 1 FROM @Selected s WHERE NOT EXISTS(SELECT 1 FROM dbo.fn_LabRstItems(@RSTNumber) i WHERE i.ItemId=s.ItemId))
            THROW 50001,'Only supervisor-confirmed items for this RST can be tested.',1;
        DECLARE @Expected TABLE(CategoryId int,ItemId int,TestId int,CategoryName nvarchar(100),ItemName nvarchar(100),TestName nvarchar(120),Unit nvarchar(30),ResultType varchar(10),PRIMARY KEY(ItemId,TestId));
        INSERT @Expected
        SELECT DISTINCT m.CategoryId,m.ItemId,m.TestId,c.CategoryName,i.ItemName,t.TestName,t.Unit,t.ResultType
        FROM @Selected s JOIN dbo.fn_LabRstItems(@RSTNumber) x ON x.ItemId=s.ItemId
        JOIN dbo.m_ItemLabTest m WITH(HOLDLOCK) ON m.ItemId=x.ItemId AND m.CategoryId=x.CategoryId AND m.IsActive=1
        JOIN dbo.m_LabTestMaster t WITH(HOLDLOCK) ON t.TestId=m.TestId AND t.IsActive=1
        JOIN dbo.m_Item i ON i.ItemId=m.ItemId AND i.CategoryId=m.CategoryId
        JOIN dbo.m_ItemCategory c ON c.CategoryId=m.CategoryId;
        IF EXISTS(SELECT 1 FROM @Selected s WHERE NOT EXISTS(SELECT 1 FROM @Expected e WHERE e.ItemId=s.ItemId))
            THROW 50001,'One or more selected items have no active tests. Configure their test mappings first.',1;
        DECLARE @Input TABLE(CategoryId int,ItemId int,TestId int,Value nvarchar(max));
        INSERT @Input SELECT CategoryId,ItemId,TestId,Value FROM OPENJSON(@Results) WITH(CategoryId int,ItemId int,TestId int,Value nvarchar(max));
        IF EXISTS(SELECT ItemId,TestId FROM @Input GROUP BY ItemId,TestId HAVING COUNT(*)>1)
            THROW 50001,'Duplicate test results are not allowed.',1;
        IF EXISTS(SELECT 1 FROM @Input r WHERE NOT EXISTS(SELECT 1 FROM @Expected e WHERE e.CategoryId=r.CategoryId AND e.ItemId=r.ItemId AND e.TestId=r.TestId))
           OR EXISTS(SELECT 1 FROM @Expected e WHERE NOT EXISTS(SELECT 1 FROM @Input r WHERE r.CategoryId=e.CategoryId AND r.ItemId=e.ItemId AND r.TestId=e.TestId))
            THROW 50001,'Test mappings changed or results are incomplete. Reload the RST and fill every selected item test.',1;
        IF EXISTS(SELECT 1 FROM @Input r JOIN @Expected e ON e.ItemId=r.ItemId AND e.TestId=r.TestId
                  WHERE NULLIF(LTRIM(RTRIM(r.Value)),'') IS NULL OR LEN(r.Value)>500 OR (e.ResultType='Number' AND TRY_CONVERT(decimal(18,6),r.Value) IS NULL))
            THROW 50001,'Enter a valid value for every test (numeric results support up to 6 decimal places).',1;
        INSERT dbo.t_LabReport(SubmissionId,RSTNumber,TestedByUserId,TechnicianName,Remarks)
        VALUES(@SubmissionId,@RSTNumber,@UserId,@Technician,@Remarks);
        SET @ReportId=SCOPE_IDENTITY();
        INSERT dbo.t_LabReportResult(ReportId,CategoryId,ItemId,TestId,CategoryName,ItemName,TestName,Unit,ResultType,ResultValue)
        SELECT @ReportId,e.CategoryId,e.ItemId,e.TestId,e.CategoryName,e.ItemName,e.TestName,e.Unit,e.ResultType,LTRIM(RTRIM(r.Value))
        FROM @Expected e JOIN @Input r ON e.ItemId=r.ItemId AND e.TestId=r.TestId;
        DECLARE @StageId int=@PartialStageId,@Status nvarchar(50)=N'Partially Completed',@Now datetime2=SYSDATETIME();
        IF EXISTS(SELECT 1 FROM dbo.fn_LabRstCoverage(@RSTNumber) WHERE ItemCount>0 AND UnmappedItems=0 AND RequiredTests>0 AND CompletedTests>=RequiredTests)
            SELECT @StageId=@CompleteStageId,@Status=N'Completed';
        UPDATE dbo.t_RSTProgressWork SET IsActive=0,CompletedAt=COALESCE(CompletedAt,@Now)
        WHERE RSTNumber=@RSTNumber AND IsActive=1;
        -- One history event per accepted report, including successive partial saves.
        INSERT dbo.t_RSTProgressWork(RSTNumber,StageId,Status,IsActive,StartedAt,CompletedAt,CreatedBy,Remarks)
        VALUES(@RSTNumber,@StageId,@Status,1,@Now,CASE WHEN @StageId=@CompleteStageId THEN @Now END,@UserId,CONCAT(N'Lab report LAB-',@ReportId));
        UPDATE dbo.t_RSTMaster SET CurrentStageId=@StageId,CurrentStatus=@Status,UpdatedAt=@Now WHERE RSTNumber=@RSTNumber;
        IF @@ROWCOUNT=0 INSERT dbo.t_RSTMaster(RSTNumber,CurrentStageId,CurrentStatus,UpdatedAt) VALUES(@RSTNumber,@StageId,@Status,@Now);
        COMMIT;
        SELECT @ReportId AS ReportId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK;
        THROW;
    END CATCH;
END;
GO
