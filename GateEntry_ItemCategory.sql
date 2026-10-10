-- Apply after GateEntry_Inward_Workflow.sql. Existing RSTs keep a NULL category.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.t_GateEntry', 'ItemCategoryId') IS NULL
    ALTER TABLE dbo.t_GateEntry ADD ItemCategoryId INT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_t_GateEntry_ItemCategory')
    ALTER TABLE dbo.t_GateEntry WITH CHECK ADD CONSTRAINT FK_t_GateEntry_ItemCategory
        FOREIGN KEY (ItemCategoryId) REFERENCES dbo.m_ItemCategory(CategoryId);
GO
CREATE OR ALTER PROCEDURE dbo.sp_CreateGateEntry
    @InwardNo NVARCHAR(100),
    @VehicleId INT,
    @PartyId INT,
    @DriverId INT,
    @GrossWeight DECIMAL(18,2),
    @ItemId INT,
    @TargetOfficeId INT = NULL,
    @CreatedBy INT,
    @WeightCharge DECIMAL(18,2) = 0,
    @ItemCategoryId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF ISNULL(@InwardNo, '') = ''
        THROW 50001, 'Gate inward entry is required.', 1;
    IF @GrossWeight IS NULL OR @GrossWeight <= 0
        THROW 50001, 'Gross Weight must be greater than zero.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.m_ItemCategory WHERE CategoryId = @ItemCategoryId AND IsActive = 1)
        THROW 50001, 'Select an active item category.', 1;

    -- Variety is assigned later by Supervisor; category and item IDs are separate.
    IF @ItemId IS NOT NULL AND NOT EXISTS (
        SELECT 1 FROM dbo.m_Item WHERE ItemId = @ItemId AND IsActive = 1 AND CategoryId = @ItemCategoryId)
        THROW 50001, 'Select an active material or variety belonging to the selected category.', 1;

    IF ISNULL(@TargetOfficeId, 0) = 0
        SELECT @TargetOfficeId = g.OfficeId
        FROM dbo.t_InwardHeader h
        INNER JOIN dbo.m_GateMaster g ON g.GateId = h.GateId
        WHERE h.InwardNo = @InwardNo;
    IF ISNULL(@TargetOfficeId, 0) = 0
        THROW 50001, 'Company could not be determined from the inward gate.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.o05_office WHERE OfficeId = @TargetOfficeId AND IsActive = 1 AND OfficeType = 1)
        THROW 50001, 'Invalid or inactive company.', 1;

    BEGIN TRY
        BEGIN TRANSACTION;
        IF NOT EXISTS (
            SELECT 1 FROM dbo.t_InwardHeader WITH (UPDLOCK, HOLDLOCK)
            WHERE InwardNo = @InwardNo AND ISNULL(RSTEntryCompleted, 0) = 0)
            THROW 50001, 'This inward is closed for new RST entries.', 1;

        IF EXISTS (
            SELECT 1 FROM dbo.t_GateEntry WHERE InwardNo = @InwardNo AND TareWeight IS NULL
              AND (ItemId = @ItemId OR (@ItemId IS NULL AND ItemCategoryId = @ItemCategoryId)))
            THROW 50001, 'An open RST already exists for this material/category and inward.', 1;

        DECLARE @Year CHAR(4) = CONVERT(CHAR(4), YEAR(GETDATE()));
        DECLARE @NextNo INT;
        SELECT @NextNo = ISNULL(MAX(TRY_CONVERT(INT,
            RIGHT(RSTNumber, CHARINDEX('-', REVERSE(RSTNumber)) - 1))), 0) + 1
        FROM dbo.t_GateEntry WITH (UPDLOCK, HOLDLOCK)
        WHERE RSTNumber LIKE 'RST-' + @Year + '-%';
        DECLARE @RSTNumber NVARCHAR(100) = 'RST-' + @Year + '-' +
            RIGHT('000000' + CONVERT(VARCHAR(12), @NextNo), 6);

        INSERT INTO dbo.t_GateEntry
            (RSTNumber, InwardNo, VehicleId, PartyId, DriverId, ItemId, ItemCategoryId,
             GrossWeight, TargetOfficeId, GateEntryTime, StatusId, Status, CreatedBy, WeighmentCharge, InwardOutward)
        VALUES
            (@RSTNumber, @InwardNo, @VehicleId, @PartyId, @DriverId, @ItemId, @ItemCategoryId,
             @GrossWeight, @TargetOfficeId, GETDATE(), 1, 'WaitingForSupervisor', @CreatedBy,
             ISNULL(@WeightCharge, 0), 'Inward');
        UPDATE dbo.t_InwardHeader SET IsRSTGenerated = 1 WHERE InwardNo = @InwardNo;
        COMMIT;
        SELECT @RSTNumber;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK;
        THROW;
    END CATCH;
END;
GO
COMMIT;
