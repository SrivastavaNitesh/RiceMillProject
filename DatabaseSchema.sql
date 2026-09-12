USE master;
GO

IF NOT EXISTS (SELECT name FROM master.dbo.sysdatabases WHERE name = N'RiceMillDB')
BEGIN
    CREATE DATABASE RiceMillDB;
END
GO

USE RiceMillDB;
GO

-- Drop all foreign key constraints if any exist
DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql += N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id)) + '.' + QUOTENAME(OBJECT_NAME(parent_object_id)) + 
              ' DROP CONSTRAINT ' + QUOTENAME(name) + ';' + CHAR(13)
FROM sys.foreign_keys;
EXEC sp_executesql @sql;
GO

-- Drop existing tables except o10_post
IF OBJECT_ID('dbo.t_GateEntryLocations', 'U') IS NOT NULL DROP TABLE dbo.t_GateEntryLocations;
IF OBJECT_ID('dbo.t_UnloadLocation', 'U') IS NOT NULL DROP TABLE dbo.t_UnloadLocation;
IF OBJECT_ID('dbo.t_WorkerAllocation', 'U') IS NOT NULL DROP TABLE dbo.t_WorkerAllocation;
IF OBJECT_ID('dbo.t_LabQualityCheck', 'U') IS NOT NULL DROP TABLE dbo.t_LabQualityCheck;
IF OBJECT_ID('dbo.t_UnloadTransaction', 'U') IS NOT NULL DROP TABLE dbo.t_UnloadTransaction;
IF OBJECT_ID('dbo.t_GateEntry', 'U') IS NOT NULL DROP TABLE dbo.t_GateEntry;
IF OBJECT_ID('dbo.t_GateInwardOutward', 'U') IS NOT NULL DROP TABLE dbo.t_GateInwardOutward;
IF OBJECT_ID('dbo.m_Challan', 'U') IS NOT NULL DROP TABLE dbo.m_Challan;
IF OBJECT_ID('dbo.m_ChallanItem', 'U') IS NOT NULL DROP TABLE dbo.m_ChallanItem;
IF OBJECT_ID('dbo.m_VehicleMapping', 'U') IS NOT NULL DROP TABLE dbo.m_VehicleMapping;
IF OBJECT_ID('dbo.p_PersonLocation', 'U') IS NOT NULL DROP TABLE dbo.p_PersonLocation;
IF OBJECT_ID('dbo.o_OfficeLocation', 'U') IS NOT NULL DROP TABLE dbo.o_OfficeLocation;
IF OBJECT_ID('dbo.sa05_user', 'U') IS NOT NULL DROP TABLE dbo.sa05_user;
IF OBJECT_ID('dbo.p1_persondesignatation', 'U') IS NOT NULL DROP TABLE dbo.p1_persondesignatation;
IF OBJECT_ID('dbo.o_designatationreletation', 'U') IS NOT NULL DROP TABLE dbo.o_designatationreletation;
IF OBJECT_ID('dbo.p02_Person', 'U') IS NOT NULL DROP TABLE dbo.p02_Person;
IF OBJECT_ID('dbo.o12_designatation', 'U') IS NOT NULL DROP TABLE dbo.o12_designatation;
IF OBJECT_ID('dbo.o05_office', 'U') IS NOT NULL DROP TABLE dbo.o05_office;
IF OBJECT_ID('dbo.m_ItemCategory', 'U') IS NOT NULL DROP TABLE dbo.m_ItemCategory;
IF OBJECT_ID('dbo.m_Item', 'U') IS NOT NULL DROP TABLE dbo.m_Item;
IF OBJECT_ID('dbo.m_BagType', 'U') IS NOT NULL DROP TABLE dbo.m_BagType;
IF OBJECT_ID('dbo.m_Vehicle', 'U') IS NOT NULL DROP TABLE dbo.m_Vehicle;
IF OBJECT_ID('dbo.sa10_usertype', 'U') IS NOT NULL DROP TABLE dbo.sa10_usertype;
IF OBJECT_ID('dbo.m_StatusMaster', 'U') IS NOT NULL DROP TABLE dbo.m_StatusMaster;
IF OBJECT_ID('dbo.t_SerialCounters', 'U') IS NOT NULL DROP TABLE dbo.t_SerialCounters;
GO

-- 1. Ensure o10_post exists (Do not drop!)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[o10_post]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[o10_post] (
        [PostId] INT IDENTITY(1,1) PRIMARY KEY,
        [PostName] NVARCHAR(100) NOT NULL,
        [IsActive] BIT DEFAULT 1
    );
END
GO

-- 2. Master Table sa10_usertype (Same IDs as o10_post)
CREATE TABLE [dbo].[sa10_usertype] (
    [UserTypeId] INT PRIMARY KEY, -- Matches PostId from o10_post
    [UserTypeName] NVARCHAR(100) NOT NULL,
    [LayoutName] NVARCHAR(100) NOT NULL,
    [IsActive] BIT DEFAULT 1
);
GO

-- Populate sa10_usertype based on o10_post
-- Standard posts: 1: Admin, 2: Gate Man, 3: Weighbridge Man / Dharamkata, 4: Supervisor, 5: Lab Tech, 6: Meth, etc.
INSERT INTO sa10_usertype (UserTypeId, UserTypeName, LayoutName)
SELECT PostId, PostName, 
    CASE 
        WHEN PostName LIKE '%Admin%' THEN '_LayoutAdmin'
        WHEN PostName LIKE '%Gate%' THEN '_LayoutGateman'
        WHEN PostName LIKE '%Weight%' OR PostName LIKE '%Weighbridge%' OR PostName LIKE '%Dherm%' THEN '_LayoutWeighbridge'
        WHEN PostName LIKE '%Supervisor%' THEN '_LayoutSupervisor'
        WHEN PostName LIKE '%Lab%' THEN '_LayoutLab'
        ELSE '_LayoutDefault'
    END
FROM o10_post;

-- If o10_post was empty, seed basic posts & usertypes
IF NOT EXISTS (SELECT 1 FROM sa10_usertype WHERE UserTypeId = 1)
BEGIN
    SET IDENTITY_INSERT o10_post ON;
    INSERT INTO o10_post (PostId, PostName, IsActive) VALUES 
    (1, 'Admin', 1),
    (2, 'Gate Man', 1),
    (3, 'Weighbridge Man', 1),
    (4, 'Supervisor', 1),
    (5, 'Lab Technician', 1),
    (6, 'Meth', 1);
    SET IDENTITY_INSERT o10_post OFF;

    INSERT INTO sa10_usertype (UserTypeId, UserTypeName, LayoutName) VALUES
    (1, 'Admin', '_LayoutAdmin'),
    (2, 'Gate Man', '_LayoutGateman'),
    (3, 'Weighbridge Man', '_LayoutWeighbridge'),
    (4, 'Supervisor', '_LayoutSupervisor'),
    (5, 'Lab Technician', '_LayoutLab'),
    (6, 'Meth', '_LayoutDefault');
END
GO

-- 3. Status Master Table (Backend plays with IDs, UI shows Text)
CREATE TABLE [dbo].[m_StatusMaster] (
    [StatusId] INT PRIMARY KEY,
    [StatusCode] NVARCHAR(50) NOT NULL,
    [StatusName] NVARCHAR(100) NOT NULL,
    [IsActive] BIT DEFAULT 1
);
GO

INSERT INTO m_StatusMaster (StatusId, StatusCode, StatusName) VALUES
(1, 'GADI_IN', 'Gadi In'),
(2, 'WEIGHT_DONE', 'Weight Done'),
(3, 'UNLOAD_ASSIGNED', 'Unload Assigned'),
(4, 'UNLOADED', 'Unloaded'),
(5, 'UNLOAD_VERIFIED', 'Unload Verified'),
(6, 'TESTED', 'Quality Tested'),
(7, 'EXITED', 'Exited'),
(8, 'SETTLED', 'Settled'),
(9, 'OUTWARD_PENDING', 'Outward Pending Approval'),
(10, 'OUTWARD_APPROVED', 'Outward Approved'),
(11, 'OUTWARD_REVERTED', 'Outward Reverted');
GO

-- 4. Core Master Tables
CREATE TABLE [dbo].[o05_office] (
    [OfficeId] INT IDENTITY(1,1) PRIMARY KEY,
    [OfficeName] NVARCHAR(100) NOT NULL,
    [Location] NVARCHAR(200),
    [IsActive] BIT DEFAULT 1
);

CREATE TABLE [dbo].[o12_designatation] (
    [DesignationId] INT IDENTITY(1,1) PRIMARY KEY,
    [DesignationName] NVARCHAR(100) NOT NULL,
    [IsActive] BIT DEFAULT 1
);

CREATE TABLE [dbo].[o_designatationreletation] (
    [RelationId] INT IDENTITY(1,1) PRIMARY KEY,
    [o12_parentid] INT NOT NULL, -- Parent designation (e.g., Meth)
    [o12_childid] INT NOT NULL,  -- Child designation (e.g., Worker)
    [parent_postid] INT NULL,
    [child_postid] INT NULL,
    [IsActive] BIT DEFAULT 1
);

CREATE TABLE [dbo].[p02_Person] (
    [PersonId] INT IDENTITY(1,1) PRIMARY KEY,
    [PersonName] NVARCHAR(200) NOT NULL,
    [MobileNumber] NVARCHAR(20),
    [Address] NVARCHAR(500),
    [PartyCategory] INT DEFAULT 1, -- 1: Direct Party, 2: Center Party
    [FirmName] NVARCHAR(200) NULL,  -- For Center Party
    [FirmDesignation] NVARCHAR(100) NULL, -- For Center Party
    [PersonType] NVARCHAR(50), -- Kisan, Gov Office, Driver, Employee, Meth, Worker, etc.
    [IsActive] BIT DEFAULT 1
);

CREATE TABLE [dbo].[p1_persondesignatation] (
    [PersonDesignationId] INT IDENTITY(1,1) PRIMARY KEY,
    [PersonId] INT NOT NULL,
    [DesignationId] INT NOT NULL,
    [IsActive] BIT DEFAULT 1
);

CREATE TABLE [dbo].[sa05_user] (
    [UserId] INT IDENTITY(1,1) PRIMARY KEY,
    [Username] NVARCHAR(100) NOT NULL,
    [PasswordHash] NVARCHAR(256) NOT NULL,
    [PersonId] INT NOT NULL,
    [o10_postid] INT NOT NULL,      -- Post ID
    [sa10_usertypeid] INT NOT NULL, -- Same ID as o10_postid
    [OfficeId] INT NULL,
    [IsActive] BIT DEFAULT 1
);

CREATE TABLE [dbo].[m_ItemCategory] (
    [CategoryId] INT IDENTITY(1,1) PRIMARY KEY,
    [CategoryName] NVARCHAR(100) NOT NULL,
    [IsActive] BIT DEFAULT 1
);

CREATE TABLE [dbo].[m_Item] (
    [ItemId] INT IDENTITY(1,1) PRIMARY KEY,
    [ItemName] NVARCHAR(100) NOT NULL,
    [CategoryId] INT NOT NULL,
    [Unit] NVARCHAR(20) DEFAULT 'Kg', -- Kg, Quintal, Pcs
    [IsActive] BIT DEFAULT 1
);

CREATE TABLE [dbo].[m_BagType] (
    [BagTypeId] INT IDENTITY(1,1) PRIMARY KEY,
    [BagTypeName] NVARCHAR(50) NOT NULL,
    [DeductionWeightGrams] INT NOT NULL,
    [IsActive] BIT DEFAULT 1
);

CREATE TABLE [dbo].[m_Vehicle] (
    [VehicleId] INT IDENTITY(1,1) PRIMARY KEY,
    [VehicleNumber] NVARCHAR(50) NOT NULL,
    [IsActive] BIT DEFAULT 1
);
GO

-- 5. Challan Master Tables (For Outward Entry)
CREATE TABLE [dbo].[m_Challan] (
    [ChallanId] INT IDENTITY(1,1) PRIMARY KEY,
    [ChallanNo] NVARCHAR(50) NOT NULL UNIQUE,
    [DepartmentName] NVARCHAR(100) NOT NULL,
    [GeneratorPersonId] INT NOT NULL,
    [GeneratorPostId] INT NOT NULL,
    [ChallanDate] DATETIME DEFAULT GETDATE(),
    [StatusId] INT DEFAULT 9 -- Outward Pending Approval
);

CREATE TABLE [dbo].[m_ChallanItem] (
    [ChallanItemId] INT IDENTITY(1,1) PRIMARY KEY,
    [ChallanId] INT NOT NULL,
    [ItemId] INT NOT NULL,
    [Quantity] DECIMAL(18,2) NOT NULL,
    [Unit] NVARCHAR(20) NOT NULL
);
GO

-- 6. Gateman Inward / Outward & Serial Tracking Tables
CREATE TABLE [dbo].[t_SerialCounters] (
    [CounterType] NVARCHAR(50) PRIMARY KEY, -- 'GLOBAL', 'INWARD', 'OUTWARD', 'RST'
    [LastNo] INT NOT NULL DEFAULT 0
);
INSERT INTO t_SerialCounters VALUES ('GLOBAL', 0), ('INWARD', 0), ('OUTWARD', 0), ('RST', 0);
GO

CREATE TABLE [dbo].[t_GateInwardOutward] (
    [GateEntryId] INT IDENTITY(1,1) PRIMARY KEY,
    [GlobalSerialNo] INT NOT NULL,
    [EntryType] NVARCHAR(10) NOT NULL, -- 'INWARD', 'OUTWARD'
    [InwardNo] NVARCHAR(50) NULL,
    [OutwardNo] NVARCHAR(50) NULL,
    [VehicleNumber] NVARCHAR(50) NULL,
    [DriverName] NVARCHAR(100) NULL,
    [ChallanNo] NVARCHAR(50) NULL,
    [ItemId] INT NULL,
    [Quantity] DECIMAL(18,2) NULL,
    [Unit] NVARCHAR(20) NULL,
    [DepartmentName] NVARCHAR(100) NULL,
    [GeneratorPostName] NVARCHAR(100) NULL,
    [GeneratorPersonName] NVARCHAR(100) NULL,
    [StatusId] INT NOT NULL, -- Status ID from m_StatusMaster
    [GateManId] INT NOT NULL,
    [CreatedDate] DATETIME DEFAULT GETDATE()
);

-- Weighbridge Gate Entry (RST Table)
CREATE TABLE [dbo].[t_GateEntry] (
    [RSTNumber] NVARCHAR(50) PRIMARY KEY,
    [InwardNo] NVARCHAR(50) NULL, -- Linked to Gateman Inward Entry
    [VehicleId] INT NOT NULL,
    [PartyId] INT NOT NULL,
    [DriverId] INT NOT NULL,
    [GrossWeight] DECIMAL(18,2) NOT NULL,
    [TareWeight] DECIMAL(18,2) NULL,
    [NetWeight] DECIMAL(18,2) NULL,
    [TargetOfficeId] INT NULL,
    [GateEntryTime] DATETIME DEFAULT GETDATE(),
    [GateExitTime] DATETIME NULL,
    [StatusId] INT DEFAULT 1 -- 1: Gadi In, 2: Weight Done, etc.
);
GO

-- 7. Seed Admin Data
INSERT INTO o05_office (OfficeName, Location) VALUES ('Main Mill', 'Headquarters');

INSERT INTO p02_Person (PersonName, MobileNumber, Address, PersonType) 
VALUES ('System Admin', '9999999999', 'Main Office', 'Admin');

DECLARE @AdminPersonId INT = SCOPE_IDENTITY();

INSERT INTO sa05_user (Username, PasswordHash, PersonId, o10_postid, sa10_usertypeid, OfficeId)
VALUES ('admin', '1111', @AdminPersonId, 1, 1, 1);
GO

-- 8. Core Stored Procedures

-- SP: Validate User & Get UserType / Post Layout
CREATE OR ALTER PROCEDURE sp_ValidateUser
    @Username NVARCHAR(100),
    @Password NVARCHAR(256)
AS
BEGIN
    SELECT 
        u.UserId,
        u.Username,
        u.PersonId,
        p.PersonName,
        u.o10_postid,
        u.sa10_usertypeid,
        ut.UserTypeName AS Role,
        ut.LayoutName
    FROM sa05_user u
    INNER JOIN p02_Person p ON u.PersonId = p.PersonId
    INNER JOIN sa10_usertype ut ON u.sa10_usertypeid = ut.UserTypeId
    WHERE u.Username = @Username AND u.PasswordHash = @Password AND u.IsActive = 1 AND p.IsActive = 1;
END
GO

-- SP: Generate Serial Numbers
CREATE OR ALTER PROCEDURE sp_GetNextSerialNumber
    @CounterType NVARCHAR(50),
    @NextNo INT OUTPUT
AS
BEGIN
    BEGIN TRANSACTION;
    UPDATE t_SerialCounters SET LastNo = LastNo + 1 WHERE CounterType = @CounterType;
    SELECT @NextNo = LastNo FROM t_SerialCounters WHERE CounterType = @CounterType;
    COMMIT TRANSACTION;
END
GO

-- SP: Save Gateman Inward Entry
CREATE OR ALTER PROCEDURE sp_CreateGatemanInward
    @VehicleNumber NVARCHAR(50),
    @DriverName NVARCHAR(100),
    @GateManId INT,
    @ItemId INT = NULL,
    @Quantity DECIMAL(18,2) = NULL,
    @Unit NVARCHAR(20) = NULL,
    @InwardNo NVARCHAR(50) OUTPUT
AS
BEGIN
    DECLARE @GlobalNo INT, @InwardSeq INT;
    EXEC sp_GetNextSerialNumber 'GLOBAL', @GlobalNo OUTPUT;
    EXEC sp_GetNextSerialNumber 'INWARD', @InwardSeq OUTPUT;
    
    SET @InwardNo = 'INW-' + CAST(@InwardSeq AS NVARCHAR(20));

    INSERT INTO t_GateInwardOutward (
        GlobalSerialNo, EntryType, InwardNo, VehicleNumber, DriverName, 
        ItemId, Quantity, Unit, StatusId, GateManId
    )
    VALUES (
        @GlobalNo, 'INWARD', @InwardNo, @VehicleNumber, @DriverName, 
        @ItemId, @Quantity, @Unit, 1, @GateManId -- 1: Gadi In
    );

    SELECT @InwardNo AS GeneratedInwardNo;
END
GO

-- SP: Get Challan Details for Outward Gateman Entry
CREATE OR ALTER PROCEDURE sp_GetChallanDetailsForOutward
    @ChallanNo NVARCHAR(50)
AS
BEGIN
    SELECT 
        c.ChallanId,
        c.ChallanNo,
        c.DepartmentName,
        p.PersonName AS GeneratorName,
        post.PostName AS GeneratorPost,
        ci.ItemId,
        i.ItemName,
        ci.Quantity,
        ci.Unit,
        c.StatusId,
        sm.StatusName
    FROM m_Challan c
    INNER JOIN p02_Person p ON c.GeneratorPersonId = p.PersonId
    INNER JOIN o10_post post ON c.GeneratorPostId = post.PostId
    INNER JOIN m_ChallanItem ci ON c.ChallanId = ci.ChallanId
    INNER JOIN m_Item i ON ci.ItemId = i.ItemId
    INNER JOIN m_StatusMaster sm ON c.StatusId = sm.StatusId
    WHERE c.ChallanNo = @ChallanNo;
END
GO

-- SP: Approve Outward Gateman Entry
CREATE OR ALTER PROCEDURE sp_ApproveOutwardChallan
    @ChallanNo NVARCHAR(50),
    @GateManId INT,
    @OutwardNo NVARCHAR(50) OUTPUT
AS
BEGIN
    DECLARE @ChallanId INT, @Dept NVARCHAR(100), @GenPerson NVARCHAR(100), @GenPost NVARCHAR(100);
    DECLARE @ItemId INT, @Qty DECIMAL(18,2), @Unit NVARCHAR(20);
    DECLARE @GlobalNo INT, @OutwardSeq INT;

    SELECT TOP 1 
        @ChallanId = c.ChallanId,
        @Dept = c.DepartmentName,
        @GenPerson = p.PersonName,
        @GenPost = post.PostName,
        @ItemId = ci.ItemId,
        @Qty = ci.Quantity,
        @Unit = ci.Unit
    FROM m_Challan c
    INNER JOIN p02_Person p ON c.GeneratorPersonId = p.PersonId
    INNER JOIN o10_post post ON c.GeneratorPostId = post.PostId
    INNER JOIN m_ChallanItem ci ON c.ChallanId = ci.ChallanId
    WHERE c.ChallanNo = @ChallanNo;

    IF @ChallanId IS NOT NULL
    BEGIN
        EXEC sp_GetNextSerialNumber 'GLOBAL', @GlobalNo OUTPUT;
        EXEC sp_GetNextSerialNumber 'OUTWARD', @OutwardSeq OUTPUT;
        
        SET @OutwardNo = 'OUT-' + CAST(@OutwardSeq AS NVARCHAR(20));

        INSERT INTO t_GateInwardOutward (
            GlobalSerialNo, EntryType, OutwardNo, ChallanNo, ItemId, Quantity, Unit,
            DepartmentName, GeneratorPostName, GeneratorPersonName, StatusId, GateManId
        )
        VALUES (
            @GlobalNo, 'OUTWARD', @OutwardNo, @ChallanNo, @ItemId, @Qty, @Unit,
            @Dept, @GenPost, @GenPerson, 10, @GateManId -- 10: Outward Approved
        );

        UPDATE m_Challan SET StatusId = 10 WHERE ChallanId = @ChallanId;

        SELECT @OutwardNo AS GeneratedOutwardNo;
    END
END
GO

-- SP: Create Employee / User Registration
CREATE OR ALTER PROCEDURE sp_CreateUserLogin
    @PersonId INT,
    @MobileNumber NVARCHAR(20),
    @DesignationId INT,
    @PersonName NVARCHAR(200),
    @OfficeId INT,
    @o10_postid INT
AS
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sa05_user WHERE PersonId = @PersonId)
    BEGIN
        DECLARE @Username NVARCHAR(100) = @MobileNumber;
        IF (@Username IS NULL OR @Username = '')
            SET @Username = REPLACE(@PersonName, ' ', '') + CAST(@PersonId AS NVARCHAR(10));

        -- sa10_usertypeid is ALWAYS SAME AS o10_postid
        INSERT INTO sa05_user (Username, PasswordHash, PersonId, o10_postid, sa10_usertypeid, OfficeId)
        VALUES (@Username, '1111', @PersonId, @o10_postid, @o10_postid, @OfficeId);

        IF @DesignationId > 0
        BEGIN
            INSERT INTO p1_persondesignatation (PersonId, DesignationId)
            VALUES (@PersonId, @DesignationId);
        END
    END
END
GO

-- SP: Get Workers By Meth ID (o_designatationreletation)
CREATE OR ALTER PROCEDURE sp_GetWorkersByMeth
    @MethPersonId INT
AS
BEGIN
    -- Get designation of Meth
    DECLARE @MethDesigId INT;
    SELECT TOP 1 @MethDesigId = DesignationId FROM p1_persondesignatation WHERE PersonId = @MethPersonId AND IsActive = 1;

    SELECT p.PersonId, p.PersonName 
    FROM p02_Person p
    INNER JOIN p1_persondesignatation pd ON p.PersonId = pd.PersonId
    INNER JOIN o_designatationreletation dr ON pd.DesignationId = dr.o12_childid
    WHERE dr.o12_parentid = @MethDesigId AND p.IsActive = 1 AND dr.IsActive = 1;
END
GO

-- SP: Get All Pending Inward Entries for Weighbridge Dropdown
CREATE OR ALTER PROCEDURE sp_GetActiveInwardEntriesForDropdown
AS
BEGIN
    SELECT InwardNo, InwardNo + ' (' + ISNULL(VehicleNumber, 'N/A') + ')' AS DisplayText
    FROM t_GateInwardOutward
    WHERE EntryType = 'INWARD' AND StatusId = 1 -- Gadi In
    ORDER BY GateEntryId DESC;
END
GO
