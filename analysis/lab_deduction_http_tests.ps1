$ErrorActionPreference='Stop'
$base='http://localhost:5038';$session=New-Object Microsoft.PowerShell.Commands.WebRequestSession
function GetPage($path){Invoke-WebRequest -UseBasicParsing ($base+$path) -WebSession $session}
function Token($html){[Net.WebUtility]::HtmlDecode([regex]::Match($html,'name="__RequestVerificationToken"[^>]*value="([^"]+)"').Groups[1].Value)}
function Post($path,$fields){Invoke-WebRequest -UseBasicParsing ($base+$path) -WebSession $session -Method Post -Body $fields}
function Check($ok,$label){if(-not $ok){throw $label};Write-Output "PASS: $label"}
$null=Post '/Account/Login' @{Username='lab-test';Password='LabTest-Only-2026';returnUrl='/Lab/Tests'}
$page=GetPage '/Lab/Tests'
Check ($page.Content.Contains('Quintal') -and $page.Content.Contains('Kilogram')) 'Unit dropdown loads master names'
Check ([regex]::IsMatch($page.Content,'<option[^>]*selected="selected"[^>]*value="1"|<option[^>]*value="1"[^>]*selected="selected"')) 'New test defaults to Q'
$name='Deduction check '+[Guid]::NewGuid().ToString('N').Substring(0,8)
$fields=@{'__RequestVerificationToken'=(Token $page.Content);'Form.TestId'=0;'Form.TestName'=$name;'Form.ResultType'='Number';'Form.Unit'='%';'Form.Deducations'=123;'Form.UnitId'=2}
$page=Post '/Lab/Tests' $fields
[IO.File]::WriteAllText((Join-Path $PSScriptRoot '../artifacts/lab-master-response.html'),$page.Content)
$row=[regex]::Match($page.Content,'(?s)<tr>\s*<td>'+[regex]::Escape($name)+'.*?</tr>').Value
Check ($row.Contains('<td>123</td>') -and $row.Contains('Kilogram')) 'Saved deduction and unit name appear in list'
$id=[regex]::Match($row,'/Lab/Tests(?:/|\?id=)(\d+)').Groups[1].Value
Check ([bool]$id) 'Edit link targets saved test'
$edit=GetPage ('/Lab/Tests/'+$id)
Check ([regex]::IsMatch($edit.Content,'name="Form.Deducations"[^>]*value="123"')) 'Edit reloads saved deduction'
Check ([regex]::IsMatch($edit.Content,'<option[^>]*selected="selected"[^>]*value="2"|<option[^>]*value="2"[^>]*selected="selected"')) 'Edit selects saved non-default unit'
$fields['Form.TestId']=$id;$fields['Form.Deducations']=456;$fields['Form.UnitId']=1;$fields['__RequestVerificationToken']=Token $edit.Content
$page=Post '/Lab/Tests' $fields
[IO.File]::WriteAllText((Join-Path $PSScriptRoot '../artifacts/lab-master-response.html'),$page.Content)
$row=[regex]::Match($page.Content,'(?s)<tr>\s*<td>'+[regex]::Escape($name)+'.*?</tr>').Value
Check ($row.Contains('<td>456</td>') -and $row.Contains('Quintal')) 'Edit persists changed deduction and unit'
$fields['Form.Deducations']=1000;$fields['Form.UnitId']=2;$fields['__RequestVerificationToken']=Token $page.Content
$bad=Post '/Lab/Tests' $fields
Check ($bad.Content.Contains('Deduction must be between 0 and 999.') -and [regex]::IsMatch($bad.Content,'<option[^>]*selected="selected"[^>]*value="2"|<option[^>]*value="2"[^>]*selected="selected"')) 'Invalid deduction rejected while posted unit is retained'
$edit=GetPage ('/Lab/Tests/'+$id)
Check ([regex]::IsMatch($edit.Content,'name="Form.Deducations"[^>]*value="456"')) 'Rejected edit leaves saved deduction unchanged'
$fields['Form.Deducations']='';$fields['Form.UnitId']='';$fields['__RequestVerificationToken']=Token $edit.Content
$page=Post '/Lab/Tests' $fields
Check ($page.Content.Contains($name)) 'Test without optional deduction unit remains visible'
$edit=GetPage ('/Lab/Tests/'+$id)
Check (-not [regex]::IsMatch($edit.Content,'<option[^>]*value="1"[^>]*selected="selected"|<option[^>]*selected="selected"[^>]*value="1"')) 'Editing empty unit does not silently replace it with Q'
$page=Post '/Lab/RemoveTest' @{__RequestVerificationToken=(Token $edit.Content);id=$id}
Check (-not $page.Content.Contains($name) -and $page.Content.Contains('Saved reports are retained')) 'Synthetic test soft-remove works'
[IO.File]::WriteAllText((Join-Path $PSScriptRoot '../artifacts/deduction-test-id.txt'),$id)
'ALL LAB MASTER CHECKS PASSED'