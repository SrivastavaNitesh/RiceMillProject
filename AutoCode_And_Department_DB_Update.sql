USE RiceMillDB;
GO

-- 1. Ensure EmployeeCode Column in P11_PersonDesignatation & p1_persondesignatation
IF COL_LENGTH('dbo.P11_PersonDesignatation', 'EmployeeCode') IS NULL
BEGIN
    ALTER TABLE dbo.P11_PersonDesignatation ADD EmployeeCode NVARCHAR(50) NULL;
END
GO

IF COL_LENGTH('dbo.p1_persondesignatation', 'EmployeeCode') IS NULL
BEGIN
    ALTER TABLE dbo.p1_persondesignatation ADD EmployeeCode NVARCHAR(50) NULL;
END
GO

-- 2. Add Code Columns to Master Tables if they don't exist
IF COL_LENGTH('dbo.o05_office', 'OfficeCode') IS NULL
BEGIN
    ALTER TABLE dbo.o05_office ADD OfficeCode NVARCHAR(50) NULL;
END
GO

IF COL_LENGTH('dbo.o05_office', 'DhermkataCode') IS NULL
BEGIN
    ALTER TABLE dbo.o05_office ADD DhermkataCode NVARCHAR(50) NULL;
END
GO

IF COL_LENGTH('dbo.o12_designatation', 'mDepartmentId') IS NULL
BEGIN
    ALTER TABLE dbo.o12_designatation ADD mDepartmentId INT NULL;
END
GO

IF COL_LENGTH('dbo.p02_Person', 'DepartmentId') IS NULL
BEGIN
    ALTER TABLE dbo.p02_Person ADD DepartmentId INT NULL;
END
GO

IF COL_LENGTH('dbo.sa05_user', 'DepartmentId') IS NULL
BEGIN
    ALTER TABLE dbo.sa05_user ADD DepartmentId INT NULL;
END
GO

-- Update existing records with default Codes if empty
UPDATE o05_office SET OfficeCode = 'OFF-' + CAST(OfficeId AS NVARCHAR(10)) WHERE OfficeCode IS NULL AND (DhermkataCode IS NULL OR DhermkataCode = '');
UPDATE P11_PersonDesignatation SET EmployeeCode = 'EMP-' + CAST(P02_PersonId AS NVARCHAR(10)) WHERE EmployeeCode IS NULL;
UPDATE p1_persondesignatation SET EmployeeCode = 'EMP-' + CAST(PersonId AS NVARCHAR(10)) WHERE EmployeeCode IS NULL;
GO

-- 3. Update SP for Office Creation to generate OfficeCode & DhermkataCode
CREATE OR ALTER PROCEDURE sp_InsertOffice
    @OfficeName NVARCHAR(100),
    @Location NVARCHAR(200) = NULL,
    @officetype INT = 1
AS
BEGIN
    DECLARE @GlobalNo INT, @GeneratedCode NVARCHAR(50);
    EXEC sp_GetNextSerialNumber 'GLOBAL', @GlobalNo OUTPUT;

    IF (@officetype = 3) -- Dharamkata / Location
    BEGIN
        SET @GeneratedCode = 'DK-' + CAST(@GlobalNo AS NVARCHAR(20));
        INSERT INTO o05_office (OfficeName, Location, DhermkataCode, IsActive)
        VALUES (@OfficeName, @Location, @GeneratedCode, 1);
    END
    ELSE
    BEGIN
        SET @GeneratedCode = 'OFF-' + CAST(@GlobalNo AS NVARCHAR(20));
        INSERT INTO o05_office (OfficeName, Location, OfficeCode, IsActive)
        VALUES (@OfficeName, @Location, @GeneratedCode, 1);
    END

    SELECT SCOPE_IDENTITY() AS OfficeId, @GeneratedCode AS GeneratedCode;
END
GO

-- 4. Update SP for Person & Employee Details Registration
CREATE OR ALTER PROCEDURE sp_SavePersondetails
    @PersonName NVARCHAR(200),
    @MobileNumber NVARCHAR(20),
    @Address NVARCHAR(500),
    @PersonType NVARCHAR(50), -- PostId or Role Name
    @O05_officeId INT = NULL,
    @o10postid INT = NULL,
    @o12_parentid INT = NULL, -- Meth PersonId (when registering Worker)
    @PartyCategory INT = 1,
    @FirmName NVARCHAR(200) = NULL,
    @FirmDesignation NVARCHAR(100) = NULL,
    @DepartmentId INT = NULL,
    @designatationId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @PersonId INT = 0;
    DECLARE @CalculatedDesignationId INT = 0;
    DECLARE @EmpCode NVARCHAR(50);

    -- Generate Auto Backend Employee Code
    DECLARE @GlobalNo INT;
    EXEC sp_GetNextSerialNumber 'GLOBAL', @GlobalNo OUTPUT;
    SET @EmpCode = 'EMP-' + CAST(@GlobalNo AS NVARCHAR(20));

    -- 1. Insert into p02_Person (Always)
    INSERT INTO p02_Person (
        PersonName, MobileNumber, Address, PersonType, 
        PartyCategory, FirmName, FirmDesignation, DepartmentId, IsActive
    )
    VALUES (
        @PersonName, @MobileNumber, @Address, @PersonType, 
        @PartyCategory, @FirmName, @FirmDesignation, @DepartmentId, 1
    );

    SET @PersonId = SCOPE_IDENTITY();

    -- Check if Designation exists in O12_Designatation matching PersonType/Post Name, or insert it
    DECLARE @PostName NVARCHAR(100);
    SELECT TOP 1 @PostName = PostName FROM o10_post WHERE PostId = TRY_CAST(@PersonType AS INT);
    IF @PostName IS NULL SET @PostName = @PersonType;

    SELECT TOP 1 @CalculatedDesignationId = DesignationId 
    FROM o12_designatation 
    WHERE DesignationName = @PostName AND IsActive = 1;

    IF (@CalculatedDesignationId = 0 OR @CalculatedDesignationId IS NULL)
    BEGIN
        INSERT INTO o12_designatation (DesignationName, mDepartmentId, IsActive)
        VALUES (@PostName, @DepartmentId, 1);
        SET @CalculatedDesignationId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE o12_designatation SET mDepartmentId = @DepartmentId WHERE DesignationId = @CalculatedDesignationId;
    END

    SET @designatationId = @CalculatedDesignationId;

    -- 2. Insert into P11_PersonDesignatation & p1_persondesignatation with Backend EmployeeCode
    IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'P11_PersonDesignatation')
    BEGIN
        INSERT INTO P11_PersonDesignatation (P02_PersonId, O12_DesignationId, EmployeeCode, IsActive)
        VALUES (@PersonId, @CalculatedDesignationId, @EmpCode, 1);
    END

    IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'p1_persondesignatation')
    BEGIN
        INSERT INTO p1_persondesignatation (PersonId, DesignationId, EmployeeCode, IsActive)
        VALUES (@PersonId, @CalculatedDesignationId, @EmpCode, 1);
    END

    -- 3. Insert into o_designatationreletation (ONLY when Worker is registered under a Meth)
    IF (@o12_parentid IS NOT NULL AND @o12_parentid > 0)
    BEGIN
        DECLARE @MethDesignationId INT = 0;
        SELECT TOP 1 @MethDesignationId = DesignationId 
        FROM p1_persondesignatation 
        WHERE PersonId = @o12_parentid AND IsActive = 1;

        IF (@MethDesignationId > 0)
        BEGIN
            INSERT INTO o_designatationreletation (
                o12_parentid, o12_childid, parent_postid, child_postid, IsActive
            )
            VALUES (
                @MethDesignationId, @CalculatedDesignationId, NULL, TRY_CAST(@PersonType AS INT), 1
            );
        END
    END

    -- Return New PersonId
    SELECT @PersonId AS PersonId;
END
GO

-- 5. Update SP to Create User Login (Including DepartmentId)
CREATE OR ALTER PROCEDURE sp_CreateUserLogin
    @PersonId INT,
    @MobileNumber NVARCHAR(20),
    @DesignationId INT,
    @PersonName NVARCHAR(200),
    @OfficeId INT,
    @o10_postid INT,
    @DepartmentId INT = NULL
AS
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sa05_user WHERE PersonId = @PersonId)
    BEGIN
        DECLARE @Username NVARCHAR(100) = @MobileNumber;
        IF (@Username IS NULL OR @Username = '')
            SET @Username = REPLACE(@PersonName, ' ', '') + CAST(@PersonId AS NVARCHAR(10));

        -- sa10_usertypeid is ALWAYS SAME AS o10_postid
        INSERT INTO sa05_user (Username, PasswordHash, PersonId, o10_postid, sa10_usertypeid, OfficeId, DepartmentId)
        VALUES (@Username, '1111', @PersonId, @o10_postid, @o10_postid, @OfficeId, @DepartmentId);

        DECLARE @EmpCode NVARCHAR(50) = 'EMP-' + CAST(@PersonId AS NVARCHAR(10));

        IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'P11_PersonDesignatation')
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM P11_PersonDesignatation WHERE P02_PersonId = @PersonId)
            BEGIN
                INSERT INTO P11_PersonDesignatation (P02_PersonId, O12_DesignationId, EmployeeCode)
                VALUES (@PersonId, @DesignationId, @EmpCode);
            END
        END

        IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'p1_persondesignatation')
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM p1_persondesignatation WHERE PersonId = @PersonId)
            BEGIN
                INSERT INTO p1_persondesignatation (PersonId, DesignationId, EmployeeCode)
                VALUES (@PersonId, @DesignationId, @EmpCode);
            END
        END
    END
END
GO

-- 6. Create Logins for PDF Module Roles if they don't exist
-- Gate Security / Gate Man (PostId: 2)
IF NOT EXISTS (SELECT 1 FROM p02_Person WHERE PersonName = 'Gate Security Officer')
BEGIN
    INSERT INTO p02_Person (PersonName, MobileNumber, Address, PersonType, DepartmentId, IsActive)
    VALUES ('Gate Security Officer', '8888888888', 'Main Gate', '2', 1, 1);
    DECLARE @PId2 INT = SCOPE_IDENTITY();
    EXEC sp_CreateUserLogin @PId2, '8888888888', 2, 'Gate Security Officer', 1, 2, 1;
END

-- Weighbridge Operator (PostId: 3)
IF NOT EXISTS (SELECT 1 FROM p02_Person WHERE PersonName = 'Weighbridge Operator')
BEGIN
    INSERT INTO p02_Person (PersonName, MobileNumber, Address, PersonType, DepartmentId, IsActive)
    VALUES ('Weighbridge Operator', '7777777777', 'Dharamkata 1', '3', 1, 1);
    DECLARE @PId3 INT = SCOPE_IDENTITY();
    EXEC sp_CreateUserLogin @PId3, '7777777777', 3, 'Weighbridge Operator', 1, 3, 1;
END

-- Unloading Supervisor (PostId: 4)
IF NOT EXISTS (SELECT 1 FROM p02_Person WHERE PersonName = 'Unloading Supervisor')
BEGIN
    INSERT INTO p02_Person (PersonName, MobileNumber, Address, PersonType, DepartmentId, IsActive)
    VALUES ('Unloading Supervisor', '6666666666', 'Yard Stores', '4', 3, 1);
    DECLARE @PId4 INT = SCOPE_IDENTITY();
    EXEC sp_CreateUserLogin @PId4, '6666666666', 4, 'Unloading Supervisor', 1, 4, 3;
END

-- Lab Quality Technician (PostId: 5)
IF NOT EXISTS (SELECT 1 FROM p02_Person WHERE PersonName = 'Lab Technician')
BEGIN
    INSERT INTO p02_Person (PersonName, MobileNumber, Address, PersonType, DepartmentId, IsActive)
    VALUES ('Lab Technician', '5555555555', 'QA Lab', '5', 7, 1);
    DECLARE @PId5 INT = SCOPE_IDENTITY();
    EXEC sp_CreateUserLogin @PId5, '5555555555', 5, 'Lab Technician', 1, 5, 7;
END
GO
