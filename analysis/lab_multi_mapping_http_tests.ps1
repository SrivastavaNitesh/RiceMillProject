param([int]$ItemId=5)
$ErrorActionPreference='Stop';$base='http://localhost:5039';$session=New-Object Microsoft.PowerShell.Commands.WebRequestSession
function GetPage($path){Invoke-WebRequest -UseBasicParsing ($base+$path) -WebSession $session}
function Token($html){[Net.WebUtility]::HtmlDecode([regex]::Match($html,'name="__RequestVerificationToken"[^>]*value="([^"]+)"').Groups[1].Value)}
function Post($path,$fields){$pairs=foreach($key in $fields.Keys){foreach($value in @($fields[$key])){[Uri]::EscapeDataString($key)+'='+[Uri]::EscapeDataString([string]$value)}};Invoke-WebRequest -UseBasicParsing ($base+$path) -WebSession $session -Method Post -ContentType 'application/x-www-form-urlencoded' -Body ($pairs -join '&')}
function Check($ok,$label){if(-not $ok){throw $label};Write-Output "PASS: $label"}
function Row($html){[regex]::Match($html,'(?s)<tr><td>[^<]*</td><td>Multi-test mapping fixture</td>.*?</tr>').Value}
function EditId($html){[regex]::Match((Row $html),'/Lab/Mappings(?:/|\?id=)(\d+)').Groups[1].Value}
function Selected($html){@([regex]::Matches($html,'<input[^>]*class="form-check-input mapping-test"[^>]*>').Value | Where-Object {$_ -match 'checked="checked"'}).Count}
$null=Post '/Account/Login' @{Username='lab-test';Password='LabTest-Only-2026';returnUrl='/Lab/Mappings'}
$page=GetPage '/Lab/Mappings'
$fields=@{__RequestVerificationToken=(Token $page.Content);'Form.CategoryId'=1;'Form.ItemId'=$ItemId;'Form.SelectedTestIds'=@(1,3)}
$page=Post '/Lab/Mappings' $fields
Check ($page.Content.Contains('Item test selections saved.') -and (Row $page.Content).Contains('Moisture') -and (Row $page.Content).Contains('Observation')) 'Two tests saved in one item group'
$id=EditId $page.Content;$edit=GetPage ('/Lab/Mappings/'+$id)
Check ((Selected $edit.Content) -eq 2) 'Edit preselects all mapped tests'
[IO.File]::WriteAllText((Join-Path $PSScriptRoot '../artifacts/mapping-edit.html'),$edit.Content)
$fields['Form.OriginalTestIds']=@(1,3);$fields['Form.SelectedTestIds']=@(3);$fields['__RequestVerificationToken']=Token $edit.Content
$page=Post '/Lab/Mappings' $fields
Check ((Row $page.Content).Contains('Observation') -and -not (Row $page.Content).Contains('Moisture')) 'Unselecting a test removes only that active mapping'
$stale=$fields.Clone();$stale['Form.SelectedTestIds']=@(1,3);$stale['__RequestVerificationToken']=Token $page.Content
$bad=Post '/Lab/Mappings' $stale
Check ($bad.Content.Contains('changed since this form was opened')) 'Stale form cannot overwrite newer mappings'
$id=EditId $page.Content;$edit=GetPage ('/Lab/Mappings/'+$id)
$fields['Form.OriginalTestIds']=@(3);$fields['Form.SelectedTestIds']=@(1,3);$fields['__RequestVerificationToken']=Token $edit.Content
$page=Post '/Lab/Mappings' $fields
Check ((Row $page.Content).Contains('Observation') -and (Row $page.Content).Contains('Moisture')) 'Editing can add a test back alongside retained tests'
$fields['Form.OriginalTestIds']=@(1,3);$fields['Form.SelectedTestIds']=@(3,999999);$fields['__RequestVerificationToken']=Token $page.Content
$bad=Post '/Lab/Mappings' $fields
Check ($bad.Content.Contains('Select active tests only')) 'Invalid test rejected atomically'
$page=GetPage '/Lab/Mappings';$edit=GetPage ('/Lab/Mappings/'+(EditId $page.Content))
Check ((Selected $edit.Content) -eq 2) 'Failed save retains previous complete mapping set'
$fields['Form.SelectedTestIds']=@(1,3);$fields['Form.CategoryId']=2;$fields['__RequestVerificationToken']=Token $edit.Content
$bad=Post '/Lab/Mappings' $fields
Check ($bad.Content.Contains('selected category')) 'Wrong category/item pair rejected'
$fields['Form.CategoryId']=1;$fields.Remove('Form.SelectedTestIds');$fields['__RequestVerificationToken']=Token $bad.Content
$page=Post '/Lab/Mappings' $fields
Check (-not (Row $page.Content)) 'Unselecting all tests clears only that item group'
$fields['Form.OriginalTestIds']=@();$fields['Form.SelectedTestIds']=@(1,3);$fields['__RequestVerificationToken']=Token $page.Content
$page=Post '/Lab/Mappings' $fields
Check ($page.Content.Contains('Item test selections saved.')) 'Item can be mapped again after clearing'
$fields.Remove('__RequestVerificationToken');$rejected=$false
try{$null=Post '/Lab/Mappings' $fields}catch{$rejected=[int]$_.Exception.Response.StatusCode -eq 400}
Check $rejected 'Missing antiforgery token rejected'
'ALL MULTI-MAPPING CHECKS PASSED'