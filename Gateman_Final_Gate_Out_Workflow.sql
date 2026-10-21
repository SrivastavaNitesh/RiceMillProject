SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

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
    IF @TareWeight <= 0 THROW 50002, 'Tare weight must be greater than zero.', 1;
    IF @TareWeight >= @PreviousGross THROW 50003, 'Tare weight must be less than gross weight.', 1;

    UPDATE dbo.t_GateEntry
    SET TareWeight = @TareWeight,
        NetWeight = @PreviousGross - @TareWeight,
        GateExitTime = NULL,
        Status = 'WaitingForGateOut',
        IsFinalRST = 1,
        RSTChainStatus = 'Finalized'
    WHERE RSTNumber = @RSTNumber;

    EXEC dbo.sp_MoveRSTProgress @RSTNumber, 13, 'Completed', NULL;
    EXEC dbo.sp_MoveRSTProgress @RSTNumber, 9, 'Completed', NULL;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_CompleteGatemanOutward
    @RSTNumber NVARCHAR(100),
    @GateManUserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF ISNULL(@GateManUserId, 0) <= 0 THROW 50001, 'Gateman user is required.', 1;
    UPDATE g
    SET Status = 'Exited',
        GateExitTime = GETDATE(),
        RSTChainStatus = 'Finalized'
    FROM dbo.t_GateEntry g
    WHERE g.RSTNumber = @RSTNumber
      AND g.Status = 'WaitingForGateOut'
      AND g.TareWeight IS NOT NULL
      AND g.NetWeight IS NOT NULL
      AND ISNULL(g.IsFinalRST, 1) = 1
      AND EXISTS
      (
          SELECT 1
          FROM dbo.t_InwardHeader h
          WHERE h.InwardNo = g.InwardNo
            AND h.CreatedBy = @GateManUserId
      );

    IF @@ROWCOUNT = 0
        THROW 50001, 'Vehicle is not ready for outward, belongs to another Gateman, or outward is already completed.', 1;
END;
GO
