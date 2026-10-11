#requires -Version 7.0
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$settings = Get-Content (Join-Path $root 'appsettings.json') -Raw | ConvertFrom-Json
$connection = New-Object System.Data.SqlClient.SqlConnection($settings.ConnectionStrings.DefaultConnection)
$transaction = $null
try {
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = "SELECT o.name,m.definition FROM sys.sql_modules m JOIN sys.objects o ON o.object_id=m.object_id WHERE o.name IN ('sp_BillingRstList','sp_BillingSave','sp_BillingSource')"
    $backup = New-Object System.Data.DataTable
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($command)
    [void]$adapter.Fill($backup)
    $directory = Join-Path $root 'artifacts/billing-stage'
    [void](New-Item -ItemType Directory -Force -Path $directory)
    $backup | Export-Clixml (Join-Path $directory ('before-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.xml'))
    $transaction = $connection.BeginTransaction()
    $batches = [regex]::Split((Get-Content (Join-Path $root 'Billing_Module.sql') -Raw), '(?im)^GO\s*$')
    foreach ($batch in $batches) {
        if ($batch -notmatch 'CREATE OR ALTER PROCEDURE dbo\.sp_Billing(RstList|Save|Source)\s' -and $batch -notmatch "VALUES\(N'BILL_GENERATED'" -and $batch -notmatch "ADD ActualWeightKg") { continue }
        $command = $connection.CreateCommand()
        $command.Transaction = $transaction
        $command.CommandText = $batch
        [void]$command.ExecuteNonQuery()
    }
    $command = $connection.CreateCommand()
    $command.Transaction = $transaction
    $command.CommandText = 'EXEC dbo.sp_BillingRstList'
    $rows = New-Object System.Data.DataTable
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($command)
    [void]$adapter.Fill($rows)
    foreach ($row in $rows.Rows) {
        if ($row.CurrentStageId -notin @(14,15) -and $row.IsNull('BillId')) { throw 'Unexpected billing stage.' }
    }
    $transaction.Commit()
    Write-Output ('Billing procedures applied; eligible rows: ' + $rows.Rows.Count)
    $command = $connection.CreateCommand()
    $command.CommandText = "SELECT StageId FROM dbo.m_RSTStageMaster WHERE StageCode=N'BILL_GENERATED'"
    Write-Output ('Bill Generated stage ID: ' + $command.ExecuteScalar())
} catch {
    if ($transaction -and $transaction.Connection) { $transaction.Rollback() }
    throw
} finally { $connection.Dispose() }
