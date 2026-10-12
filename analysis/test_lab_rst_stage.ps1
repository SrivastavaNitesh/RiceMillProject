$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$database = 'RiceMillLabStageTest_' + [Guid]::NewGuid().ToString('N')
$connection = New-Object System.Data.SqlClient.SqlConnection('Server=.\SQLEXPRESS;Database=master;Integrated Security=true;TrustServerCertificate=true')
function Invoke-Batches([string] $sql) {
    foreach ($batch in [regex]::Split($sql, '(?im)^GO\s*$')) {
        if ([string]::IsNullOrWhiteSpace($batch)) { continue }
        $command = $connection.CreateCommand()
        $command.CommandText = $batch
        $command.CommandTimeout = 60
        try { [void]$command.ExecuteNonQuery() } finally { $command.Dispose() }
    }
}
$connection.add_InfoMessage({ param($sender, $event) Write-Output $event.Message })
try {
    $connection.Open()
    Invoke-Batches "CREATE DATABASE [$database]"
    $connection.ChangeDatabase($database)
    $fixture = (Get-Content (Join-Path $PSScriptRoot 'lab_rst_stage_tests.sql') -Raw) -split '-- TEST_BODY', 2
    Invoke-Batches $fixture[0]
    Invoke-Batches (Get-Content (Join-Path $root 'RST_Progress_Integration.sql') -Raw)
    Invoke-Batches (Get-Content (Join-Path $root 'Lab_RST_Stage_Workflow.sql') -Raw)
    Invoke-Batches (Get-Content (Join-Path $root 'Lab_RST_Stage_Workflow.sql') -Raw)
    Invoke-Batches $fixture[1]
    Invoke-Batches (Get-Content (Join-Path $root 'Lab_Report_Edit.sql') -Raw)
    Invoke-Batches (Get-Content (Join-Path $root 'Lab_Report_Edit.sql') -Raw)
    Invoke-Batches (Get-Content (Join-Path $PSScriptRoot 'lab_report_edit_tests.sql') -Raw)
    foreach ($batch in [regex]::Split((Get-Content (Join-Path $root 'Billing_Module.sql') -Raw), '(?im)^GO\s*$')) {
        if ($batch -match "VALUES\(N'BILL_GENERATED'" -or $batch -match "IF OBJECT_ID\('dbo.t_BillingBill'" -or $batch -match 'CREATE OR ALTER PROCEDURE dbo.sp_BillingSave\s') {
            Invoke-Batches $batch
        }
    }
    Invoke-Batches (Get-Content (Join-Path $PSScriptRoot 'billing_progress_tests.sql') -Raw)
    Write-Output 'PASS: migration applies twice and workflow assertions pass.'
} finally {
    if ($connection.State -eq 'Open') {
        $connection.ChangeDatabase('master')
        if ($database -match '^RiceMillLabStageTest_[0-9a-f]{32}$') {
            Invoke-Batches "IF DB_ID(N'$database') IS NOT NULL DROP DATABASE [$database]"
        }
    }
    $connection.Dispose()
}
