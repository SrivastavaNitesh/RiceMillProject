USE RiceMillDB;
GO

-- Stored Procedures for o10_post
CREATE OR ALTER PROCEDURE sp_InsertPost
    @PostName NVARCHAR(100)
AS
BEGIN
    INSERT INTO o10_post (PostName) VALUES (@PostName);
    SELECT SCOPE_IDENTITY() AS PostId;
END
GO

CREATE OR ALTER PROCEDURE sp_UpdatePost
    @PostId INT,
    @PostName NVARCHAR(100),
    @IsActive BIT
AS
BEGIN
    UPDATE o10_post
    SET PostName = @PostName, IsActive = @IsActive
    WHERE PostId = @PostId;
END
GO

CREATE OR ALTER PROCEDURE sp_GetAllPosts
AS
BEGIN
    SELECT PostId, PostName, IsActive 
    FROM o10_post 
    WHERE IsActive = 1;
END
GO

-- Stored Procedures for m_ItemCategory
CREATE OR ALTER PROCEDURE sp_InsertItemCategory
    @CategoryName NVARCHAR(100)
AS
BEGIN
    INSERT INTO m_ItemCategory (CategoryName) VALUES (@CategoryName);
    SELECT SCOPE_IDENTITY() AS CategoryId;
END
GO

CREATE OR ALTER PROCEDURE sp_UpdateItemCategory
    @CategoryId INT,
    @CategoryName NVARCHAR(100),
    @IsActive BIT
AS
BEGIN
    UPDATE m_ItemCategory
    SET CategoryName = @CategoryName, IsActive = @IsActive
    WHERE CategoryId = @CategoryId;
END
GO

CREATE OR ALTER PROCEDURE sp_GetAllItemCategories
AS
BEGIN
    SELECT CategoryId, CategoryName, IsActive 
    FROM m_ItemCategory 
    WHERE IsActive = 1;
END
GO

-- Stored Procedures for m_BagType
CREATE OR ALTER PROCEDURE sp_InsertBagType
    @BagTypeName NVARCHAR(50),
    @DeductionWeightGrams INT
AS
BEGIN
    INSERT INTO m_BagType (BagTypeName, DeductionWeightGrams) VALUES (@BagTypeName, @DeductionWeightGrams);
    SELECT SCOPE_IDENTITY() AS BagTypeId;
END
GO

CREATE OR ALTER PROCEDURE sp_UpdateBagType
    @BagTypeId INT,
    @BagTypeName NVARCHAR(50),
    @DeductionWeightGrams INT,
    @IsActive BIT
AS
BEGIN
    UPDATE m_BagType
    SET BagTypeName = @BagTypeName, DeductionWeightGrams = @DeductionWeightGrams, IsActive = @IsActive
    WHERE BagTypeId = @BagTypeId;
END
GO

CREATE OR ALTER PROCEDURE sp_GetAllBagTypes
AS
BEGIN
    SELECT BagTypeId, BagTypeName, DeductionWeightGrams, IsActive 
    FROM m_BagType 
    WHERE IsActive = 1;
END
GO
