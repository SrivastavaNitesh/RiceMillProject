$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
function Read-RepoFile([string]$relativePath) {
    Get-Content -LiteralPath (Join-Path $root $relativePath) -Raw
}
function Assert-Contains([string]$text, [string]$pattern, [string]$label) {
    if ($text -notmatch $pattern) { throw "FAIL: $label" }
    Write-Output "PASS: $label"
}

$unload = Read-RepoFile 'Views/Unload/Index.cshtml'
Assert-Contains $unload 'RST Assignment Board' 'Unload page has professional operations heading'
Assert-Contains $unload 'pendingAssignments' 'Unload tickets use database-backed pending assignments'
Assert-Contains $unload 'asp-action="Assign"' 'Every RST ticket keeps the existing Assign action'
Assert-Contains $unload 'asp-route-rstNumber="@entry\.RSTNumber"' 'Assign action passes the selected RST number'
Assert-Contains $unload 'id="ticketSearch"' 'Ticket search input is available'
Assert-Contains $unload 'data-ticket-search=' 'Ticket cards expose RST, party and vehicle search data'
Assert-Contains $unload 'ticket\.classList\.toggle' 'Ticket filtering hides only non-matching cards'
Assert-Contains $unload '@@media \(max-width:767\.98px\)' 'Ticket board has a mobile layout'
Assert-Contains $unload 'No pending RST assignment' 'Empty assignment state is user friendly'

$gateView = Read-RepoFile 'Views/GateEntry/Create.cshtml'
$gateController = Read-RepoFile 'Controllers/GateEntryController.cs'
$gateBal = Read-RepoFile 'BAL/GateEntryBAL.cs'
$gateDal = Read-RepoFile 'DAL/GateEntryDAL.cs'
Assert-Contains $gateView 'id="btnQuickAddCategory"' 'GateEntry page exposes Item Category quick-add'
Assert-Contains $gateView '/GateEntry/QuickAddItemCategory' 'Category quick-add calls its protected endpoint'
Assert-Contains $gateController '\[ValidateAntiForgeryToken\][\s\S]*QuickAddItemCategory' 'Category endpoint requires anti-forgery validation'
Assert-Contains $gateBal 'categoryName\.Length < 2 \|\| categoryName\.Length > 100' 'Category name length is validated in BAL'
Assert-Contains $gateDal 'IsolationLevel\.Serializable' 'Category duplicate check runs serializably'
Assert-Contains $gateDal 'LOWER\(LTRIM\(RTRIM\(CategoryName\)\)\)' 'Category duplicate match is case-insensitive'

$vehicleHelper = Read-RepoFile 'Helpers/IndianVehicleNumber.cs'
$gatemanController = Read-RepoFile 'Controllers/GatemanController.cs'
$vehicleController = Read-RepoFile 'Controllers/VehicleController.cs'
Assert-Contains $vehicleHelper '\[0-9\]\{2\}BH' 'Vehicle validator supports BH-series numbers'
Assert-Contains $vehicleHelper '\[A-Z\]\{2\}\[0-9\]\{1,2\}' 'Vehicle validator supports state registration numbers'
Assert-Contains $gatemanController 'IndianVehicleNumber\.IsValid' 'Gateman backend uses shared vehicle validation'
Assert-Contains $vehicleController 'IndianVehicleNumber\.IsValid' 'Vehicle Master uses shared vehicle validation'

Write-Output 'ALL RECENT UI/FLOW CHECKS PASSED'
