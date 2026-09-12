USE RiceMillDB;
GO

-- Disable Foreign Keys
EXEC sp_MSforeachtable "ALTER TABLE ? NOCHECK CONSTRAINT ALL";
GO

-- Truncate Person Registration Tables
DELETE FROM dbo.sa05_user;
DELETE FROM dbo.p1_persondesignatation;
DELETE FROM dbo.o_designatationreletation;
DELETE FROM dbo.p02_Person;
DELETE FROM dbo.o12_designatation;

DBCC CHECKIDENT ('dbo.sa05_user', RESEED, 0);
DBCC CHECKIDENT ('dbo.p1_persondesignatation', RESEED, 0);
DBCC CHECKIDENT ('dbo.o_designatationreletation', RESEED, 0);
DBCC CHECKIDENT ('dbo.p02_Person', RESEED, 0);
DBCC CHECKIDENT ('dbo.o12_designatation', RESEED, 0);
GO

-- Re-enable Foreign Keys
EXEC sp_MSforeachtable "ALTER TABLE ? CHECK CONSTRAINT ALL";
GO

-- 1. Insert Admin Record into p02_Person
INSERT INTO p02_Person (PersonName, MobileNumber, Address, PersonType, PartyCategory, IsActive)
VALUES ('System Admin', '9999999999', 'Main Office', '1', 1, 1);

DECLARE @AdminPersonId INT = SCOPE_IDENTITY();

-- 2. Insert Designation for Admin into O12_Designatation
INSERT INTO o12_Designatation (DesignationName, IsActive)
VALUES ('Admin', 1);

DECLARE @AdminDesignationId INT = SCOPE_IDENTITY();

-- 3. Link Admin in P11_PersonDesignatation (p1_persondesignatation)
INSERT INTO p1_persondesignatation (PersonId, DesignationId, IsActive)
VALUES (@AdminPersonId, @AdminDesignationId, 1);

-- 4. Create Login for Admin in sa05_user (PostId = 1, UserTypeId = 1)
INSERT INTO sa05_user (Username, PasswordHash, PersonId, o10_postid, sa10_usertypeid, OfficeId, IsActive)
VALUES ('admin', '1111', @AdminPersonId, 1, 1, 1, 1);
GO

SELECT 'p02_Person' AS TableName, COUNT(*) AS RecordCount FROM p02_Person
UNION ALL
SELECT 'P11_PersonDesignatation', COUNT(*) FROM p1_persondesignatation
UNION ALL
SELECT 'O12_Designatation', COUNT(*) FROM o12_Designatation
UNION ALL
SELECT 'o_designatationreletation', COUNT(*) FROM o_designatationreletation
UNION ALL
SELECT 'sa05_user', COUNT(*) FROM sa05_user;
GO
