SET NOCOUNT ON;
SELECT 'COUNTS' AS Section;
SELECT (SELECT COUNT(*) FROM sys.tables) AS Tables,(SELECT COUNT(*) FROM sys.procedures) AS Procedures,(SELECT COUNT(*) FROM sys.foreign_keys) AS ForeignKeys,(SELECT COUNT(*) FROM sys.check_constraints) AS CheckConstraints,(SELECT COUNT(*) FROM sys.triggers) AS Triggers,(SELECT COUNT(*) FROM sys.views) AS Views,(SELECT COUNT(*) FROM sys.synonyms) AS Synonyms;
SELECT 'MISSING_OBJECTS' AS Section;
SELECT v.ObjectName,OBJECT_ID(v.ObjectName) AS ObjectId FROM (VALUES ('dbo.p1_person'),('dbo.p1_persondesignatation'),('dbo.t_OfficeLocations'),('dbo.sp_GetPersonsByType')) v(ObjectName);
SELECT 'COUNTER_SEEDS' AS Section;
SELECT v.CounterType, c.LastNo FROM (VALUES ('GLOBAL'),('INWARD'),('OUTWARD'),('RST')) v(CounterType) LEFT JOIN dbo.t_SerialCounters c ON c.CounterType=v.CounterType;
SELECT 'STATUS_MASTER' AS Section;
SELECT StatusId,StatusCode,StatusName FROM dbo.m_StatusMaster ORDER BY StatusId;
SELECT 'LIVE_PARAMETERS' AS Section;
SELECT OBJECT_NAME(p.object_id) AS ProcedureName,p.name,TYPE_NAME(p.user_type_id) AS DataType,p.max_length,p.is_output FROM sys.parameters p JOIN sys.procedures pr ON p.object_id=pr.object_id ORDER BY ProcedureName,p.parameter_id;
SELECT 'GATE_RESULT_COLUMNS' AS Section;
SELECT column_ordinal,name,system_type_name,error_number,error_message FROM sys.dm_exec_describe_first_result_set_for_object(OBJECT_ID('dbo.sp_GetAllGateEntries'),0);
SELECT 'INWARD_SELECT_CHECK' AS Section;
BEGIN TRY
 EXEC dbo.sp_ManageGateInwardEntry @Action='SELECT_ALL';
END TRY BEGIN CATCH SELECT ERROR_NUMBER() AS ErrorNumber,ERROR_MESSAGE() AS ErrorMessage; END CATCH;
SELECT 'WORKER_SELECT_CHECK' AS Section;
BEGIN TRY
 EXEC dbo.sp_GetWorkersByMeth @MethPersonId=0;
END TRY BEGIN CATCH SELECT ERROR_NUMBER() AS ErrorNumber,ERROR_MESSAGE() AS ErrorMessage; END CATCH;
SELECT 'LOCATION_SELECT_CHECK' AS Section;
BEGIN TRY
 EXEC sys.sp_executesql N'SELECT TOP (0) LocationId,OfficeId,LocationName FROM dbo.t_OfficeLocations';
END TRY BEGIN CATCH SELECT ERROR_NUMBER() AS ErrorNumber,ERROR_MESSAGE() AS ErrorMessage; END CATCH;
SELECT 'DATA_QUALITY_COUNTS' AS Section;
SELECT 'Users without person' AS Issue,COUNT(*) AS AffectedRows FROM sa05_user u LEFT JOIN P02_Person p ON p.PersonId=u.PersonId WHERE p.PersonId IS NULL
UNION ALL SELECT 'Users without user type',COUNT(*) FROM sa05_user u LEFT JOIN sa10_usertype t ON t.UserTypeId=u.sa10_usertypeid WHERE t.UserTypeId IS NULL
UNION ALL SELECT 'Users with role text differing from user type',COUNT(*) FROM sa05_user u JOIN P02_Person p ON p.PersonId=u.PersonId JOIN sa10_usertype t ON t.UserTypeId=u.sa10_usertypeid WHERE ISNULL(p.PersonType,'')<>t.UserTypeName
UNION ALL SELECT 'Offices without OfficeType',COUNT(*) FROM o05_office WHERE OfficeType IS NULL
UNION ALL SELECT 'Person-designation without person',COUNT(*) FROM P11_PersonDesignatation d LEFT JOIN P02_Person p ON p.PersonId=d.P02_PersonId WHERE p.PersonId IS NULL
UNION ALL SELECT 'Person-designation without designation',COUNT(*) FROM P11_PersonDesignatation d LEFT JOIN O12_Designatation o ON o.DesignationId=d.O12_DesignationId WHERE o.DesignationId IS NULL
UNION ALL SELECT 'Duplicate username groups',COUNT(*) FROM (SELECT Username FROM sa05_user GROUP BY Username HAVING COUNT(*)>1) x
UNION ALL SELECT 'Duplicate vehicle-number groups',COUNT(*) FROM (SELECT VehicleNumber FROM m_Vehicle GROUP BY VehicleNumber HAVING COUNT(*)>1) x;
SELECT 'UNRESOLVED_STATIC_DEPENDENCIES' AS Section;
SELECT DISTINCT OBJECT_NAME(d.referencing_id) AS ProcedureName,d.referenced_schema_name,d.referenced_entity_name FROM sys.sql_expression_dependencies d JOIN sys.procedures p ON p.object_id=d.referencing_id WHERE d.referenced_id IS NULL AND d.referenced_database_name IS NULL AND d.referenced_server_name IS NULL ORDER BY ProcedureName,d.referenced_entity_name;
