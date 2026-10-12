#requires -Version 7.0
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$source = Get-Content (Join-Path $root 'DAL/PurchaseReportDAL.cs') -Raw
$query = [regex]::Match($source, 'internal const string Query = """\r?\n([\s\S]*?)\r?\n\s*""";').Groups[1].Value
if (!$query) { throw 'Report query not found.' }
$settings = Get-Content (Join-Path $root 'appsettings.json') -Raw | ConvertFrom-Json
$connection = [System.Data.SqlClient.SqlConnection]::new($settings.ConnectionStrings.DefaultConnection)
try {
    $command = $connection.CreateCommand()
    $command.CommandText = $query
    $command.CommandTimeout = 90
    [void]$command.Parameters.AddWithValue('@From', [datetime]'2026-01-01')
    [void]$command.Parameters.AddWithValue('@Until', [datetime]'2027-01-01')
    [void]$command.Parameters.AddWithValue('@Company', [DBNull]::Value)
    [void]$command.Parameters.AddWithValue('@Party', [DBNull]::Value)
    [void]$command.Parameters.AddWithValue('@AllDates', $true)
    $adapter = [System.Data.SqlClient.SqlDataAdapter]::new($command)
    $data = [System.Data.DataSet]::new()
    [void]$adapter.Fill($data)
    if ($data.Tables.Count -ne 7) { throw 'Unexpected result sets.' }
    for ($i = 0; $i -lt 7; $i++) { Write-Output "Result set ${i}: $($data.Tables[$i].Rows.Count) rows" }
    $duplicates = $data.Tables[1].Rows | Group-Object DetailId | Where-Object Count -gt 1
    if ($duplicates) { throw 'Duplicate unload details in report.' }
    Write-Output 'Read-only purchase query verified.'
} finally { $connection.Dispose() }
