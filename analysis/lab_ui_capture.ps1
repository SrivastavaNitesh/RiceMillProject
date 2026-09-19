$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$taskSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$null = Invoke-WebRequest -UseBasicParsing -Uri 'http://localhost:5033/Account/Login' -WebSession $taskSession -Method Post -Body @{Username='lab-test';Password='LabTest-Only-2026';returnUrl='/Lab/Index'}
$page = Invoke-WebRequest -UseBasicParsing -Uri 'http://localhost:5033/Lab/Create?rstNumber=LAB-TEST-A' -WebSession $taskSession
$html = $page.Content
$html = [regex]::Replace($html, '(?s)<link[^>]+href="https://[^>]+>', '')
$html = [regex]::Replace($html, '(src|href)="/([^"?]+)(?:\?[^"]*)?"', {
 param($match)
 $assetPath = Join-Path (Join-Path $taskRoot 'wwwroot') $match.Groups[2].Value
 if (Test-Path -LiteralPath $assetPath -PathType Leaf) { $match.Groups[1].Value+'="'+([Uri]$assetPath).AbsoluteUri+'"' } else { $match.Value }
})
$checks = @'
<script>
try {
 const out=[]; const check=(value,name)=>{if(!value)throw new Error(name);out.push('PASS: '+name);};
 const categories=[...document.querySelectorAll('.lab-category')];
 const choose=(input,on)=>{input.checked=on;input.dispatchEvent(new Event('change',{bubbles:true}));};
 const result=(id,test)=>document.getElementById('result-'+id+'-'+test);
 check(categories.length===2,'Only unloaded categories');
 check(document.querySelectorAll('#lab-items input').length===0,'No unrelated default items');
 choose(categories.find(c=>c.value==='1'),true);
 check(document.querySelectorAll('#lab-items input').length===2,'Paddy category shows two unloaded items');
 choose(document.getElementById('item-1'),true);
 check(document.querySelectorAll('#lab-results input:not([type=hidden])').length===2,'Item selection displays its two mapped tests');
 result(1,1).value='12.25';result(1,1).dispatchEvent(new Event('input'));
 result(1,3).value='Good';result(1,3).dispatchEvent(new Event('input'));
 choose(categories.find(c=>c.value==='2'),true);
 choose(document.getElementById('item-3'),true);
 check(result(1,1).value==='12.25','Existing values survive another category/item selection');
 check(document.querySelectorAll('#lab-results input:not([type=hidden])').length===3,'Multiple category/item tests appear together');
 result(3,3).value='Clean';result(3,3).dispatchEvent(new Event('input'));
 const formData=new FormData(document.getElementById('lab-entry-form'));
 check(formData.getAll('SelectedItemIds').length===2 && formData.getAll('SelectedCategoryIds').length===2,'Multiple selection submits all IDs');
 const bagKey=[...formData.keys()].find(k=>k.endsWith('.ItemId') && formData.get(k)==='3');
 check(bagKey && formData.get(bagKey.replace('.ItemId','.Value'))==='Clean','Results bind to correct item/test');
 choose(categories.find(c=>c.value==='1'),false);
 check(!document.getElementById('item-1') && !result(1,1),'Removing category removes its item selection/results');
 choose(categories.find(c=>c.value==='1'),true); choose(document.getElementById('item-1'),true);
 check(result(1,1).value==='12.25','Reselection preserves entered values');
 check(!document.getElementById('save-lab').disabled,'Complete mapped selections enable save');
 document.body.dataset.uiTests='PASS';
 const pre=document.createElement('pre');pre.id='ui-test-results';pre.textContent=out.join('\n');document.body.append(pre);
} catch(error) {document.body.dataset.uiTests='FAIL';const pre=document.createElement('pre');pre.id='ui-test-results';pre.textContent=error.stack;document.body.append(pre);}
</script>
'@
$html = $html.Replace('</body>', $checks+'</body>')
Set-Content -LiteralPath (Join-Path $PSScriptRoot 'lab-ui-check.html') -Value $html -Encoding UTF8
Write-Output 'Saved synthetic rendered UI with local assets and selection assertions.'
