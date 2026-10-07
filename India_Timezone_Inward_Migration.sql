SET NOCOUNT ON;
-- Store Gateman inward timestamps in India Standard Time (UTC+05:30),
-- independent of the hosting SQL Server timezone.
IF OBJECT_ID('dbo.sp_ManageGateInwardEntry','P') IS NOT NULL
BEGIN
    DECLARE @d nvarchar(max)=OBJECT_DEFINITION(OBJECT_ID('dbo.sp_ManageGateInwardEntry'));
    IF @d IS NOT NULL AND @d NOT LIKE '%SWITCHOFFSET%'
    BEGIN
        SET @d=REPLACE(@d,'GETDATE()','CONVERT(datetime,SWITCHOFFSET(SYSDATETIMEOFFSET(),''+05:30''))');
        SET @d=STUFF(@d,1,CHARINDEX('PROCEDURE',@d)-1,'CREATE OR ALTER ');
        EXEC sys.sp_executesql @d;
    END
END;
GO
