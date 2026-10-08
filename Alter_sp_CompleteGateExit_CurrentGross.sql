CREATE OR ALTER PROCEDURE dbo.sp_CompleteGateExit
    @RSTNumber NVARCHAR(50),
    @TareWeight DECIMAL(18,2)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @PreviousGross DECIMAL(18,2);
    SELECT @PreviousGross = GrossWeight
    FROM dbo.t_GateEntry
    WHERE RSTNumber = @RSTNumber;

    IF @PreviousGross IS NULL THROW 50001, 'RST was not found.', 1;
    IF @TareWeight <= 0 THROW 50002, 'Current gross weight must be greater than zero.', 1;
    IF @TareWeight >= @PreviousGross THROW 50003, 'Current gross weight must be less than previous gross weight.', 1;

    UPDATE dbo.t_GateEntry
    SET TareWeight = @TareWeight,
        NetWeight = @PreviousGross - @TareWeight,
        GateExitTime = GETDATE(),
        Status = 'Exited',
        IsFinalRST = 1,
        RSTChainStatus = 'Finalized'
    WHERE RSTNumber = @RSTNumber;
END;
GO
