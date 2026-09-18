SET NOCOUNT ON;
SELECT 'TABLE_COLUMNS' AS Section;
SELECT t.name AS TableName,c.column_id,c.name AS ColumnName,TYPE_NAME(c.user_type_id) AS DataType,c.max_length,c.precision,c.scale,c.is_nullable,c.is_identity,dc.definition AS DefaultDefinition
FROM sys.tables t JOIN sys.columns c ON t.object_id=c.object_id LEFT JOIN sys.default_constraints dc ON c.default_object_id=dc.object_id ORDER BY t.name,c.column_id;
SELECT 'FOREIGN_KEYS' AS Section;
SELECT fk.name,OBJECT_NAME(fk.parent_object_id) AS ParentTable, COL_NAME(fkc.parent_object_id,fkc.parent_column_id) AS ParentColumn,OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable,COL_NAME(fkc.referenced_object_id,fkc.referenced_column_id) AS ReferencedColumn,fk.is_disabled,fk.is_not_trusted FROM sys.foreign_keys fk JOIN sys.foreign_key_columns fkc ON fk.object_id=fkc.constraint_object_id ORDER BY ParentTable,fk.name;
SELECT 'INDEXES' AS Section;
SELECT t.name AS TableName,i.name,i.is_unique,i.is_primary_key,i.filter_definition, c.name AS ColumnName,ic.key_ordinal,ic.is_included_column FROM sys.tables t JOIN sys.indexes i ON t.object_id=i.object_id JOIN sys.index_columns ic ON i.object_id=ic.object_id AND i.index_id=ic.index_id JOIN sys.columns c ON ic.object_id=c.object_id AND ic.column_id=c.column_id ORDER BY t.name,i.name,ic.key_ordinal;
SELECT 'CHECKS' AS Section;
SELECT OBJECT_NAME(parent_object_id) AS TableName,name,definition,is_disabled,is_not_trusted FROM sys.check_constraints;
SELECT 'ROW_COUNTS_APPROXIMATE' AS Section;
SELECT t.name,SUM(p.rows) AS ApproxRows FROM sys.tables t JOIN sys.partitions p ON t.object_id=p.object_id AND p.index_id IN (0,1) GROUP BY t.name ORDER BY t.name;
SELECT 'PROCEDURES' AS Section;
SELECT '-- PROCEDURE: '+QUOTENAME(SCHEMA_NAME(p.schema_id))+'.'+QUOTENAME(p.name)+CHAR(13)+CHAR(10)+ISNULL(m.definition,'-- Definition unavailable')+CHAR(13)+CHAR(10)+'GO' FROM sys.procedures p LEFT JOIN sys.sql_modules m ON p.object_id=m.object_id ORDER BY p.name;
