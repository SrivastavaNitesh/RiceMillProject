-- Multiple unloading locations; status IDs: 1=Gadi In, 2=Weight Done, 3=Unload Assigned.

CREATE OR ALTER PROCEDURE [dbo].[sp_AssignUnloading]
    @RSTNumber NVARCHAR(100),
    @SupervisorId INT,
    @MethId INT,
    @ItemId INT = NULL,
    @LocationId INT = NULL,
    @LocationIds NVARCHAR(MAX) = NULL,
    @OfficeId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.o05_office WHERE OfficeId=@OfficeId AND IsActive=1)
        THROW 50001, 'Valid login company is required.', 1;

    DECLARE @Locations TABLE (LocationId INT PRIMARY KEY);
    IF @LocationIds IS NOT NULL
    BEGIN
        IF NULLIF(LTRIM(RTRIM(@LocationIds)), '') IS NULL
           OR EXISTS (SELECT 1 FROM STRING_SPLIT(@LocationIds, ',')
                      WHERE TRY_CONVERT(INT, value) IS NULL OR TRY_CONVERT(INT, value) <= 0)
            THROW 50001, 'Select valid unloading locations.', 1;
        INSERT @Locations SELECT DISTINCT CONVERT(INT, value) FROM STRING_SPLIT(@LocationIds, ',');
    END
    ELSE IF @LocationId > 0
        INSERT @Locations VALUES (@LocationId);
    IF NOT EXISTS (SELECT 1 FROM @Locations)
        THROW 50001, 'Select at least one unloading location.', 1;
    -- Preserve a primary location for existing single-location consumers.
    SELECT @LocationId = MIN(LocationId) FROM @Locations;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @RstItemId INT = NULL;

        /* ---------------------------------------------------------
           FIX 1:
           RST availability must NOT depend on ItemId.
           Material/Variety is now filled later in Supervisor Final.
           --------------------------------------------------------- */
        IF NOT EXISTS
        (
            SELECT 1
            FROM dbo.t_GateEntry WITH (UPDLOCK, HOLDLOCK)
            WHERE RSTNumber = @RSTNumber
              AND StatusId IN (1, 2)
        )
        BEGIN
            THROW 50001, 'RST is not available for Supervisor action.', 1;
        END;

        SELECT
            @RstItemId = ItemId
        FROM dbo.t_GateEntry WITH (UPDLOCK, HOLDLOCK)
        WHERE RSTNumber = @RSTNumber;

        /* ---------------------------------------------------------
           Prevent duplicate assignment
           --------------------------------------------------------- */
        IF EXISTS
        (
            SELECT 1
            FROM dbo.t_UnloadTransaction WITH (UPDLOCK, HOLDLOCK)
            WHERE RSTNumber = @RSTNumber
        )
        BEGIN
            THROW 50001, 'This RST is already assigned for unloading.', 1;
        END;

        /* ---------------------------------------------------------
           Validate Supervisor
           --------------------------------------------------------- */
        IF NOT EXISTS
        (
            SELECT 1
            FROM dbo.P02_Person
            WHERE PersonId = @SupervisorId
              AND IsActive = 1
        )
        BEGIN
            THROW 50001, 'Active Supervisor is required.', 1;
        END;

        /* ---------------------------------------------------------
           Validate Meth
           --------------------------------------------------------- */
        IF NOT EXISTS
        (
            SELECT 1
            FROM dbo.P02_Person
            WHERE PersonId = @MethId
              AND IsActive = 1
        )
        BEGIN
            THROW 50001, 'Active Meth is required.', 1;
        END;

        /* ---------------------------------------------------------
           FIX 2:
           Current Location master is m_LocationMaster and mapping is
           t_OfficeLocationMapping. Do not use old P03_Location.
           Use the login company passed by the server, not the RST company.
           --------------------------------------------------------- */
        IF EXISTS (
            SELECT 1 FROM @Locations selected
            WHERE NOT EXISTS (
                SELECT 1 FROM dbo.m_LocationMaster L
                INNER JOIN dbo.t_OfficeLocationMapping M ON M.LocationId=L.LocationId
                WHERE L.LocationId=selected.LocationId AND L.IsActive=1
                  AND M.IsActive=1 AND M.UnloadingAllowed=1
                  AND M.OfficeId=@OfficeId
            )
        )
            THROW 50001, 'Valid unloading locations are required for this company.', 1;

        /* ---------------------------------------------------------
           ItemId is intentionally nullable here.
           It will be finalized later in Supervisor Final Details.
           --------------------------------------------------------- */
        INSERT INTO dbo.t_UnloadTransaction
        (
            RSTNumber,
            SupervisorId,
            MethId,
            ItemId,
            LocationId,
            Status
        )
        VALUES
        (
            @RSTNumber,
            @SupervisorId,
            @MethId,
            @RstItemId,
            @LocationId,
            'Assigned'
        );

        DECLARE @UnloadId INT = CONVERT(INT, SCOPE_IDENTITY());

        DELETE FROM dbo.t_GateEntryLocations WHERE RSTNumber=@RSTNumber;
        INSERT dbo.t_GateEntryLocations (RSTNumber, LocationId)
        SELECT @RSTNumber, LocationId FROM @Locations;

        UPDATE dbo.t_GateEntry
        SET Status = 'UnloadingAssigned', StatusId = 3
        WHERE RSTNumber = @RSTNumber;

        COMMIT TRANSACTION;

        SELECT @UnloadId AS UnloadId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH;
END;

GO

CREATE OR ALTER PROCEDURE dbo.sp_GetSupervisorMethWorkRegister
    @SupervisorId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.UnloadId,u.RSTNumber,u.SupervisorId,u.MethId,u.LocationId,u.ItemId,u.NumberOfBags,u.Status,
           ISNULL(m.PersonName,'') AS MethName, ISNULL(loc.LocationName,'') AS LocationName,
           ISNULL(v.VehicleNumber,'') AS VehicleNumber, ISNULL(p.PersonName,'') AS PartyName,
           ISNULL(i.ItemName,'') AS ItemName,
           CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.t_WorkerAllocation wa WHERE wa.UnloadId=u.UnloadId AND ISNULL(wa.BagCount,0)>0) THEN 1 ELSE 0 END AS bit) AS IsWorkerWorkCompleted,
           CAST(CASE WHEN sa.SupervisorActionId IS NOT NULL THEN 1 ELSE 0 END AS bit) AS IsSupervisorActionCompleted
    FROM dbo.t_UnloadTransaction u
    LEFT JOIN dbo.t_GateEntry ge ON ge.RSTNumber=u.RSTNumber
    LEFT JOIN dbo.P02_Person m ON m.PersonId=u.MethId
    OUTER APPLY (
        SELECT STRING_AGG(CONVERT(nvarchar(max), names.LocationName), ', ')
               WITHIN GROUP (ORDER BY names.LocationName) AS LocationName
        FROM (
            SELECT DISTINCT l.LocationId, l.LocationName
            FROM dbo.t_GateEntryLocations gl
            INNER JOIN dbo.m_LocationMaster l ON l.LocationId=gl.LocationId
            WHERE gl.RSTNumber=u.RSTNumber
        ) names
    ) loc
    LEFT JOIN dbo.m_Vehicle v ON v.VehicleId=ge.VehicleId
    LEFT JOIN dbo.P02_Person p ON p.PersonId=ge.PartyId
    LEFT JOIN dbo.m_Item i ON i.ItemId=u.ItemId
    LEFT JOIN dbo.t_SupervisorUnloadAction sa ON sa.UnloadId=u.UnloadId
    WHERE u.SupervisorId=@SupervisorId AND ISNULL(u.MethId,0)>0
    ORDER BY u.UnloadId DESC;
END;

GO
-- Preserve existing single-location assignments in the report's new source.
INSERT dbo.t_GateEntryLocations (RSTNumber, LocationId)
SELECT DISTINCT u.RSTNumber, u.LocationId
FROM dbo.t_UnloadTransaction u
INNER JOIN dbo.t_GateEntry ge ON ge.RSTNumber=u.RSTNumber
INNER JOIN dbo.m_LocationMaster l ON l.LocationId=u.LocationId
WHERE NOT EXISTS (SELECT 1 FROM dbo.t_GateEntryLocations gl WHERE gl.RSTNumber=u.RSTNumber);
