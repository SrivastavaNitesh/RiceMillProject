USE RiceMillDB;
GO

-- 1. Update Admin Password
UPDATE sa05_user SET PasswordHash = '1111' WHERE Username = 'admin';
GO

-- 2. Stored Procedure for User Validation
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
        p.PersonType AS Role
    FROM sa05_user u
    INNER JOIN p02_Person p ON u.PersonId = p.PersonId
    WHERE u.Username = @Username AND u.PasswordHash = @Password AND u.IsActive = 1 AND p.IsActive = 1;
END
GO
