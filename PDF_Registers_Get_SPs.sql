USE RiceMillDB;
GO

-- SP: Get All Visitors
CREATE OR ALTER PROCEDURE sp_GetAllVisitors
AS
BEGIN
    SELECT v.*, g.GateName, d.DepartmentName
    FROM t_VisitorRegister v
    INNER JOIN m_GateMaster g ON v.GateId = g.GateId
    LEFT JOIN m_DepartmentMaster d ON v.DepartmentId = d.DepartmentId
    ORDER BY v.VisitorId DESC;
END
GO

-- SP: Get All Temp Material Items
CREATE OR ALTER PROCEDURE sp_GetAllTempMaterials
AS
BEGIN
    SELECT tm.*, g.GateName, d.DepartmentName
    FROM t_TempMaterialRegister tm
    INNER JOIN m_GateMaster g ON tm.GateId = g.GateId
    LEFT JOIN m_DepartmentMaster d ON tm.DepartmentId = d.DepartmentId
    ORDER BY tm.TempMaterialId DESC;
END
GO

-- SP: Get All Security Incidents
CREATE OR ALTER PROCEDURE sp_GetAllSecurityIncidents
AS
BEGIN
    SELECT i.*, g.GateName, p.PersonName AS ReportedToPersonName
    FROM t_SecurityIncident i
    INNER JOIN m_GateMaster g ON i.GateId = g.GateId
    LEFT JOIN p02_Person p ON i.ReportedToPersonId = p.PersonId
    ORDER BY i.IncidentId DESC;
END
GO
