USE RiceMillDB;
GO

-- 1. Create Gate Master Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[m_GateMaster]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[m_GateMaster] (
        [GateId] INT IDENTITY(1,1) PRIMARY KEY,
        [GateCode] NVARCHAR(50) NOT NULL UNIQUE,
        [GateName] NVARCHAR(100) NOT NULL,
        [GateType] NVARCHAR(50) NOT NULL, -- Main Gate, Dispatch Gate, Staff Gate, Gate-1, etc.
        [LocationArea] NVARCHAR(200) NULL,
        [DepartmentId] INT NULL,
        [InwardAllowed] BIT DEFAULT 1,
        [OutwardAllowed] BIT DEFAULT 1,
        [IsActive] BIT DEFAULT 1,
        [CreatedDate] DATETIME DEFAULT GETDATE()
    );
END
GO

-- Seed Default Gates
IF NOT EXISTS (SELECT 1 FROM m_GateMaster)
BEGIN
    INSERT INTO m_GateMaster (GateCode, GateName, GateType, LocationArea, InwardAllowed, OutwardAllowed) VALUES
    ('GATE-01', 'Main Gate 1', 'Main Gate', 'Front Entrance', 1, 1),
    ('GATE-02', 'Dispatch Gate 2', 'Dispatch Gate', 'Yard East', 0, 1),
    ('GATE-03', 'Staff & Visitor Gate', 'Staff Gate', 'Admin Office Side', 1, 1);
END
GO

-- 2. Create Department Master Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[m_DepartmentMaster]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[m_DepartmentMaster] (
        [DepartmentId] INT IDENTITY(1,1) PRIMARY KEY,
        [DepartmentCode] NVARCHAR(50) NOT NULL UNIQUE,
        [DepartmentName] NVARCHAR(100) NOT NULL,
        [OfficeId] INT NULL,
        [ResponsibleOfficerId] INT NULL,
        [IsActive] BIT DEFAULT 1,
        [CreatedDate] DATETIME DEFAULT GETDATE()
    );
END
GO

-- Seed Default Departments
IF NOT EXISTS (SELECT 1 FROM m_DepartmentMaster)
BEGIN
    INSERT INTO m_DepartmentMaster (DepartmentCode, DepartmentName) VALUES
    ('DEP-ADM', 'Admin & HR'),
    ('DEP-PUR', 'Purchase & Procurement'),
    ('DEP-STR', 'Stores & Inventory'),
    ('DEP-PRD', 'Production & Milling'),
    ('DEP-DSP', 'Dispatch & Logistics'),
    ('DEP-ACC', 'Accounts & Billing'),
    ('DEP-LAB', 'Quality Assurance & Lab'),
    ('DEP-MNT', 'Maintenance & Electrical');
END
GO

-- 3. Create Visitor Register Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[t_VisitorRegister]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[t_VisitorRegister] (
        [VisitorId] INT IDENTITY(1,1) PRIMARY KEY,
        [VisitorNo] NVARCHAR(50) NOT NULL UNIQUE,
        [GateId] INT NOT NULL,
        [VisitDateTime] DATETIME DEFAULT GETDATE(),
        [VisitorName] NVARCHAR(200) NOT NULL,
        [MobileNo] NVARCHAR(20) NOT NULL,
        [CompanyOrg] NVARCHAR(200) NULL,
        [PurposeOfVisit] NVARCHAR(100) NOT NULL, -- Official, Vendor, Customer, Interview, Other
        [PersonToMeetId] INT NULL,
        [PersonToMeetName] NVARCHAR(200) NULL,
        [DepartmentId] INT NULL,
        [VisitorIDType] NVARCHAR(50) NULL, -- DL, PAN, Aadhaar, Passport, Other
        [IDReference] NVARCHAR(100) NULL,
        [VehicleNo] NVARCHAR(50) NULL,
        [VisitorPassNo] NVARCHAR(50) NULL,
        [ExitDateTime] DATETIME NULL,
        [PassStatus] NVARCHAR(50) DEFAULT 'Active', -- Active, Exited, Lost, Cancelled
        [Remarks] NVARCHAR(MAX) NULL,
        [CreatedBy] INT NOT NULL,
        [CreatedDate] DATETIME DEFAULT GETDATE()
    );
END
GO

-- 4. Create Temporary Material / Tools Inward & Return Register Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[t_TempMaterialRegister]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[t_TempMaterialRegister] (
        [TempMaterialId] INT IDENTITY(1,1) PRIMARY KEY,
        [EntryNo] NVARCHAR(50) NOT NULL UNIQUE,
        [OutwardNo] NVARCHAR(50) NULL,
        [GateId] INT NOT NULL,
        [EntryDateTime] DATETIME DEFAULT GETDATE(),
        [OwnerVendor] NVARCHAR(200) NOT NULL,
        [VehicleNo] NVARCHAR(50) NULL,
        [ItemCategory] NVARCHAR(100) NOT NULL, -- Machine, Tool, Electrical, Spare, Other
        [ItemDescription] NVARCHAR(MAX) NOT NULL,
        [Quantity] DECIMAL(18,2) NOT NULL,
        [Unit] NVARCHAR(20) NOT NULL,
        [SerialAssetNo] NVARCHAR(100) NULL,
        [Purpose] NVARCHAR(100) NOT NULL, -- Repair, Service, Installation, Demo, Other
        [DepartmentId] INT NULL,
        [ExpectedReturnDate] DATETIME NULL,
        [ReturnDateTime] DATETIME NULL,
        [GatePassNo] NVARCHAR(50) NULL,
        [SecurityVerification] NVARCHAR(50) DEFAULT 'Checked', -- Checked, Exception
        [Status] NVARCHAR(50) DEFAULT 'In Yard', -- In Yard, Returned, Overdue
        [Remarks] NVARCHAR(MAX) NULL,
        [CreatedBy] INT NOT NULL,
        [CreatedDate] DATETIME DEFAULT GETDATE()
    );
END
GO

-- 5. Create Security Incident / Exception Register Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[t_SecurityIncident]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[t_SecurityIncident] (
        [IncidentId] INT IDENTITY(1,1) PRIMARY KEY,
        [IncidentNo] NVARCHAR(50) NOT NULL UNIQUE,
        [GateId] INT NOT NULL,
        [IncidentDateTime] DATETIME DEFAULT GETDATE(),
        [IncidentType] NVARCHAR(100) NOT NULL, -- Document Mismatch, Unauthorized Movement, Vehicle Issue, Material Mismatch, Visitor Issue, Other
        [RelatedEntryNo] NVARCHAR(100) NULL,
        [Description] NVARCHAR(MAX) NOT NULL,
        [ActionTaken] NVARCHAR(MAX) NULL,
        [ReportedToPersonId] INT NULL,
        [Severity] NVARCHAR(50) DEFAULT 'Medium', -- Low, Medium, High, Critical
        [Status] NVARCHAR(50) DEFAULT 'Open', -- Open, Under Review, Resolved, Closed
        [ResolvedBy] INT NULL,
        [ResolutionDate] DATETIME NULL,
        [CreatedBy] INT NOT NULL
    );
END
GO

-- 6. Core Stored Procedures for Masters & PDF Operations

-- SP: Gate Master CRUD
CREATE OR ALTER PROCEDURE sp_ManageGateMaster
    @Action NVARCHAR(20), -- 'GETALL', 'INSERT', 'UPDATE', 'DELETE'
    @GateId INT = NULL,
    @GateCode NVARCHAR(50) = NULL,
    @GateName NVARCHAR(100) = NULL,
    @GateType NVARCHAR(50) = NULL,
    @LocationArea NVARCHAR(200) = NULL,
    @DepartmentId INT = NULL,
    @InwardAllowed BIT = 1,
    @OutwardAllowed BIT = 1,
    @IsActive BIT = 1
AS
BEGIN
    IF @Action = 'GETALL'
    BEGIN
        SELECT g.*, d.DepartmentName 
        FROM m_GateMaster g
        LEFT JOIN m_DepartmentMaster d ON g.DepartmentId = d.DepartmentId
        WHERE g.IsActive = 1
        ORDER BY g.GateId DESC;
    END
    ELSE IF @Action = 'INSERT'
    BEGIN
        INSERT INTO m_GateMaster (GateCode, GateName, GateType, LocationArea, DepartmentId, InwardAllowed, OutwardAllowed, IsActive)
        VALUES (@GateCode, @GateName, @GateType, @LocationArea, @DepartmentId, @InwardAllowed, @OutwardAllowed, 1);
        SELECT SCOPE_IDENTITY() AS GateId;
    END
    ELSE IF @Action = 'UPDATE'
    BEGIN
        UPDATE m_GateMaster
        SET GateCode = @GateCode, GateName = @GateName, GateType = @GateType, 
            LocationArea = @LocationArea, DepartmentId = @DepartmentId, 
            InwardAllowed = @InwardAllowed, OutwardAllowed = @OutwardAllowed, IsActive = @IsActive
        WHERE GateId = @GateId;
    END
    ELSE IF @Action = 'DELETE'
    BEGIN
        UPDATE m_GateMaster SET IsActive = 0 WHERE GateId = @GateId;
    END
END
GO

-- SP: Department Master CRUD
CREATE OR ALTER PROCEDURE sp_ManageDepartmentMaster
    @Action NVARCHAR(20), -- 'GETALL', 'INSERT', 'UPDATE', 'DELETE'
    @DepartmentId INT = NULL,
    @DepartmentCode NVARCHAR(50) = NULL,
    @DepartmentName NVARCHAR(100) = NULL,
    @OfficeId INT = NULL,
    @ResponsibleOfficerId INT = NULL,
    @IsActive BIT = 1
AS
BEGIN
    IF @Action = 'GETALL'
    BEGIN
        SELECT d.*, o.OfficeName, p.PersonName AS ResponsibleOfficerName
        FROM m_DepartmentMaster d
        LEFT JOIN o05_office o ON d.OfficeId = o.OfficeId
        LEFT JOIN p02_Person p ON d.ResponsibleOfficerId = p.PersonId
        WHERE d.IsActive = 1
        ORDER BY d.DepartmentId DESC;
    END
    ELSE IF @Action = 'INSERT'
    BEGIN
        INSERT INTO m_DepartmentMaster (DepartmentCode, DepartmentName, OfficeId, ResponsibleOfficerId, IsActive)
        VALUES (@DepartmentCode, @DepartmentName, @OfficeId, @ResponsibleOfficerId, 1);
        SELECT SCOPE_IDENTITY() AS DepartmentId;
    END
    ELSE IF @Action = 'UPDATE'
    BEGIN
        UPDATE m_DepartmentMaster
        SET DepartmentCode = @DepartmentCode, DepartmentName = @DepartmentName, 
            OfficeId = @OfficeId, ResponsibleOfficerId = @ResponsibleOfficerId, IsActive = @IsActive
        WHERE DepartmentId = @DepartmentId;
    END
    ELSE IF @Action = 'DELETE'
    BEGIN
        UPDATE m_DepartmentMaster SET IsActive = 0 WHERE DepartmentId = @DepartmentId;
    END
END
GO

-- SP: Create Visitor Registration & Pass
CREATE OR ALTER PROCEDURE sp_CreateVisitorEntry
    @GateId INT,
    @VisitorName NVARCHAR(200),
    @MobileNo NVARCHAR(20),
    @CompanyOrg NVARCHAR(200),
    @PurposeOfVisit NVARCHAR(100),
    @PersonToMeetId INT,
    @PersonToMeetName NVARCHAR(200),
    @DepartmentId INT,
    @VisitorIDType NVARCHAR(50),
    @IDReference NVARCHAR(100),
    @VehicleNo NVARCHAR(50),
    @Remarks NVARCHAR(MAX),
    @CreatedBy INT
AS
BEGIN
    DECLARE @GlobalNo INT, @VisitorNo NVARCHAR(50), @VisitorPassNo NVARCHAR(50);
    EXEC sp_GetNextSerialNumber 'GLOBAL', @GlobalNo OUTPUT;
    
    SET @VisitorNo = 'VIS-' + CAST(@GlobalNo AS NVARCHAR(20));
    SET @VisitorPassNo = 'PASS-' + CAST(@GlobalNo AS NVARCHAR(20));

    INSERT INTO t_VisitorRegister (
        VisitorNo, GateId, VisitorName, MobileNo, CompanyOrg, PurposeOfVisit,
        PersonToMeetId, PersonToMeetName, DepartmentId, VisitorIDType, IDReference,
        VehicleNo, VisitorPassNo, PassStatus, Remarks, CreatedBy
    )
    VALUES (
        @VisitorNo, @GateId, @VisitorName, @MobileNo, @CompanyOrg, @PurposeOfVisit,
        @PersonToMeetId, @PersonToMeetName, @DepartmentId, @VisitorIDType, @IDReference,
        @VehicleNo, @VisitorPassNo, 'Active', @Remarks, @CreatedBy
    );

    SELECT @VisitorNo AS VisitorNo, @VisitorPassNo AS VisitorPassNo;
END
GO

-- SP: Exit Visitor
CREATE OR ALTER PROCEDURE sp_ExitVisitor
    @VisitorNo NVARCHAR(50)
AS
BEGIN
    UPDATE t_VisitorRegister
    SET ExitDateTime = GETDATE(), PassStatus = 'Exited'
    WHERE VisitorNo = @VisitorNo AND PassStatus = 'Active';
END
GO

-- SP: Create Temp Material Entry (Tools / Equipment Repair Inward)
CREATE OR ALTER PROCEDURE sp_CreateTempMaterialEntry
    @GateId INT,
    @OwnerVendor NVARCHAR(200),
    @VehicleNo NVARCHAR(50),
    @ItemCategory NVARCHAR(100),
    @ItemDescription NVARCHAR(MAX),
    @Quantity DECIMAL(18,2),
    @Unit NVARCHAR(20),
    @SerialAssetNo NVARCHAR(100),
    @Purpose NVARCHAR(100),
    @DepartmentId INT,
    @ExpectedReturnDate DATETIME,
    @Remarks NVARCHAR(MAX),
    @CreatedBy INT
AS
BEGIN
    DECLARE @GlobalNo INT, @EntryNo NVARCHAR(50), @GatePassNo NVARCHAR(50);
    EXEC sp_GetNextSerialNumber 'GLOBAL', @GlobalNo OUTPUT;
    
    SET @EntryNo = 'TMPIN-' + CAST(@GlobalNo AS NVARCHAR(20));
    SET @GatePassNo = 'GP-' + CAST(@GlobalNo AS NVARCHAR(20));

    INSERT INTO t_TempMaterialRegister (
        EntryNo, GateId, OwnerVendor, VehicleNo, ItemCategory, ItemDescription,
        Quantity, Unit, SerialAssetNo, Purpose, DepartmentId, ExpectedReturnDate,
        GatePassNo, Status, Remarks, CreatedBy
    )
    VALUES (
        @EntryNo, @GateId, @OwnerVendor, @VehicleNo, @ItemCategory, @ItemDescription,
        @Quantity, @Unit, @SerialAssetNo, @Purpose, @DepartmentId, @ExpectedReturnDate,
        @GatePassNo, 'In Yard', @Remarks, @CreatedBy
    );

    SELECT @EntryNo AS EntryNo, @GatePassNo AS GatePassNo;
END
GO

-- SP: Return Temp Material (Outward)
CREATE OR ALTER PROCEDURE sp_ReturnTempMaterial
    @EntryNo NVARCHAR(50),
    @Remarks NVARCHAR(MAX)
AS
BEGIN
    DECLARE @GlobalNo INT, @OutwardNo NVARCHAR(50);
    EXEC sp_GetNextSerialNumber 'GLOBAL', @GlobalNo OUTPUT;
    
    SET @OutwardNo = 'TMPOUT-' + CAST(@GlobalNo AS NVARCHAR(20));

    UPDATE t_TempMaterialRegister
    SET OutwardNo = @OutwardNo, ReturnDateTime = GETDATE(), Status = 'Returned',
        Remarks = ISNULL(Remarks, '') + ' | ' + ISNULL(@Remarks, '')
    WHERE EntryNo = @EntryNo AND Status = 'In Yard';

    SELECT @OutwardNo AS OutwardNo;
END
GO

-- SP: Security Incident Logging
CREATE OR ALTER PROCEDURE sp_CreateSecurityIncident
    @GateId INT,
    @IncidentType NVARCHAR(100),
    @RelatedEntryNo NVARCHAR(100),
    @Description NVARCHAR(MAX),
    @ActionTaken NVARCHAR(MAX),
    @ReportedToPersonId INT,
    @Severity NVARCHAR(50),
    @CreatedBy INT
AS
BEGIN
    DECLARE @GlobalNo INT, @IncidentNo NVARCHAR(50);
    EXEC sp_GetNextSerialNumber 'GLOBAL', @GlobalNo OUTPUT;
    
    SET @IncidentNo = 'INC-' + CAST(@GlobalNo AS NVARCHAR(20));

    INSERT INTO t_SecurityIncident (
        IncidentNo, GateId, IncidentType, RelatedEntryNo, Description,
        ActionTaken, ReportedToPersonId, Severity, Status, CreatedBy
    )
    VALUES (
        @IncidentNo, @GateId, @IncidentType, @RelatedEntryNo, @Description,
        @ActionTaken, @ReportedToPersonId, @Severity, 'Open', @CreatedBy
    );

    SELECT @IncidentNo AS IncidentNo;
END
GO
