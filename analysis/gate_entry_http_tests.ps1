$ErrorActionPreference='Stop'
$base='http://localhost:5034'
$session=New-Object Microsoft.PowerShell.Commands.WebRequestSession
function Check($ok,$text) { if(-not $ok) { throw $text }; Write-Output "PASS: $text" }
$page=Invoke-WebRequest -UseBasicParsing "$base/GateEntry/Create" -WebSession $session
Check ($page.Content.Contains('RST-UI-A') -and $page.Content.Contains('RST-UI-C') -and -not $page.Content.Contains('RST-NOT-REQUIRED') -and -not $page.Content.Contains('RST-EXITED')) 'Only pending RST-required inwards are shown'
Check ($page.Content.Contains('Test Company') -and $page.Content.Contains('Company Name') -and -not $page.Content.Contains('Target Unloading')) 'Company options render and target unloading field is removed'
$token=[System.Net.WebUtility]::HtmlDecode([regex]::Match($page.Content,'name="__RequestVerificationToken"[^>]*value="([^"]+)"').Groups[1].Value)
$fields=@{__RequestVerificationToken=$token;InwardNo='RST-UI-A';GrossWeight='1200.50';TargetOfficeId='1'}
$bad=$fields.Clone();$bad.GrossWeight='-1'
$response=Invoke-WebRequest -UseBasicParsing "$base/GateEntry/Create" -WebSession $session -Method Post -Body $bad
Check ($response.Content.Contains('positive gross weight') -and $response.Content.Contains('RST-UI-A')) 'Invalid weight rejected and selection retained'
$bad=$fields.Clone();$bad.InwardNo='RST-NOT-REQUIRED'
$response=Invoke-WebRequest -UseBasicParsing "$base/GateEntry/Create" -WebSession $session -Method Post -Body $bad
Check ($response.Content.Contains('pending RST-required inward')) 'Non-RST inward rejected on server'
# Preserve rendered HTML for DOM checks before consuming the fixture.
$html=$page.Content
$html=[regex]::Replace($html,'(src|href)="/([^"?]+)(?:\?[^"]*)?"', {param($m) $p=Join-Path (Join-Path (Split-Path $PSScriptRoot -Parent) 'wwwroot') $m.Groups[2].Value; if(Test-Path -LiteralPath $p -PathType Leaf){$m.Groups[1].Value+'="'+([Uri]$p).AbsoluteUri+'"'}else{$m.Value}})
$checks=@"
<script>
try {
 const a=[];const check=(v,n)=>{if(!v)throw Error(n);a.push('PASS: '+n)};
 const inward=document.getElementById('ddlInwardNo'),vehicle=document.getElementById('ddlVehicle'),driver=document.getElementById('ddlDriver');
 const choose=(e,v)=>{e.value=v;e.dispatchEvent(new Event('change'))};
 choose(inward,'RST-UI-A');
 check(vehicle.value==='TEST-VEHICLE-A' && driver.selectedOptions[0].text.includes('Test Driver A'),'Inward fills vehicle and driver');
 check(document.getElementById('inwardParty').value==='Test Admin','Inward fills party');
 choose(vehicle,'TEST-VEHICLE-B');
 check(inward.value==='' && inward.options.length===3 && document.getElementById('generate-rst').disabled,'Multiple matching trips require exact inward');
 choose(inward,'RST-UI-C');
 check(driver.selectedOptions[0].text.includes('Test Driver C'),'Chosen trip uses its own driver');
 choose(driver,JSON.stringify(['test driver a','1111111111']));
 check(inward.value==='RST-UI-A' && vehicle.value==='TEST-VEHICLE-A','Driver selects its pending vehicle and inward');
 choose(vehicle,'TEST-VEHICLE-A');
 check(driver.selectedOptions[0].text.includes('Test Driver A'),'Vehicle selects its driver');
 const data=new FormData(document.getElementById('weighbridgeForm'));
 check(data.get('InwardNo')==='RST-UI-A' && !data.has('TargetLocationIds'),'Form submits exact inward without removed location');
 choose(inward,'');check(vehicle.value==='' && driver.value==='' && document.getElementById('generate-rst').disabled,'Clearing inward clears dependent selection');
 document.body.dataset.gateTests='PASS';const pre=document.createElement('pre');pre.id='gate-test-results';pre.textContent=a.join('\n');document.body.append(pre);
}catch(e){document.body.dataset.gateTests='FAIL';const pre=document.createElement('pre');pre.id='gate-test-results';pre.textContent=e.stack;document.body.append(pre);}
</script>
"@
$html=$html.Replace('</body>',$checks+'</body>')
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'gate-entry-dom-check.html'),$html)
$response=Invoke-WebRequest -UseBasicParsing "$base/GateEntry/Create" -WebSession $session -Method Post -Body $fields
Check ($response.Content.Contains('RST Generated Successfully!') -and -not $response.Content.Contains('RST-UI-A')) 'RST generated and inward removed from pending dropdown'
$response=Invoke-WebRequest -UseBasicParsing "$base/GateEntry/Create" -WebSession $session -Method Post -Body $fields
Check ($response.Content.Contains('already has an RST')) 'Duplicate RST submission rejected'
$fields.Remove('__RequestVerificationToken');$blocked=$false
try { $null=Invoke-WebRequest -UseBasicParsing "$base/GateEntry/Create" -WebSession $session -Method Post -Body $fields } catch { $blocked=[int]$_.Exception.Response.StatusCode -eq 400 }
Check $blocked 'Missing antiforgery token rejected'