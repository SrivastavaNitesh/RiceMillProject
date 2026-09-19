namespace RiceMillProject.Models;
public class DatabaseSchemaMap { public List<DatabaseTableMap> Tables { get; set; }=[]; public List<DatabaseRelationshipMap> Relationships { get; set; }=[]; }
public class DatabaseTableMap { public string Name { get; set; }=""; public long RowCount { get; set; } public List<DatabaseColumnMap> Columns { get; set; }=[]; }
public class DatabaseColumnMap { public string Name { get; set; }=""; public string DataType { get; set; }=""; public bool IsPrimaryKey { get; set; } }
public class DatabaseRelationshipMap { public string FromTable { get; set; }=""; public string FromColumn { get; set; }=""; public string ToTable { get; set; }=""; public string ToColumn { get; set; }=""; public bool IsEnforced { get; set; } }
