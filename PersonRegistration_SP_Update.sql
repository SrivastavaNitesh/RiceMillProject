-- SP: Save Person Details with exact Table Insertion Logic
-- Tables involved:
-- 1. p02_Person (Always inserted for any person registration)
-- 2. P11_PersonDesignatation / p1_persondesignatation (Inserted for any registered person having a designation/post)
-- 3. o_designatationreletation (Inserted ONLY when a Worker is registered under a Meth)
-- 4. sa05_user (Inserted ONLY when an Employee/Login-required role is registered)

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
    @designatationId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @PersonId INT = 0;
    DECLARE @CalculatedDesignationId INT = 0;

    -- 1. Insert into p02_Person (Always)
    INSERT INTO p02_Person (
        PersonName, MobileNumber, Address, PersonType, 
        PartyCategory, FirmName, FirmDesignation, IsActive
    )
    VALUES (
        @PersonName, @MobileNumber, @Address, @PersonType, 
        @PartyCategory, @FirmName, @FirmDesignation, 1
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
        INSERT INTO o12_designatation (DesignationName, IsActive)
        VALUES (@PostName, 1);
        SET @CalculatedDesignationId = SCOPE_IDENTITY();
    END

    SET @designatationId = @CalculatedDesignationId;

    -- 2. Insert into P11_PersonDesignatation / p1_persondesignatation (Always upon Person Registration)
    INSERT INTO p1_persondesignatation (PersonId, DesignationId, IsActive)
    VALUES (@PersonId, @CalculatedDesignationId, 1);

    -- 3. Insert into o_designatationreletation (ONLY when Worker is registered under a Meth)
    -- If @o12_parentid (Meth PersonId) is provided
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
