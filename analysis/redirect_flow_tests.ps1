$ErrorActionPreference = 'Stop'

function Read-Source([string]$path) {
    return Get-Content -LiteralPath $path -Raw
}

function Test-Case([string]$name, [bool]$passed, [string]$detail) {
    [pscustomobject]@{ Test = $name; Passed = $passed; Detail = $detail }
}

$gateman = Read-Source 'Controllers/GatemanController.cs'
$gateEntry = Read-Source 'Controllers/GateEntryController.cs'
$unload = Read-Source 'Controllers/UnloadController.cs'
$meth = Read-Source 'Controllers/MethController.cs'
$person = Read-Source 'Controllers/PersonController.cs'
$gatemanPrint = Read-Source 'Views/Gateman/Print.cshtml'
$weightPrint = Read-Source 'Views/GateEntry/WeighmentSlip.cshtml'
$unloadPrint = Read-Source 'Views/Unload/PrintChallan.cshtml'

$results = @(
    Test-Case 'Gateman inward success opens Print' ($gateman -match 'RedirectToAction\(nameof\(Print\),\s*new\s*\{\s*id\s*=\s*model\.InwardId') 'CreateGateEntry -> Gateman/Print'
    Test-Case 'Weightman RST success opens WeighmentSlip' ($gateEntry -match 'RedirectToAction\(nameof\(WeighmentSlip\),\s*new\s*\{\s*id\s*=\s*savedRST') 'GateEntry/Create -> WeighmentSlip'
    Test-Case 'Supervisor challan save opens PrintChallan' ($unload -match 'RedirectToAction\(\s*nameof\(PrintChallan\)') 'Unload assignment/completion -> PrintChallan'
    Test-Case 'Gateman print Back returns registration' ($gatemanPrint -match 'Url\.Action\("Index",\s*"Gateman"\)') 'Gateman/Print -> Gateman/Index'
    Test-Case 'Weight print Back returns registration' ($weightPrint -match 'Url\.Action\("Index",\s*"GateEntry"\)') 'WeighmentSlip -> GateEntry/Index'
    Test-Case 'Unload print has operational Back route' ($unloadPrint -match 'href="/Unload/Index"') 'PrintChallan -> Unload/Index'
    Test-Case 'Meth save returns to Meth dashboard' ($meth -match 'RST \{saved\.RSTNumber\} work entry saved successfully\.[\s\S]{0,500}RedirectToAction\([\s\S]{0,80}nameof\(Index\)') 'ExecuteUnload -> Meth/Index'
    Test-Case 'Supervisor field save returns work register' ($unload -match 'Field Details saved successfully\.[\s\S]{0,300}RedirectToAction\(nameof\(MethWorkRegister\)') 'SupervisorAction -> MethWorkRegister'
    Test-Case 'Supervisor meth save returns work register' ($unload -match 'Meth unloading payment details saved[\s\S]{0,200}RedirectToAction\(nameof\(MethWorkRegister\)') 'SupervisorMethEntry -> MethWorkRegister'
    Test-Case 'Employee save returns employee list' ($person -match 'registered successfully![\s\S]{0,200}RedirectToAction\("EmployeeList"\)') 'Person/Employee -> EmployeeList'
)

$results | ForEach-Object {
    $status = if ($_.Passed) { 'PASS' } else { 'FAIL' }
    Write-Output "$status | $($_.Test) | $($_.Detail)"
}

$failed = @($results | Where-Object { -not $_.Passed })
Write-Output "SUMMARY | Passed=$($results.Count - $failed.Count) Failed=$($failed.Count) Total=$($results.Count)"
if ($failed.Count -gt 0) { exit 1 }
