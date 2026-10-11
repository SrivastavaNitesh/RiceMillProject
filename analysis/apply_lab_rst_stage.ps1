#requires -Version 7.0
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$settings = Get-Content (Join-Path $root 'appsettings.json') -Raw | ConvertFrom-Json
$connection = New-Object System.Data.SqlClient.SqlConnection($settings.ConnectionStrings.DefaultConnection)
$transaction = $null
try {
    $connection.Open()
    $backupCommand = $connection.CreateCommand()
    $backupCommand.CommandText = "SELECT o.name,m.definition FROM sys.sql_modules m JOIN sys.objects o ON o.object_id=m.object_id WHERE o.name IN ('sp_MoveRSTProgress','sp_LabRstList','sp_LabItemOptions','sp_LabReportSave','fn_LabRstItems','fn_LabRstCoverage','sp_LabReportEditGet','sp_LabReportUpdate')"
    $backup = New-Object System.Data.DataTable
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($backupCommand)
    [void]$adapter.Fill($backup)
    $backupDirectory = Join-Path $root 'artifacts/lab-stage'
    [void](New-Item -ItemType Directory -Force -Path $backupDirectory)
    $backup | Export-Clixml (Join-Path $backupDirectory ('procedures-before-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.xml'))
    $transaction = $connection.BeginTransaction()
    foreach ($file in @('RST_Progress_Integration.sql','Lab_RST_Stage_Workflow.sql','Lab_Report_Edit.sql')) {
        foreach ($batch in [regex]::Split((Get-Content (Join-Path $root $file) -Raw), '(?im)^GO\s*$')) {
            if ([string]::IsNullOrWhiteSpace($batch)) { continue }
            $command = $connection.CreateCommand()
            $command.Transaction = $transaction
            $command.CommandTimeout = 60
            $command.CommandText = $batch
            try { [void]$command.ExecuteNonQuery() } finally { $command.Dispose() }
        }
    }
    $transaction.Commit()
    Write-Output 'Applied lab stage migration successfully.'
    $verify = $connection.CreateCommand()
    $verify.CommandText = "SELECT StageId,StageCode,StageName FROM dbo.m_RSTStageMaster WHERE StageId=7 OR StageCode IN ('LAB_REPORT_PARTIAL','LAB_REPORT_COMPLETED'); EXEC dbo.sp_LabRstList @ForEntry=1;"
    $data = New-Object System.Data.DataSet
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($verify)
    [void]$adapter.Fill($data)
    $data.Tables[0] | Format-Table
    Write-Output ('Eligible lab dashboard rows: ' + $data.Tables[1].Rows.Count)
} catch {
    if ($transaction -and $transaction.Connection) { $transaction.Rollback() }
    throw
} finally {
    $connection.Dispose()
}
