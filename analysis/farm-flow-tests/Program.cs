using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using RiceMillProject.DAL;

var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
var settings = JsonDocument.Parse(
    File.ReadAllText(Path.Combine(root, "appsettings.json")),
    new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
var connectionString = settings.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString()
    ?? throw new InvalidOperationException("DefaultConnection is missing.");
var migration = @"D:\2026\Working_R\DB\2026-11-03_DirectCenterFarmFlow.sql";
if (!File.Exists(migration)) throw new FileNotFoundException("Farm migration is missing.", migration);

await using var connection = new SqlConnection(connectionString);
await connection.OpenAsync();
foreach (var batch in Regex.Split(await File.ReadAllTextAsync(migration), @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
{
    if (string.IsNullOrWhiteSpace(batch)) continue;
    await using var command = new SqlCommand(batch, connection) { CommandTimeout = 120 };
    await command.ExecuteNonQueryAsync();
}

async Task Check(string sql, string label)
{
    await using var command = new SqlCommand(sql, connection);
    if (Convert.ToInt32(await command.ExecuteScalarAsync()) != 1) throw new Exception("FAIL: " + label);
    Console.WriteLine("PASS: " + label);
}

await Check("SELECT IIF(OBJECT_ID('dbo.m_PartyFarm','U') IS NOT NULL,1,0)", "Farm master exists");
await Check("SELECT IIF(OBJECT_ID('dbo.t_PartyFarmPerson','U') IS NOT NULL,1,0)", "Farm-person mapping exists");
await Check("SELECT IIF(OBJECT_ID('dbo.t_InwardFarmMapping','U') IS NOT NULL,1,0)", "Inward-farm mapping exists");
await Check("SELECT IIF(EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.t_PartyFarmPerson') AND name='UX_t_PartyFarmPerson_Primary'),1,0)", "One active primary contact index exists");
await Check("SELECT IIF((SELECT COUNT(*) FROM sys.foreign_keys WHERE parent_object_id IN (OBJECT_ID('dbo.t_PartyFarmPerson'),OBJECT_ID('dbo.t_InwardFarmMapping')))>=4,1,0)", "Farm mappings have required foreign keys");

var configuration = new ConfigurationBuilder()
    .AddJsonFile(Path.Combine(root, "appsettings.json"), optional: false)
    .Build();
var inwardRows = new GateInwardDAL(configuration).GetAllInwardEntries();
Console.WriteLine($"PASS: Inward DAL reads with an open connection ({inwardRows.Count} rows)");
var farmRows = new GateEntryDAL(configuration).GetCenterFarms();
Console.WriteLine($"PASS: Center farm DAL reads with an available connection ({farmRows.Rows.Count} rows)");
Console.WriteLine("ALL FARM FLOW DATABASE CHECKS PASSED");
