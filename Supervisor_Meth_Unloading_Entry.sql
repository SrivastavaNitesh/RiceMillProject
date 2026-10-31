SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.t_SupervisorMethEntry', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.t_SupervisorMethEntry
    (
        EntryId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_t_SupervisorMethEntry PRIMARY KEY,
        UnloadId INT NOT NULL,
        MethId INT NOT NULL,
        WorkType NVARCHAR(20) NOT NULL CONSTRAINT DF_t_SupervisorMethEntry_WorkType DEFAULT N'Unloading',
        BagTypeId INT NOT NULL,
        BagCount INT NOT NULL,
        PerBagPrice DECIMAL(18,2) NOT NULL,
        TotalAmount AS CONVERT(DECIMAL(18,2), BagCount * PerBagPrice) PERSISTED,
        EnteredBySupervisorId INT NOT NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_t_SupervisorMethEntry_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt DATETIME2(0) NULL,
        CONSTRAINT CK_t_SupervisorMethEntry_WorkType CHECK (WorkType = N'Unloading'),
        CONSTRAINT CK_t_SupervisorMethEntry_BagCount CHECK (BagCount > 0),
        CONSTRAINT CK_t_SupervisorMethEntry_PerBagPrice CHECK (PerBagPrice >= 0),
        CONSTRAINT FK_t_SupervisorMethEntry_Unload FOREIGN KEY (UnloadId) REFERENCES dbo.t_UnloadTransaction(UnloadId),
        CONSTRAINT FK_t_SupervisorMethEntry_Meth FOREIGN KEY (MethId) REFERENCES dbo.P02_Person(PersonId),
        CONSTRAINT FK_t_SupervisorMethEntry_BagType FOREIGN KEY (BagTypeId) REFERENCES dbo.m_BagType(BagTypeId),
        CONSTRAINT FK_t_SupervisorMethEntry_Supervisor FOREIGN KEY (EnteredBySupervisorId) REFERENCES dbo.P02_Person(PersonId)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.t_SupervisorMethEntry') AND name = N'UX_t_SupervisorMethEntry_Unload_BagType')
    CREATE UNIQUE INDEX UX_t_SupervisorMethEntry_Unload_BagType
        ON dbo.t_SupervisorMethEntry(UnloadId, BagTypeId);
GO

IF OBJECT_ID(N'dbo.t_SupervisorMethLooseEntry', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.t_SupervisorMethLooseEntry
    (
        LooseEntryId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_t_SupervisorMethLooseEntry PRIMARY KEY,
        UnloadId INT NOT NULL,
        MethId INT NOT NULL,
        LooseBagCount INT NOT NULL,
        EnteredBySupervisorId INT NOT NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_t_SupervisorMethLooseEntry_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt DATETIME2(0) NULL,
        CONSTRAINT UQ_t_SupervisorMethLooseEntry_Unload UNIQUE (UnloadId),
        CONSTRAINT CK_t_SupervisorMethLooseEntry_Count CHECK (LooseBagCount >= 0),
        CONSTRAINT FK_t_SupervisorMethLooseEntry_Unload FOREIGN KEY (UnloadId) REFERENCES dbo.t_UnloadTransaction(UnloadId),
        CONSTRAINT FK_t_SupervisorMethLooseEntry_Meth FOREIGN KEY (MethId) REFERENCES dbo.P02_Person(PersonId),
        CONSTRAINT FK_t_SupervisorMethLooseEntry_Supervisor FOREIGN KEY (EnteredBySupervisorId) REFERENCES dbo.P02_Person(PersonId)
    );
END;
GO

IF COL_LENGTH(N'dbo.t_SupervisorMethLooseEntry', N'ReturnBagCount') IS NULL
    ALTER TABLE dbo.t_SupervisorMethLooseEntry
        ADD ReturnBagCount INT NOT NULL
            CONSTRAINT DF_t_SupervisorMethLooseEntry_ReturnBagCount DEFAULT (0) WITH VALUES;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.t_SupervisorMethLooseEntry')
      AND name = N'CK_t_SupervisorMethLooseEntry_ReturnCount'
)
    ALTER TABLE dbo.t_SupervisorMethLooseEntry WITH CHECK
        ADD CONSTRAINT CK_t_SupervisorMethLooseEntry_ReturnCount CHECK (ReturnBagCount >= 0);
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetSupervisorMethEntry
    @UnloadId INT,
    @SupervisorId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS
    (
        SELECT 1 FROM dbo.t_UnloadTransaction
        WHERE UnloadId = @UnloadId AND SupervisorId = @SupervisorId
    )
        THROW 51001, 'This RST is not assigned to the logged-in supervisor.', 1;

    SELECT e.EntryId, e.BagTypeId, b.BagTypeName, e.BagCount, e.PerBagPrice, e.TotalAmount
    FROM dbo.t_SupervisorMethEntry e
    INNER JOIN dbo.m_BagType b ON b.BagTypeId = e.BagTypeId
    WHERE e.UnloadId = @UnloadId
    ORDER BY b.BagTypeName, e.EntryId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetSupervisorMethLooseEntry
    @UnloadId INT,
    @SupervisorId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS
    (
        SELECT 1 FROM dbo.t_UnloadTransaction
        WHERE UnloadId = @UnloadId AND SupervisorId = @SupervisorId
    )
        THROW 51001, 'This RST is not assigned to the logged-in supervisor.', 1;

    SELECT LooseBagCount, ReturnBagCount
    FROM dbo.t_SupervisorMethLooseEntry
    WHERE UnloadId = @UnloadId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_SaveSupervisorMethEntry
    @UnloadId INT,
    @SupervisorId INT,
    @LooseBagCount INT,
    @ReturnBagCount INT,
    @Rows NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @MethId INT;
    SELECT @MethId = MethId
    FROM dbo.t_UnloadTransaction
    WHERE UnloadId = @UnloadId AND SupervisorId = @SupervisorId;

    IF ISNULL(@MethId, 0) <= 0
        THROW 51002, 'Assigned Meth was not found for this RST.', 1;
    IF ISJSON(@Rows) <> 1
        THROW 51003, 'Meth unloading rows are invalid.', 1;
    IF @LooseBagCount IS NULL OR @LooseBagCount < 0
        THROW 51008, 'Loose bag count cannot be negative.', 1;
    IF @ReturnBagCount IS NULL OR @ReturnBagCount < 0
        THROW 51009, 'Return bag count cannot be negative.', 1;

    DECLARE @Data TABLE
    (
        BagTypeId INT NOT NULL,
        BagCount INT NOT NULL,
        PerBagPrice DECIMAL(18,2) NOT NULL
    );

    INSERT @Data(BagTypeId, BagCount, PerBagPrice)
    SELECT BagTypeId, BagCount, PerBagPrice
    FROM OPENJSON(@Rows)
    WITH
    (
        BagTypeId INT '$.BagTypeId',
        BagCount INT '$.BagCount',
        PerBagPrice DECIMAL(18,2) '$.PerBagPrice'
    );

    IF NOT EXISTS (SELECT 1 FROM @Data)
        THROW 51004, 'Enter at least one Meth unloading row.', 1;
    IF EXISTS (SELECT 1 FROM @Data WHERE BagTypeId <= 0 OR BagCount <= 0 OR PerBagPrice < 0)
        THROW 51005, 'Bag type, bag count or per bag price is invalid.', 1;
    IF EXISTS (SELECT BagTypeId FROM @Data GROUP BY BagTypeId HAVING COUNT(*) > 1)
        THROW 51006, 'Same bag type cannot be entered twice.', 1;
    IF EXISTS (SELECT 1 FROM @Data d LEFT JOIN dbo.m_BagType b ON b.BagTypeId = d.BagTypeId AND b.IsActive = 1 WHERE b.BagTypeId IS NULL)
        THROW 51007, 'Selected bag type is not active.', 1;

    BEGIN TRANSACTION;
    DELETE FROM dbo.t_SupervisorMethEntry WHERE UnloadId = @UnloadId;

    INSERT dbo.t_SupervisorMethEntry
        (UnloadId, MethId, WorkType, BagTypeId, BagCount, PerBagPrice, EnteredBySupervisorId)
    SELECT @UnloadId, @MethId, N'Unloading', BagTypeId, BagCount, PerBagPrice, @SupervisorId
    FROM @Data;

    UPDATE dbo.t_SupervisorMethLooseEntry
    SET MethId = @MethId,
        LooseBagCount = @LooseBagCount,
        ReturnBagCount = @ReturnBagCount,
        EnteredBySupervisorId = @SupervisorId,
        UpdatedAt = SYSDATETIME()
    WHERE UnloadId = @UnloadId;

    IF @@ROWCOUNT = 0
        INSERT dbo.t_SupervisorMethLooseEntry
            (UnloadId, MethId, LooseBagCount, ReturnBagCount, EnteredBySupervisorId)
        VALUES
            (@UnloadId, @MethId, @LooseBagCount, @ReturnBagCount, @SupervisorId);
    COMMIT TRANSACTION;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetSupervisorMethWorkRegister
    @SupervisorId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT u.UnloadId,u.RSTNumber,u.SupervisorId,u.MethId,u.LocationId,u.ItemId,u.NumberOfBags,u.Status,u.UnloadTime,
           ISNULL(m.PersonName,'') AS MethName, COALESCE(assigned.LocationName,loc.LocationName,'') AS LocationName,
           ISNULL(v.VehicleNumber,'') AS VehicleNumber, ISNULL(p.PersonName,'') AS PartyName,
           ISNULL(i.ItemName,'') AS ItemName,
           ISNULL(methTotals.TotalBags,0) AS MethBagTotal,
           CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.t_WorkerAllocation wa WHERE wa.UnloadId=u.UnloadId AND ISNULL(wa.BagCount,0)>0) THEN 1 ELSE 0 END AS bit) AS IsWorkerWorkCompleted,
           CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.t_SupervisorMethEntry me WHERE me.UnloadId=u.UnloadId) THEN 1 ELSE 0 END AS bit) AS IsMethWorkEntered,
           CAST(CASE WHEN sa.SupervisorActionId IS NOT NULL THEN 1 ELSE 0 END AS bit) AS IsSupervisorActionCompleted
    FROM dbo.t_UnloadTransaction u
    LEFT JOIN dbo.t_GateEntry ge ON ge.RSTNumber=u.RSTNumber
    LEFT JOIN dbo.P02_Person m ON m.PersonId=u.MethId
    LEFT JOIN dbo.m_LocationMaster loc ON loc.LocationId=u.LocationId
    OUTER APPLY
    (
        SELECT STRING_AGG(CONVERT(nvarchar(max), lm.LocationName), ', ') WITHIN GROUP (ORDER BY lm.LocationName) AS LocationName
        FROM dbo.t_UnloadLocation ul
        INNER JOIN dbo.m_LocationMaster lm ON lm.LocationId=ul.LocationId
        WHERE ul.UnloadId=u.UnloadId AND ul.IsActive=1
    ) assigned
    OUTER APPLY
    (
        SELECT SUM(me.BagCount) AS TotalBags
        FROM dbo.t_SupervisorMethEntry me
        WHERE me.UnloadId = u.UnloadId
    ) methTotals
    LEFT JOIN dbo.m_Vehicle v ON v.VehicleId=ge.VehicleId
    LEFT JOIN dbo.P02_Person p ON p.PersonId=ge.PartyId
    LEFT JOIN dbo.m_Item i ON i.ItemId=u.ItemId
    LEFT JOIN dbo.t_SupervisorUnloadAction sa ON sa.UnloadId=u.UnloadId
    WHERE u.SupervisorId=@SupervisorId AND ISNULL(u.MethId,0)>0 AND ISNULL(u.LocationId,0)>0
    ORDER BY u.UnloadTime DESC, u.UnloadId DESC;
END;
GO
