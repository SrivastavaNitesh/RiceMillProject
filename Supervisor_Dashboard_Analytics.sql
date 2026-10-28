CREATE OR ALTER PROCEDURE dbo.sp_GetSupervisorDashboard
    @SupervisorId INT,
    @OfficeId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT unload.UnloadId, unload.RSTNumber, unload.SupervisorId, unload.MethId, unload.UnloadTime, unload.Status,
           gate.TargetOfficeId, gate.VehicleId, gate.PartyId
    INTO #ScopedUnload
    FROM dbo.t_UnloadTransaction unload
    INNER JOIN dbo.t_GateEntry gate ON gate.RSTNumber = unload.RSTNumber
    WHERE unload.SupervisorId = @SupervisorId AND gate.TargetOfficeId = @OfficeId;

    SELECT
        ISNULL(supervisor.PersonName, 'Supervisor') AS SupervisorName,
        ISNULL(office.OfficeName, 'Company') AS CompanyName,
        (SELECT COUNT(1) FROM dbo.t_GateEntry gate
         WHERE gate.TargetOfficeId = @OfficeId AND gate.Status IN ('WaitingForSupervisor', 'Entered')
           AND NOT EXISTS (SELECT 1 FROM dbo.t_UnloadTransaction unload WHERE unload.RSTNumber = gate.RSTNumber)) AS PendingAssignments,
        (SELECT COUNT(1) FROM #ScopedUnload scoped
         WHERE NOT EXISTS (SELECT 1 FROM dbo.t_WorkerAllocation worker WHERE worker.UnloadId = scoped.UnloadId)) AS WorkerPending,
        (SELECT COUNT(1) FROM #ScopedUnload scoped
         WHERE NOT EXISTS (SELECT 1 FROM dbo.t_SupervisorUnloadAction action WHERE action.UnloadId = scoped.UnloadId AND action.SupervisorId = @SupervisorId)) AS FieldPending,
        (SELECT COUNT(1) FROM #ScopedUnload scoped
         WHERE EXISTS (SELECT 1 FROM dbo.t_SupervisorUnloadAction action WHERE action.UnloadId = scoped.UnloadId AND action.SupervisorId = @SupervisorId
                       AND CONVERT(DATE, COALESCE(action.UpdatedAt, action.CreatedAt)) = CONVERT(DATE, GETDATE()))) AS TodayCompleted,
        (SELECT COUNT(1) FROM #ScopedUnload) AS TotalAssigned,
        (SELECT ISNULL(SUM(action.SupervisorTotalBags), 0) FROM dbo.t_SupervisorUnloadAction action
         INNER JOIN #ScopedUnload scoped ON scoped.UnloadId = action.UnloadId WHERE action.SupervisorId = @SupervisorId) AS TotalRecordedBags
    FROM dbo.P02_Person supervisor
    CROSS JOIN dbo.o05_office office
    WHERE supervisor.PersonId = @SupervisorId AND office.OfficeId = @OfficeId;

    SELECT Name, Value FROM
    (
        SELECT 1 SortOrder, 'Worker + Field Pending' Name, CONVERT(DECIMAL(18,2), COUNT(1)) Value FROM #ScopedUnload scoped
        WHERE NOT EXISTS (SELECT 1 FROM dbo.t_WorkerAllocation worker WHERE worker.UnloadId = scoped.UnloadId)
          AND NOT EXISTS (SELECT 1 FROM dbo.t_SupervisorUnloadAction action WHERE action.UnloadId = scoped.UnloadId AND action.SupervisorId = @SupervisorId)
        UNION ALL
        SELECT 2, 'Worker Pending', CONVERT(DECIMAL(18,2), COUNT(1)) FROM #ScopedUnload scoped
        WHERE NOT EXISTS (SELECT 1 FROM dbo.t_WorkerAllocation worker WHERE worker.UnloadId = scoped.UnloadId)
          AND EXISTS (SELECT 1 FROM dbo.t_SupervisorUnloadAction action WHERE action.UnloadId = scoped.UnloadId AND action.SupervisorId = @SupervisorId)
        UNION ALL
        SELECT 3, 'Field Pending', CONVERT(DECIMAL(18,2), COUNT(1)) FROM #ScopedUnload scoped
        WHERE EXISTS (SELECT 1 FROM dbo.t_WorkerAllocation worker WHERE worker.UnloadId = scoped.UnloadId)
          AND NOT EXISTS (SELECT 1 FROM dbo.t_SupervisorUnloadAction action WHERE action.UnloadId = scoped.UnloadId AND action.SupervisorId = @SupervisorId)
        UNION ALL
        SELECT 4, 'Completed', CONVERT(DECIMAL(18,2), COUNT(1)) FROM #ScopedUnload scoped
        WHERE EXISTS (SELECT 1 FROM dbo.t_WorkerAllocation worker WHERE worker.UnloadId = scoped.UnloadId)
          AND EXISTS (SELECT 1 FROM dbo.t_SupervisorUnloadAction action WHERE action.UnloadId = scoped.UnloadId AND action.SupervisorId = @SupervisorId)
    ) status ORDER BY SortOrder;

    ;WITH Dates AS
    (
        SELECT DATEADD(DAY, -6, CONVERT(DATE, GETDATE())) WorkDate
        UNION ALL SELECT DATEADD(DAY, 1, WorkDate) FROM Dates WHERE WorkDate < CONVERT(DATE, GETDATE())
    )
    SELECT FORMAT(dates.WorkDate, 'dd-MMM') DateLabel,
           COUNT(DISTINCT action.UnloadId) Completed,
           ISNULL(SUM(action.SupervisorTotalBags), 0) Bags
    FROM Dates dates
    LEFT JOIN dbo.t_SupervisorUnloadAction action ON CONVERT(DATE, COALESCE(action.UpdatedAt, action.CreatedAt)) = dates.WorkDate AND action.SupervisorId = @SupervisorId
    LEFT JOIN #ScopedUnload scoped ON scoped.UnloadId = action.UnloadId
    GROUP BY dates.WorkDate ORDER BY dates.WorkDate OPTION (MAXRECURSION 7);

    SELECT detail.LocationId, location.LocationName, detail.CategoryId, category.CategoryName,
           detail.ItemId, item.ItemName, detail.BagTypeId, bag.BagTypeName, SUM(detail.BagCount) Bags
    FROM dbo.t_SupervisorUnloadDetail detail
    INNER JOIN dbo.t_SupervisorUnloadAction action ON action.SupervisorActionId = detail.SupervisorActionId AND action.SupervisorId = @SupervisorId
    INNER JOIN #ScopedUnload scoped ON scoped.UnloadId = action.UnloadId
    INNER JOIN dbo.m_LocationMaster location ON location.LocationId = detail.LocationId
    INNER JOIN dbo.m_ItemCategory category ON category.CategoryId = detail.CategoryId
    INNER JOIN dbo.m_Item item ON item.ItemId = detail.ItemId
    INNER JOIN dbo.m_BagType bag ON bag.BagTypeId = detail.BagTypeId
    GROUP BY detail.LocationId, location.LocationName, detail.CategoryId, category.CategoryName,
             detail.ItemId, item.ItemName, detail.BagTypeId, bag.BagTypeName
    ORDER BY location.LocationName, category.CategoryName, item.ItemName, bag.BagTypeName;

    SELECT bag.BagTypeName Name, CONVERT(DECIMAL(18,2), SUM(detail.BagCount)) Value
    FROM dbo.t_SupervisorUnloadDetail detail
    INNER JOIN dbo.t_SupervisorUnloadAction action ON action.SupervisorActionId = detail.SupervisorActionId AND action.SupervisorId = @SupervisorId
    INNER JOIN #ScopedUnload scoped ON scoped.UnloadId = action.UnloadId
    INNER JOIN dbo.m_BagType bag ON bag.BagTypeId = detail.BagTypeId
    GROUP BY bag.BagTypeName ORDER BY Value DESC;

    SELECT allocation.WorkerId, worker.PersonName WorkerName, allocation.WorkType,
           allocation.BagTypeId, ISNULL(bag.BagTypeName, 'Not recorded') BagTypeName, SUM(allocation.BagCount) Bags
    FROM dbo.t_WorkerAllocation allocation
    INNER JOIN #ScopedUnload scoped ON scoped.UnloadId = allocation.UnloadId
    INNER JOIN dbo.P02_Person worker ON worker.PersonId = allocation.WorkerId
    LEFT JOIN dbo.m_BagType bag ON bag.BagTypeId = allocation.BagTypeId
    GROUP BY allocation.WorkerId, worker.PersonName, allocation.WorkType, allocation.BagTypeId, bag.BagTypeName
    ORDER BY worker.PersonName, allocation.WorkType, bag.BagTypeName;

    SELECT TOP (12) scoped.UnloadId, scoped.RSTNumber, ISNULL(vehicle.VehicleNumber, '-') VehicleNumber,
           ISNULL(party.PersonName, '-') PartyName, ISNULL(meth.PersonName, 'Not assigned') MethName,
           ISNULL(locations.LocationName, 'Pending') LocationName,
           CASE
             WHEN action.SupervisorActionId IS NOT NULL AND worker.HasWorker = 1 THEN 'Completed'
             WHEN action.SupervisorActionId IS NOT NULL THEN 'Worker Details Pending'
             WHEN worker.HasWorker = 1 THEN 'Field Details Pending'
             ELSE 'Worker + Field Pending'
           END CurrentStage,
           ISNULL(action.SupervisorTotalBags, 0) Bags,
           COALESCE(action.UpdatedAt, action.CreatedAt, scoped.UnloadTime) LastActivity
    FROM #ScopedUnload scoped
    LEFT JOIN dbo.m_Vehicle vehicle ON vehicle.VehicleId = scoped.VehicleId
    LEFT JOIN dbo.P02_Person party ON party.PersonId = scoped.PartyId
    LEFT JOIN dbo.P02_Person meth ON meth.PersonId = scoped.MethId
    OUTER APPLY (SELECT TOP (1) item.SupervisorActionId,item.SupervisorTotalBags,item.CreatedAt,item.UpdatedAt FROM dbo.t_SupervisorUnloadAction item WHERE item.UnloadId=scoped.UnloadId AND item.SupervisorId=@SupervisorId ORDER BY item.SupervisorActionId DESC) action
    OUTER APPLY (SELECT CASE WHEN EXISTS(SELECT 1 FROM dbo.t_WorkerAllocation item WHERE item.UnloadId=scoped.UnloadId) THEN 1 ELSE 0 END HasWorker) worker
    OUTER APPLY (SELECT STRING_AGG(CONVERT(NVARCHAR(MAX), location.LocationName), ', ') LocationName FROM dbo.t_UnloadLocation mapped INNER JOIN dbo.m_LocationMaster location ON location.LocationId=mapped.LocationId WHERE mapped.UnloadId=scoped.UnloadId AND mapped.IsActive=1) locations
    ORDER BY CASE WHEN action.SupervisorActionId IS NOT NULL AND worker.HasWorker=1 THEN 1 ELSE 0 END, COALESCE(action.UpdatedAt, action.CreatedAt, scoped.UnloadTime) DESC;
END;
GO
