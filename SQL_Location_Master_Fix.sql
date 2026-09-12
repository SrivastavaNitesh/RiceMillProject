USE [RiceMillDB];
GO

-- ============================================================================
-- STORED PROCEDURE: MANAGE OFFICE LOCATION MAPPING
-- ============================================================================
CREATE OR ALTER PROCEDURE sp_ManageOfficeLocationMapping
    @Action VARCHAR(20),
    @MappingId INT = NULL,
    @OfficeId INT = NULL,
    @LocationId INT = NULL,
    @UnloadingAllowed BIT = 1,
    @EffectiveFrom DATE = NULL,
    @IsActive BIT = 1,
    @CreatedBy INT = 1
AS
BEGIN
    SET NOCOUNT ON;

    IF @Action = 'INSERT'
    BEGIN
        IF EXISTS (SELECT 1 FROM t_OfficeLocationMapping WHERE OfficeId = @OfficeId AND LocationId = @LocationId)
        BEGIN
            UPDATE t_OfficeLocationMapping
            SET UnloadingAllowed = @UnloadingAllowed,
                EffectiveFrom = ISNULL(@EffectiveFrom, GETDATE()),
                IsActive = ISNULL(@IsActive, 1),
                ModifiedBy = @CreatedBy,
                ModifiedDate = GETDATE()
            WHERE OfficeId = @OfficeId AND LocationId = @LocationId;
        END
        ELSE
        BEGIN
            INSERT INTO t_OfficeLocationMapping (
                OfficeId, LocationId, UnloadingAllowed, EffectiveFrom, IsActive, CreatedBy, CreatedDate
            )
            VALUES (
                @OfficeId, @LocationId, @UnloadingAllowed, ISNULL(@EffectiveFrom, GETDATE()), ISNULL(@IsActive, 1), @CreatedBy, GETDATE()
            );
        END
    END
    ELSE IF @Action = 'UPDATE'
    BEGIN
        UPDATE t_OfficeLocationMapping
        SET OfficeId = @OfficeId,
            LocationId = @LocationId,
            UnloadingAllowed = @UnloadingAllowed,
            EffectiveFrom = @EffectiveFrom,
            IsActive = @IsActive,
            ModifiedBy = @CreatedBy,
            ModifiedDate = GETDATE()
        WHERE MappingId = @MappingId;
    END
    ELSE IF @Action = 'SELECT_ALL'
    BEGIN
        SELECT 
            m.MappingId,
            m.OfficeId,
            o.PostName AS OfficeName,
            m.LocationId,
            l.LocationName,
            l.LocationCode,
            m.UnloadingAllowed,
            m.EffectiveFrom,
            m.IsActive
        FROM t_OfficeLocationMapping m
        INNER JOIN o10_post o ON m.OfficeId = o.PostId
        INNER JOIN m_LocationMaster l ON m.LocationId = l.LocationId
        ORDER BY m.MappingId DESC;
    END
    ELSE IF @Action = 'DELETE'
    BEGIN
        DELETE FROM t_OfficeLocationMapping WHERE MappingId = @MappingId;
    END
END
GO
