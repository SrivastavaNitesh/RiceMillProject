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

$employeeView = Read-RepoFile 'Views/Person/Employee.cshtml'
$personController = Read-RepoFile 'Controllers/PersonController.cs'
Assert-Contains $employeeView 'id="personNameLookup"' 'Employee Full Name uses a searchable person list'
Assert-Contains $employeeView 'tags: true' 'Employee Full Name allows a typed new person'
Assert-Contains $employeeView 'data-mobile=' 'Existing person options carry mobile details'
Assert-Contains $employeeView 'selectedPersonId\.value = isNew' 'Existing and new person selections remain distinguishable'
Assert-Contains $personController 'existing\.PersonId == person\.PersonId' 'Existing person selection is revalidated on the server'
Assert-Contains $personController 'person\.ShareCompany = true' 'Existing person is mapped without creating a duplicate person'

$gatemanView = Read-RepoFile 'Views/Gateman/Index.cshtml'
$gateEntryDal = Read-RepoFile 'DAL/GateEntryDAL.cs'
$gateInwardDal = Read-RepoFile 'DAL/GateInwardDAL.cs'
Assert-Contains $gatemanView 'name="PartyCategory"' 'Inward form provides Direct and Center party modes'
Assert-Contains $gatemanView 'id="txtFarmName"' 'Center mode provides searchable farm selection'
Assert-Contains $gatemanView '/Gateman/QuickAddCenterFarm' 'New farm uses the protected center-farm endpoint'
Assert-Contains $gatemanView 'quickFarmPerson' 'New farm captures its contact person'
Assert-Contains $gatemanView 'quickFarmMobile' 'New farm captures Indian mobile number'
Assert-Contains $gatemanView 'Search or type contact person' 'Farm popup provides searchable existing/new contact control'
Assert-Contains $gatemanView 'Existing person ko is Center / Farm ke saath share karein' 'Farm popup provides explicit person sharing consent'
Assert-Contains $gatemanView 'allowOutsideClick: false' 'Quick-add modal prevents accidental backdrop dismissal'
Assert-Contains $gatemanView 'personId: String\(confirmation\.value\?\.personId' 'Selected farm contact id is posted to backend'
Assert-Contains (Read-RepoFile 'Controllers/GatemanController.cs') '!sharePerson' 'Backend validates existing farm contact sharing consent'
Assert-Contains $gateEntryDal 'existingPersonId\.GetValueOrDefault\(\) > 0' 'Farm mapping reuses selected existing contact without duplication'
Assert-Contains $gateEntryDal 't_PartyFarmPerson' 'Farm-person many-to-many mapping is implemented'
Assert-Contains $gateEntryDal 'ISNULL\(P\.PartyCategory,1\) = @PartyCategory' 'Direct and Center person reuse remains type scoped'
Assert-Contains $gateInwardDal 'GetAllInwardEntries\(\)[\s\S]*RecordEditContext\.Open\(con\)[\s\S]*sp_ManageGateInwardEntry' 'Inward page opens its database connection before reading'

Write-Output 'ALL RECENT UI/FLOW CHECKS PASSED'
