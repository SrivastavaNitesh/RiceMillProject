$ErrorActionPreference='Stop'
$base='http://localhost:5036'
$s=New-Object Microsoft.PowerShell.Commands.WebRequestSession
function Check($value,$label){if(-not $value){throw $label};Write-Output "PASS: $label"}
$null=Invoke-WebRequest -UseBasicParsing "$base/Account/Login" -WebSession $s -Method Post -Body @{Username='admin-test';Password='LabTest-Only-2026';returnUrl='/Dashboard/Index'}
$page=Invoke-WebRequest -UseBasicParsing "$base/GateEntry/Index" -WebSession $s
Check ($page.Content.Contains('RST-2026-1') -and $page.Content.Contains('TEST-VEHICLE-A') -and $page.Content.Contains('Test Driver A')) 'Saved inward-linked RST renders with vehicle and driver'
Check (-not $page.Content.Contains('An error occurred while loading entries')) 'Register reader has all required SQL columns'
Check ($page.Content.Contains('/Unload/Assign?rstNumber=RST-2026-1')) 'Saved RST links to the existing unloading assignment action'
$page=Invoke-WebRequest -UseBasicParsing "$base/Dashboard/Index" -WebSession $s
Check ($page.BaseResponse.ResponseUri.AbsolutePath -eq '/Dashboard/Index') 'Admin remains on operations dashboard'
$cards=[regex]::Matches($page.Content,'<h2 class="display-6[^>]*>(\d+)</h2>')
Check ($cards.Count -eq 4) 'Four dashboard counts rendered'
Check ($cards[0].Groups[1].Value -eq '9') 'Inward-linked RST counted once and legacy RSTs retained'
Check ($cards[1].Groups[1].Value -eq '1') 'Weight-done RST counted as pending unloading'
Check ($cards[2].Groups[1].Value -eq '4') 'Pending lab count uses mapped-test coverage'
Check ($page.Content -match '(?s)<a[^>]+href="/GateEntry(?:/Index)?"[^>]*>(?:(?!</a>).)*Weighbridge RST Operations') 'Weighbridge dashboard link opens RST register'