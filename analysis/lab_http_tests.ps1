$ErrorActionPreference = 'Stop'
$baseUrl = 'http://localhost:5033'
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
function Get-Page([string]$path) { Invoke-WebRequest -UseBasicParsing -Uri ($baseUrl+$path) -WebSession $session }
function Token([string]$html) {
    $match = [regex]::Match($html, 'name="__RequestVerificationToken"[^>]*value="([^"]+)"')
    if (-not $match.Success) { throw 'Antiforgery token missing' }
    [System.Net.WebUtility]::HtmlDecode($match.Groups[1].Value)
}
function Post-Form([string]$path, [hashtable]$fields) {
    $pairs = foreach ($key in $fields.Keys) {
        foreach ($value in @($fields[$key])) { [Uri]::EscapeDataString($key)+'='+[Uri]::EscapeDataString([string]$value) }
    }
    Invoke-WebRequest -UseBasicParsing -Uri ($baseUrl+$path) -WebSession $session -Method Post -ContentType 'application/x-www-form-urlencoded' -Body ($pairs -join '&')
}
function Assert([bool]$condition, [string]$message) { if (-not $condition) { throw $message }; Write-Output ('PASS: '+$message) }

$page = Get-Page '/Lab/Index'
Assert ($page.BaseResponse.ResponseUri.AbsolutePath -eq '/Account/Login') 'Anonymous lab access redirects to login'
$page = Post-Form '/Account/Login' @{ Username='lab-test'; Password='LabTest-Only-2026'; returnUrl='/Lab/Index' }
Assert ($page.Content.Contains('RST quality checks') -and $page.Content.Contains('LAB-TEST-A')) 'Lab login opens unloaded RST dashboard'

$entry = Get-Page '/Lab/Create?rstNumber=LAB-TEST-A'
$json = [regex]::Match($entry.Content, '<script id="lab-entry-data" type="application/json">(.*?)</script>', 'Singleline').Groups[1].Value | ConvertFrom-Json
Assert (@($json.items).Count -eq 3 -and @($json.items | Where-Object ItemId -eq 4).Count -eq 0) 'RST A exposes exactly its three unloaded items'
Assert (@($json.items | Select-Object -ExpandProperty CategoryId -Unique).Count -eq 2) 'Only the two unloaded categories are available'
Assert (@($json.items | Where-Object ItemId -eq 1)[0].Tests.Count -eq 2) 'Selected item receives its mapped tests'

$tests = Get-Page '/Lab/Tests'
$saved = Post-Form '/Lab/Tests' @{ '__RequestVerificationToken'=(Token $tests.Content); 'Form.TestId'=0; 'Form.TestName'='HTTP test'; 'Form.ResultType'='Text'; 'Form.Unit'='grade'; 'Form.Description'='Synthetic HTTP check' }
Assert ($saved.Content.Contains('Test saved.') -and $saved.Content.Contains('HTTP test')) 'Test master form saves'
$editId = [regex]::Match($saved.Content, '(?s)<tr><td>HTTP test.*?/Lab/Tests/([0-9]+)').Groups[1].Value
if (-not $editId) { $editId = [regex]::Match($saved.Content, '(?s)<tr><td>HTTP test.*?/Lab/Tests\?id=([0-9]+)').Groups[1].Value }
Assert ([bool]$editId) 'Test edit link is present'
$edit = Get-Page ('/Lab/Tests/'+$editId)
$saved = Post-Form '/Lab/Tests' @{ '__RequestVerificationToken'=(Token $edit.Content); 'Form.TestId'=$editId; 'Form.TestName'='HTTP test edited'; 'Form.ResultType'='Text'; 'Form.Unit'='grade' }
Assert ($saved.Content.Contains('Test saved.') -and $saved.Content.Contains('HTTP test edited')) 'Test master edit saves'

$mappings = Get-Page '/Lab/Mappings'
$saved = Post-Form '/Lab/Mappings' @{ '__RequestVerificationToken'=(Token $mappings.Content); 'Form.MappingId'=0; 'Form.CategoryId'=1; 'Form.ItemId'=2; 'Form.TestId'=$editId }
Assert ($saved.Content.Contains('Item test mapping saved.') -and $saved.Content.Contains('HTTP test edited')) 'Mapping form saves without display-field validation errors'
$mappingId = [regex]::Match($saved.Content, '(?s)<td>HTTP test edited</td><td>.*?/Lab/Mappings/([0-9]+)').Groups[1].Value
if (-not $mappingId) { $mappingId = [regex]::Match($saved.Content, '(?s)<td>HTTP test edited</td><td>.*?/Lab/Mappings\?id=([0-9]+)').Groups[1].Value }
Assert ([bool]$mappingId) 'Mapping edit link is present'
$edit = Get-Page ('/Lab/Mappings/'+$mappingId)
$saved = Post-Form '/Lab/Mappings' @{ '__RequestVerificationToken'=(Token $edit.Content); 'Form.MappingId'=$mappingId; 'Form.CategoryId'=2; 'Form.ItemId'=3; 'Form.TestId'=$editId }
Assert ($saved.Content.Contains('Item test mapping saved.')) 'Mapping edit saves'
$saved = Post-Form '/Lab/RemoveMapping' @{ '__RequestVerificationToken'=(Token $saved.Content); id=$mappingId }
Assert ($saved.Content.Contains('Mapping removed.')) 'Mapping remove works'
$tests = Get-Page '/Lab/Tests'
$saved = Post-Form '/Lab/RemoveTest' @{ '__RequestVerificationToken'=(Token $tests.Content); id=$editId }
Assert ($saved.Content.Contains('Test removed from active selections.')) 'Test remove works'

$entry = Get-Page '/Lab/Create?rstNumber=LAB-TEST-A'
$submission = [regex]::Match($entry.Content, 'name="SubmissionId"[^>]*value="([^"]+)"').Groups[1].Value
$fields = @{ '__RequestVerificationToken'=(Token $entry.Content); RSTNumber='LAB-TEST-A'; SubmissionId=$submission; SelectedCategoryIds=@('1','2'); SelectedItemIds=@('1','3'); Remarks='HTTP multi-item check';
 'Results[0].CategoryId'=1; 'Results[0].ItemId'=1; 'Results[0].TestId'=1; 'Results[0].Value'='14.25';
 'Results[1].CategoryId'=1; 'Results[1].ItemId'=1; 'Results[1].TestId'=3; 'Results[1].Value'='Acceptable';
 'Results[2].CategoryId'=2; 'Results[2].ItemId'=3; 'Results[2].TestId'=3; 'Results[2].Value'='Clean' }
$saved = Post-Form '/Lab/Create' $fields
Assert ($saved.BaseResponse.ResponseUri.AbsolutePath -match '/Lab/Report' -and $saved.Content.Contains('14.25') -and $saved.Content.Contains('Clean')) 'Multi-category multi-item results save through MVC'
$reportId = [regex]::Match($saved.Content, 'Lab report LAB-([0-9]+)').Groups[1].Value
$retry = Post-Form '/Lab/Create' $fields
Assert ($retry.BaseResponse.ResponseUri.AbsolutePath -eq $saved.BaseResponse.ResponseUri.AbsolutePath) 'Same submission returns the same saved report'
$report = Get-Page ('/Lab/Report/'+$reportId)
Assert ($report.Content.Contains('Test Lab Technician') -and $report.Content.Contains('HTTP multi-item check')) 'Saved report shows technician and remarks'
$csv = Get-Page ('/Lab/Export/'+$reportId)
Assert ($csv.Headers['Content-Type'] -match 'text/csv' -and $csv.Content.Contains('14.25')) 'CSV report downloads stored results'
$list = Get-Page '/Lab/Reports?RstNumber=LAB-TEST-B'
Assert (-not $list.Content.Contains('LAB-'+$reportId+'</td>')) 'RST report filter excludes another RST'

$fields['Results[0].Value']='not numeric'
$fields['SubmissionId']=[Guid]::NewGuid().ToString()
$invalid = Post-Form '/Lab/Create' $fields
Assert ($invalid.Content.Contains('Enter a valid number') -and $invalid.Content.Contains('not numeric')) 'Invalid numeric result is rejected and entered values are retained'
$fields.Remove('__RequestVerificationToken')
$rejected = $false
try { $null = Post-Form '/Lab/Create' $fields } catch { $rejected = [int]$_.Exception.Response.StatusCode -eq 400 }
Assert $rejected 'Missing antiforgery token rejected'

$unload = Get-Page '/Meth/ExecuteUnload/3'
Assert ($unload.Content.Contains('Items actually unloaded') -and $unload.Content.Contains('unload-item-3')) 'Unloading form exposes multiple item selection'
$unloadSave = Post-Form '/Meth/ExecuteUnload' @{ '__RequestVerificationToken'=(Token $unload.Content); UnloadId=3; RSTNumber='LAB-TEST-C'; GateManId=4; BagTypeId=1; NumberOfBags=10; ActualLocationId=1; Shift='Day'; SelectedItemIds=@('1','3'); SelectedWorkers=@('3') }
Assert ($unloadSave.BaseResponse.ResponseUri.AbsolutePath -match '/Meth/PrintSlip' -and $unloadSave.Content.Contains('LAB-TEST-C')) 'Unloading form saves items and produces slip'
$afterUnload = Get-Page '/Lab/Create?rstNumber=LAB-TEST-C'
$afterData = [regex]::Match($afterUnload.Content, '<script id="lab-entry-data" type="application/json">(.*?)</script>', 'Singleline').Groups[1].Value | ConvertFrom-Json
Assert (@($afterData.items).Count -eq 2 -and @($afterData.items | Where-Object ItemId -eq 2).Count -eq 0) 'Lab exposes precisely the items saved through unloading form'

$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$worker = Post-Form '/Account/Login' @{ Username='worker-test'; Password='LabTest-Only-2026'; returnUrl='/Lab/Tests' }
Assert ($worker.BaseResponse.ResponseUri.AbsolutePath -eq '/Account/AccessDenied') 'Worker login cannot access lab masters'
Write-Output 'ALL HTTP ASSERTIONS PASSED'
