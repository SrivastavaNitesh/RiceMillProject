USE [RiceMillDB];
GO

-- ============================================================================
-- 1. INWARD TYPE MASTER TABLE
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'm_InwardTypeMaster')
BEGIN
    CREATE TABLE m_InwardTypeMaster (
        InwardTypeId INT IDENTITY(1,1) PRIMARY KEY,
        InwardTypeName NVARCHAR(100) NOT NULL,
        RSTRequired BIT NOT NULL DEFAULT 1,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedBy INT NULL,
        CreatedDate DATETIME NULL DEFAULT GETDATE(),
        ModifiedBy INT NULL,
        ModifiedDate DATETIME NULL
    );

    INSERT INTO m_InwardTypeMaster (InwardTypeName, RSTRequired, IsActive) VALUES
    ('Paddy Inward', 1, 1),
    ('Spare Parts Inward', 0, 1),
    ('Packing Material Inward', 0, 1),
    ('Stationery Inward', 0, 1),
    ('Other Inward', 0, 1);
END
GO

-- ============================================================================
-- 2. VEHICLE TYPE MASTER TABLE
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'm_VehicleTypeMaster')
BEGIN
    CREATE TABLE m_VehicleTypeMaster (
        VehicleTypeId INT IDENTITY(1,1) PRIMARY KEY,
        VehicleTypeName NVARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedBy INT NULL,
        CreatedDate DATETIME NULL DEFAULT GETDATE(),
        ModifiedBy INT NULL,
        ModifiedDate DATETIME NULL
    );

    INSERT INTO m_VehicleTypeMaster (VehicleTypeName, IsActive) VALUES
    ('Truck', 1),
    ('Tractor', 1),
    ('Trolley', 1),
    ('Mini Truck', 1),
    ('Other', 1);
END
GO

-- ============================================================================
-- 3. STATUS MASTER TABLE
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'm_StatusMaster')
BEGIN
    CREATE TABLE m_StatusMaster (
        StatusId INT IDENTITY(1,1) PRIMARY KEY,
        StatusName NVARCHAR(50) NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedBy INT NULL,
        CreatedDate DATETIME NULL DEFAULT GETDATE()
    );

    INSERT INTO m_StatusMaster (StatusId, StatusName, IsActive) VALUES
    (1, 'Active', 1),
    (2, 'Cancelled', 1),
    (3, 'Pending', 1),
    (4, 'Processed', 1);
END
GO

-- ============================================================================
-- 4. GATE ENTRY / INWARD HEADER TABLE
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 't_InwardHeader')
BEGIN
    CREATE TABLE t_InwardHeader (
        InwardId INT IDENTITY(1,1) PRIMARY KEY,
        GateEntryNo NVARCHAR(50) NOT NULL,
        InwardNo NVARCHAR(50) NOT NULL UNIQUE,
        InwardDate DATE NOT NULL DEFAULT GETDATE(),
        InwardTime NVARCHAR(20) NOT NULL,
        GateId INT NOT NULL,
        InwardTypeId INT NOT NULL,
        PartyId INT NOT NULL,
        TransporterName NVARCHAR(150) NULL,
        VehicleNo NVARCHAR(50) NOT NULL,
        VehicleTypeId INT NOT NULL,
        DriverName NVARCHAR(150) NOT NULL,
        DriverMobile NVARCHAR(15) NULL,
        ChallanNo NVARCHAR(100) NULL,
        ApproxNoOfBags INT NULL DEFAULT 0,
        ApproxWeight DECIMAL(18,2) NULL DEFAULT 0,
        PurposeRemarks NVARCHAR(250) NULL,
        GateInDateTime DATETIME NOT NULL DEFAULT GETDATE(),
        CreatedBy INT NULL,
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        StatusId INT NOT NULL DEFAULT 1,
        IsRSTGenerated BIT NOT NULL DEFAULT 0
    );
END
GO

-- ============================================================================
-- 5. STORED PROCEDURE: MANAGE GATE INWARD ENTRY
-- ============================================================================
CREATE OR ALTER PROCEDURE sp_ManageGateInwardEntry
    @Action VARCHAR(20),
    @InwardId INT = NULL,
    @GateId INT = NULL,
    @InwardTypeId INT = NULL,
    @PartyId INT = NULL,
    @TransporterName NVARCHAR(150) = NULL,
    @VehicleNo NVARCHAR(50) = NULL,
    @VehicleTypeId INT = NULL,
    @DriverName NVARCHAR(150) = NULL,
    @DriverMobile NVARCHAR(15) = NULL,
    @ChallanNo NVARCHAR(100) = NULL,
    @ApproxNoOfBags INT = 0,
    @ApproxWeight DECIMAL(18,2) = 0,
    @PurposeRemarks NVARCHAR(250) = NULL,
    @CreatedBy INT = 1
AS
BEGIN
    SET NOCOUNT ON;

    IF @Action = 'INSERT'
    BEGIN
        DECLARE @Year VARCHAR(4) = CAST(YEAR(GETDATE()) AS VARCHAR(4));
        DECLARE @NextSeq INT;
        SELECT @NextSeq = ISNULL(MAX(InwardId), 0) + 1 FROM t_InwardHeader;
        
        DECLARE @GateEntryNo NVARCHAR(50) = 'GE-' + @Year + '-' + RIGHT('000000' + CAST(@NextSeq AS VARCHAR(10)), 6);
        DECLARE @InwardNo NVARCHAR(50) = 'INW-' + @Year + '-' + RIGHT('000000' + CAST(@NextSeq AS VARCHAR(10)), 6);
        DECLARE @InwardTime NVARCHAR(20) = FORMAT(GETDATE(), 'hh:mm tt');

        INSERT INTO t_InwardHeader (
            GateEntryNo, InwardNo, InwardDate, InwardTime, GateId, InwardTypeId,
            PartyId, TransporterName, VehicleNo, VehicleTypeId, DriverName, DriverMobile,
            ChallanNo, ApproxNoOfBags, ApproxWeight, PurposeRemarks, GateInDateTime,
            CreatedBy, CreatedDate, StatusId, IsRSTGenerated
        )
        VALUES (
            @GateEntryNo, @InwardNo, CAST(GETDATE() AS DATE), @InwardTime, @GateId, @InwardTypeId,
            @PartyId, @TransporterName, @VehicleNo, @VehicleTypeId, @DriverName, @DriverMobile,
            @ChallanNo, @ApproxNoOfBags, @ApproxWeight, @PurposeRemarks, GETDATE(),
            @CreatedBy, GETDATE(), 3, -- 3 = Pending
            0
        );

        SELECT SCOPE_IDENTITY() AS InwardId, @InwardNo AS InwardNo, @GateEntryNo AS GateEntryNo;
    END
    ELSE IF @Action = 'SELECT_ALL'
    BEGIN
        SELECT 
            h.InwardId,
            h.GateEntryNo,
            h.InwardNo,
            h.InwardDate,
            h.InwardTime,
            g.GateName,
            it.InwardTypeName,
            it.RSTRequired,
            p.PersonName AS PartyName,
            h.TransporterName,
            h.VehicleNo,
            vt.VehicleTypeName,
            h.DriverName,
            h.DriverMobile,
            h.ChallanNo,
            h.ApproxNoOfBags,
            h.ApproxWeight,
            h.PurposeRemarks,
            h.GateInDateTime,
            s.StatusName,
            h.IsRSTGenerated
        FROM t_InwardHeader h
        INNER JOIN m_GateMaster g ON h.GateId = g.GateId
        INNER JOIN m_InwardTypeMaster it ON h.InwardTypeId = it.InwardTypeId
        INNER JOIN p1_person p ON h.PartyId = p.p1_personid
        INNER JOIN m_VehicleTypeMaster vt ON h.VehicleTypeId = vt.VehicleTypeId
        INNER JOIN m_StatusMaster s ON h.StatusId = s.StatusId
        ORDER BY h.InwardId DESC;
    END
END
GO
