$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$snapshot = Get-Content (Join-Path $PSScriptRoot 'database_metadata.txt') -Raw
$procedureMatches = [regex]::Matches($snapshot, '(?ms)^-- PROCEDURE: \[([^\]]+)\]\.\[([^\]]+)\]\r?\n(.*?)(?=^-- PROCEDURE:|\z)')
$sourceFiles = @(Get-ChildItem (Join-Path $projectRoot 'DAL') -Filter *.cs) + @(Get-Item (Join-Path $projectRoot 'Models/DataLayer.cs'))
$sqlFiles = Get-ChildItem $projectRoot -Filter *.sql
$procedureRows = foreach ($entry in $procedureMatches) {
    $procName = $entry.Groups[2].Value
    $callers = @($sourceFiles | Where-Object { (Get-Content $_.FullName -Raw) -match ('(?i)"' + [regex]::Escape($procName) + '"') } | ForEach-Object { $_.FullName.Substring($projectRoot.Length+1) })
    $definitions = @($sqlFiles | Where-Object { (Get-Content $_.FullName -Raw) -match ('(?i)\b(?:CREATE\s+(?:OR\s+ALTER\s+)?|ALTER\s+)PROC(?:EDURE)?\s+(?:\[?dbo\]?\.)?\[?' + [regex]::Escape($procName) + '\]?\b') } | ForEach-Object { $_.Name })
    [pscustomobject]@{ Procedure=$procName; SnapshotLine=($snapshot.Substring(0,$entry.Index) -split "`n").Count; Callers=($callers -join '; '); RepositoryDefinitions=($definitions -join '; ') }
}
$procedureRows | Export-Csv (Join-Path $PSScriptRoot 'procedure_inventory.csv') -NoTypeInformation -Encoding UTF8
$liveNames = @($procedureRows.Procedure)
$allCalls = foreach ($file in $sourceFiles) {
    foreach ($m in [regex]::Matches((Get-Content $file.FullName -Raw), '(?i)"(sp_[a-z0-9_]+)"')) { $m.Groups[1].Value }
}
$allCalls = @($allCalls | Sort-Object -Unique)
'Live procedure count: ' + $procedureRows.Count
'Distinct literal procedure references in DAL/DataLayer: ' + $allCalls.Count
'Missing live procedures referenced by application:'
$allCalls | Where-Object { $_ -notin $liveNames }
'Live procedures without repository CREATE/ALTER definition:'
$procedureRows | Where-Object { -not $_.RepositoryDefinitions } | Select-Object -ExpandProperty Procedure
