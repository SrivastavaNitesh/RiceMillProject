USE RiceMillDB;
GO

-- Stored Procedures for m_Vehicle
CREATE OR ALTER PROCEDURE sp_InsertVehicle
    @VehicleNumber NVARCHAR(50)
AS
BEGIN
    INSERT INTO m_Vehicle (VehicleNumber) VALUES (@VehicleNumber);
    SELECT SCOPE_IDENTITY() AS VehicleId;
END
GO

CREATE OR ALTER PROCEDURE sp_UpdateVehicle
    @VehicleId INT,
    @VehicleNumber NVARCHAR(50),
    @IsActive BIT
AS
BEGIN
    UPDATE m_Vehicle
    SET VehicleNumber = @VehicleNumber, IsActive = @IsActive
    WHERE VehicleId = @VehicleId;
END
GO

CREATE OR ALTER PROCEDURE sp_GetAllVehicles
AS
BEGIN
    SELECT VehicleId, VehicleNumber, IsActive 
    FROM m_Vehicle 
    WHERE IsActive = 1;
END
GO

-- Stored Procedures for m_Item
CREATE OR ALTER PROCEDURE sp_InsertItem
    @ItemName NVARCHAR(100),
    @CategoryId INT
AS
BEGIN
    INSERT INTO m_Item (ItemName, CategoryId) VALUES (@ItemName, @CategoryId);
    SELECT SCOPE_IDENTITY() AS ItemId;
END
GO

CREATE OR ALTER PROCEDURE sp_UpdateItem
    @ItemId INT,
    @ItemName NVARCHAR(100),
    @CategoryId INT,
    @IsActive BIT
AS
BEGIN
    UPDATE m_Item
    SET ItemName = @ItemName, CategoryId = @CategoryId, IsActive = @IsActive
    WHERE ItemId = @ItemId;
END
GO

CREATE OR ALTER PROCEDURE sp_GetAllItems
AS
BEGIN
    SELECT i.ItemId, i.ItemName, i.CategoryId, c.CategoryName, i.IsActive 
    FROM m_Item i
    INNER JOIN m_ItemCategory c ON i.CategoryId = c.CategoryId
    WHERE i.IsActive = 1;
END
GO

-- Stored Procedures for o12_designatation
CREATE OR ALTER PROCEDURE sp_InsertDesignation
    @DesignationName NVARCHAR(100)
AS
BEGIN
    INSERT INTO o12_designatation (DesignationName) VALUES (@DesignationName);
    SELECT SCOPE_IDENTITY() AS DesignationId;
END
GO

CREATE OR ALTER PROCEDURE sp_UpdateDesignation
    @DesignationId INT,
    @DesignationName NVARCHAR(100),
    @IsActive BIT
AS
BEGIN
    UPDATE o12_designatation
    SET DesignationName = @DesignationName, IsActive = @IsActive
    WHERE DesignationId = @DesignationId;
END
GO

CREATE OR ALTER PROCEDURE sp_GetAllDesignations
AS
BEGIN
    SELECT DesignationId, DesignationName, IsActive 
    FROM o12_designatation 
    WHERE IsActive = 1;
END
GO
