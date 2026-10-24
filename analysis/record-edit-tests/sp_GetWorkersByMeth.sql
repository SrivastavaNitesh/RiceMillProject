

/* =========================================================
   2. METH KE UNDER KE WORKERS
   ========================================================= */

CREATE   PROCEDURE dbo.sp_GetWorkersByMeth
    @MethPersonId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT DISTINCT
        p.PersonId,
        p.PersonName
    FROM dbo.P11_PersonDesignatation methPD

    INNER JOIN dbo.O12_Designatation methD
        ON methD.DesignationId = methPD.O12_DesignationId
       AND methD.IsActive = 1
       AND methD.O10_PostId = 6

    INNER JOIN dbo.o_designatationreletation rel
        ON rel.o12_parentid = methD.DesignationId
       AND rel.IsActive = 1

    INNER JOIN dbo.O12_Designatation workerD
        ON workerD.DesignationId = rel.o12_childid
       AND workerD.IsActive = 1
       AND workerD.O10_PostId = 7

    INNER JOIN dbo.P11_PersonDesignatation workerPD
        ON workerPD.O12_DesignationId = workerD.DesignationId
       AND workerPD.IsActive = 1

    INNER JOIN dbo.P02_Person p
        ON p.PersonId = workerPD.P02_PersonId
       AND p.IsActive = 1

    WHERE methPD.P02_PersonId = @MethPersonId
      AND methPD.IsActive = 1
      AND workerD.O05_Office_Id = methD.O05_Office_Id

    ORDER BY p.PersonName;
END
