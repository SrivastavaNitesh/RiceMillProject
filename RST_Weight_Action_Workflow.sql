SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
-- Keep Lab RST routing limited to final RSTs when this migration is rerun.
IF OBJECT_ID('dbo.sp_LabRstList','P') IS NOT NULL
BEGIN
    DECLARE @LabDefinition nvarchar(max)=OBJECT_DEFINITION(OBJECT_ID('dbo.sp_LabRstList'));
    IF @LabDefinition IS NOT NULL AND @LabDefinition NOT LIKE '%IsFinalRST%'
    BEGIN
        SET @LabDefinition=REPLACE(@LabDefinition,CHAR(13)+CHAR(10)+'    ORDER BY',CHAR(13)+CHAR(10)+'    AND ISNULL(G.IsFinalRST,1)=1'+CHAR(13)+CHAR(10)+CHAR(13)+CHAR(10)+'    ORDER BY');
        IF @LabDefinition NOT LIKE 'CREATE OR ALTER%'
            SET @LabDefinition=STUFF(@LabDefinition,1,CHARINDEX('PROCEDURE',@LabDefinition)-1,'CREATE OR ALTER ');
        EXEC sys.sp_executesql @LabDefinition;
    END
END;
GO
IF COL_LENGTH('dbo.t_GateEntry','ParentRSTNumber') IS NULL ALTER TABLE dbo.t_GateEntry ADD ParentRSTNumber nvarchar(50) NULL;
IF COL_LENGTH('dbo.t_GateEntry','IsFinalRST') IS NULL ALTER TABLE dbo.t_GateEntry ADD IsFinalRST bit NOT NULL CONSTRAINT DF_GateEntry_IsFinalRST DEFAULT(1);
IF COL_LENGTH('dbo.t_GateEntry','RSTChainStatus') IS NULL ALTER TABLE dbo.t_GateEntry ADD RSTChainStatus varchar(20) NOT NULL CONSTRAINT DF_GateEntry_RSTChainStatus DEFAULT('Active');
GO
-- Synchronize Lab item lookup/save with the final-RST routing rule.
IF OBJECT_ID('dbo.sp_LabItemOptions','P') IS NOT NULL
BEGIN
    DECLARE @ItemOptionsDefinition nvarchar(max)=OBJECT_DEFINITION(OBJECT_ID('dbo.sp_LabItemOptions'));
    IF @ItemOptionsDefinition IS NOT NULL AND @ItemOptionsDefinition NOT LIKE '%IsFinalRST%'
    BEGIN
        SET @ItemOptionsDefinition=REPLACE(@ItemOptionsDefinition,
            'U.RSTNumber = @RSTNumber',
            'U.RSTNumber = @RSTNumber AND EXISTS (SELECT 1 FROM dbo.t_GateEntry g WHERE g.RSTNumber=U.RSTNumber AND ISNULL(g.IsFinalRST,1)=1)');
        IF @ItemOptionsDefinition NOT LIKE 'CREATE OR ALTER%'
            SET @ItemOptionsDefinition=STUFF(@ItemOptionsDefinition,1,CHARINDEX('PROCEDURE',@ItemOptionsDefinition)-1,'CREATE OR ALTER ');
        EXEC sys.sp_executesql @ItemOptionsDefinition;
    END
END;
IF OBJECT_ID('dbo.sp_LabReportSave','P') IS NOT NULL
BEGIN
    DECLARE @LabSaveDefinition nvarchar(max)=OBJECT_DEFINITION(OBJECT_ID('dbo.sp_LabReportSave'));
    IF @LabSaveDefinition IS NOT NULL AND @LabSaveDefinition NOT LIKE '%IsFinalRST%'
    BEGIN
        SET @LabSaveDefinition=REPLACE(@LabSaveDefinition,
            'WHERE RSTNumber=@RSTNumber) THROW 50001,''RST number does not exist.''',
            'WHERE RSTNumber=@RSTNumber AND ISNULL(IsFinalRST,1)=1) THROW 50001,''Only the final RST can be sent for lab testing.''');
        SET @LabSaveDefinition=REPLACE(@LabSaveDefinition,
            'u.RSTNumber=@RSTNumber',
            'u.RSTNumber=@RSTNumber AND EXISTS (SELECT 1 FROM dbo.t_GateEntry g WHERE g.RSTNumber=u.RSTNumber AND ISNULL(g.IsFinalRST,1)=1)');
        IF @LabSaveDefinition NOT LIKE 'CREATE OR ALTER%'
            SET @LabSaveDefinition=STUFF(@LabSaveDefinition,1,CHARINDEX('PROCEDURE',@LabSaveDefinition)-1,'CREATE OR ALTER ');
        EXEC sys.sp_executesql @LabSaveDefinition;
    END
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_ContinueWeightmanRst
    @RSTNumber nvarchar(50), @CurrentGrossWeight decimal(18,3), @CreatedBy int
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @CurrentGrossWeight<=0 THROW 50001,'Current gross weight must be greater than zero.',1;
    BEGIN TRANSACTION;
    DECLARE @InwardNo nvarchar(100),@VehicleId int,@PartyId int,@DriverId int,@ItemId int,@OfficeId int,@Charge decimal(18,2),@PreviousGross decimal(18,3),@NewRST nvarchar(50),@NextNo int;
    SELECT @InwardNo=InwardNo,@VehicleId=VehicleId,@PartyId=PartyId,@DriverId=DriverId,@ItemId=ItemId,@OfficeId=TargetOfficeId,@Charge=WeighmentCharge,@PreviousGross=GrossWeight FROM dbo.t_GateEntry WITH (UPDLOCK,HOLDLOCK) WHERE RSTNumber=@RSTNumber;
    IF @InwardNo IS NULL THROW 50001,'RST was not found.',1;
    IF @CurrentGrossWeight<=@PreviousGross THROW 50001,'Current gross weight must exceed previous gross weight.',1;
    SELECT @NextNo=ISNULL(MAX(TRY_CONVERT(int,RIGHT(RSTNumber,CHARINDEX('-',REVERSE(RSTNumber))-1))),0)+1 FROM dbo.t_GateEntry WITH (UPDLOCK,HOLDLOCK) WHERE RSTNumber LIKE 'RST-'+CONVERT(char(4),YEAR(GETDATE()))+'-%';
    SET @NewRST='RST-'+CONVERT(char(4),YEAR(GETDATE()))+'-'+RIGHT('000000'+CONVERT(varchar(12),@NextNo),6);
    INSERT dbo.t_GateEntry(RSTNumber,InwardNo,VehicleId,PartyId,DriverId,ItemId,GrossWeight,TargetOfficeId,GateEntryTime,StatusId,Status,CreatedBy,WeighmentCharge,InwardOutward,ParentRSTNumber,IsFinalRST,RSTChainStatus)
    VALUES(@NewRST,@InwardNo,@VehicleId,@PartyId,@DriverId,@ItemId,@CurrentGrossWeight,@OfficeId,SYSDATETIME(),1,'WaitingForSupervisor',@CreatedBy,@Charge,'Inward',@RSTNumber,1,'Active');
    UPDATE dbo.t_GateEntry SET IsFinalRST=0,RSTChainStatus='Continued',Status='Continued' WHERE RSTNumber=@RSTNumber;
    INSERT dbo.t_RSTWeightActionHistory(RSTNumber,ActionType,PreviousGrossWeight,CurrentGrossWeight,ReceivedWeight,NewRSTNumber,CreatedBy) VALUES(@RSTNumber,'CONTINUE',@PreviousGross,@CurrentGrossWeight,@CurrentGrossWeight-@PreviousGross,@NewRST,@CreatedBy);
    COMMIT; SELECT @NewRST AS NewRSTNumber,@CurrentGrossWeight-@PreviousGross AS ReceivedWeight;
END;
GO
IF OBJECT_ID('dbo.t_RSTWeightActionHistory','U') IS NULL
BEGIN
    CREATE TABLE dbo.t_RSTWeightActionHistory
    (
        ActionId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_RSTWeightActionHistory PRIMARY KEY,
        RSTNumber nvarchar(50) NOT NULL,
        ActionType varchar(20) NOT NULL CONSTRAINT CK_RSTWeightActionType CHECK(ActionType IN ('CONTINUE','END')),
        PreviousGrossWeight decimal(18,3) NULL,
        CurrentGrossWeight decimal(18,3) NOT NULL,
        ReceivedWeight decimal(18,3) NOT NULL,
        NewRSTNumber nvarchar(50) NULL,
        CreatedBy int NULL,
        CreatedAt datetime2 NOT NULL CONSTRAINT DF_RSTWeightActionCreatedAt DEFAULT sysdatetime()
    );
    CREATE INDEX IX_RSTWeightActionHistory_RST ON dbo.t_RSTWeightActionHistory(RSTNumber, CreatedAt DESC);
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_RecordWeightmanRstAction
    @RSTNumber nvarchar(50),
    @ActionType varchar(20),
    @CurrentGrossWeight decimal(18,3),
    @NewRSTNumber nvarchar(50)=NULL,
    @CreatedBy int=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF NULLIF(LTRIM(RTRIM(@RSTNumber)), '') IS NULL THROW 50001,'RST number is required.',1;
    IF @ActionType NOT IN ('CONTINUE','END') THROW 50001,'Invalid Weightman action.',1;
    IF ISNULL(@CurrentGrossWeight,0)<=0 THROW 50001,'Current gross weight must be greater than zero.',1;
    DECLARE @PreviousGross decimal(18,3), @Received decimal(18,3);
    SELECT @PreviousGross=GrossWeight FROM dbo.t_GateEntry WHERE RSTNumber=@RSTNumber;
    IF @PreviousGross IS NULL THROW 50001,'RST was not found.',1;
    SET @Received=@CurrentGrossWeight-@PreviousGross;
    IF @Received<=0 THROW 50001,'Current gross weight must be greater than previous gross weight.',1;
    INSERT dbo.t_RSTWeightActionHistory(RSTNumber,ActionType,PreviousGrossWeight,CurrentGrossWeight,ReceivedWeight,NewRSTNumber,CreatedBy)
    VALUES(@RSTNumber,@ActionType,@PreviousGross,@CurrentGrossWeight,@Received,@NewRSTNumber,@CreatedBy);
    IF @ActionType='END'
    BEGIN
        UPDATE dbo.t_GateEntry
        SET GrossWeight=@CurrentGrossWeight, NetWeight=@Received, IsFinalRST=1,
            RSTChainStatus='Finalized', Status='WaitingForTare'
        WHERE RSTNumber=@RSTNumber;
    END;
    SELECT CAST(SCOPE_IDENTITY() AS bigint) ActionId,@Received ReceivedWeight;
END;
GO
