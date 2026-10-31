SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

CREATE OR ALTER PROCEDURE dbo.sp_CreateGateEntry
    @RSTNumber NVARCHAR(100),
    @InwardNo NVARCHAR(100),
    @VehicleId INT,
    @PartyId INT,
    @DriverId INT,
    @GrossWeight DECIMAL(18,2),
    @ItemId INT,
    @CategoryId INT = NULL,
    @TargetOfficeId INT = NULL,
    @CreatedBy INT,
    @WeightCharge DECIMAL(18,2) = 0
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @RSTNumber = LTRIM(RTRIM(ISNULL(@RSTNumber, N'')));
    IF LEN(@RSTNumber) NOT BETWEEN 1 AND 6 OR @RSTNumber LIKE N'%[^0-9]%'
        THROW 50001, 'RST number must contain only 1 to 6 digits.', 1;
    IF ISNULL(@InwardNo, '') = ''
        THROW 50001, 'Gate inward entry is required.', 1;
    IF @GrossWeight <= 0
        THROW 50001, 'Gross Weight must be greater than zero.', 1;
    IF ISNULL(@CategoryId, 0) = 0 OR NOT EXISTS
        (SELECT 1 FROM dbo.m_ItemCategory WHERE CategoryId = @CategoryId AND IsActive = 1)
        THROW 50001, 'Select an active product category.', 1;

    IF ISNULL(@TargetOfficeId, 0) = 0
        SELECT @TargetOfficeId = g.OfficeId
        FROM dbo.t_InwardHeader h
        INNER JOIN dbo.m_GateMaster g ON g.GateId = h.GateId
        WHERE h.InwardNo = @InwardNo;

    IF ISNULL(@TargetOfficeId, 0) = 0
        THROW 50001, 'Company could not be determined from the inward gate.', 1;
    IF NOT EXISTS
        (SELECT 1 FROM dbo.o05_office WHERE OfficeId = @TargetOfficeId AND IsActive = 1 AND OfficeType = 1)
        THROW 50001, 'Invalid or inactive company.', 1;

    BEGIN TRANSACTION;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.t_GateEntry WITH (UPDLOCK, HOLDLOCK)
        WHERE RSTNumber = @RSTNumber
    )
        THROW 50001, 'This RST number already exists. Enter a unique RST number.', 1;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.t_InwardHeader WITH (UPDLOCK, HOLDLOCK)
        WHERE InwardNo = @InwardNo AND ISNULL(RSTEntryCompleted, 0) = 0
    )
        THROW 50001, 'This inward is closed for new RST entries.', 1;

    IF EXISTS
        (SELECT 1 FROM dbo.t_GateEntry WHERE InwardNo = @InwardNo AND ItemId = @ItemId AND TareWeight IS NULL)
        THROW 50001, 'An open RST already exists for this variety and inward.', 1;
    IF EXISTS
        (SELECT 1 FROM dbo.t_GateEntry WHERE InwardNo = @InwardNo AND Status IN ('WaitingForSupervisor','Entered'))
        THROW 50001, 'Supervisor must complete the current RST before another RST can be saved.', 1;

    INSERT dbo.t_GateEntry
        (RSTNumber, InwardNo, VehicleId, PartyId, DriverId, ItemId, CategoryId, GrossWeight,
         TargetOfficeId, GateEntryTime, StatusId, Status, CreatedBy, WeighmentCharge, InwardOutward)
    VALUES
        (@RSTNumber, @InwardNo, @VehicleId, @PartyId, @DriverId, NULL, @CategoryId, @GrossWeight,
         @TargetOfficeId, GETDATE(), 1, 'WaitingForSupervisor', @CreatedBy, ISNULL(@WeightCharge, 0), 'Inward');

    UPDATE dbo.t_InwardHeader SET IsRSTGenerated = 1 WHERE InwardNo = @InwardNo;
    COMMIT TRANSACTION;
    SELECT @RSTNumber;
END;
GO
