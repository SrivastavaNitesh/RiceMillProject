IF COL_LENGTH('dbo.t_GateEntry','CategoryId') IS NULL
    ALTER TABLE dbo.t_GateEntry ADD CategoryId int NULL;
GO
