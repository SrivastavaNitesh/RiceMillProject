$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$map = @{
    'Dashboard'='कार्य सारांश'; 'Admin Dashboard'='प्रशासन कार्य सारांश'
    'Gateman Dashboard'='गेट कार्य सारांश'; 'Weightman Dashboard'='तौल कार्य सारांश'
    'New Gate Inward Form'='नई गेट प्रवेश प्रविष्टि'; 'My Inward Register Report'='मेरा प्रवेश रजिस्टर'
    'Gateman Inward / Outward'='गेट प्रवेश एवं निकासी'; 'Gateman Inward Report'='गेट प्रवेश रिपोर्ट'
    'Weighbridge RST Operations'='धर्मकांटा आरएसटी कार्य'; 'Visitor Register & Passes'='आगंतुक रजिस्टर एवं पास'
    'Temp Tools Register'='अस्थायी औजार रजिस्टर'; 'Security Exception Log'='सुरक्षा घटना रजिस्टर'
    'Supervisor Unloading'='पर्यवेक्षक उतराई कार्य'; 'Lab Quality Check'='प्रयोगशाला गुणवत्ता जाँच'
    'Meth Dashboard'='मेठ कार्य सारांश'; 'Accounts & Billing'='बिल एवं लेखा'
    'Master Data'='मूल जानकारी'; 'Companies & Locations'='कंपनियाँ एवं स्थान'; 'Location Master'='स्थान सूची'
    'Company - Location Mapping'='कंपनी एवं स्थान संबंध'; 'Department Master'='विभाग सूची'; 'Gate Master Setup'='गेट प्रबंधन'
    'DhermKata'='धर्मकांटा'; 'Employee Registration'='कर्मचारी पंजीकरण'; 'Person Registration'='व्यक्ति पंजीकरण'
    'Vehicles'='वाहन सूची'; 'Posts & Roles'='पद एवं भूमिकाएँ'; 'Item Categories'='सामग्री श्रेणियाँ'; 'Items'='सामग्री सूची'
    'Bag Types'='बोरी के प्रकार'; 'Analysis'='विश्लेषण'; 'System Reports'='सिस्टम रिपोर्ट'; 'Database Mapping'='डेटाबेस संबंध'
    'Scroll to Top Menu'='मेन्यू के ऊपर जाएँ'; 'Sign out'='लॉग आउट'; 'Gate Operations'='गेट संचालन'
    'Gate Inward Entry'='गेट प्रवेश प्रविष्टि'; 'Inward Register'='प्रवेश रजिस्टर'; 'Security'='सुरक्षा'
    'Visitor Register'='आगंतुक रजिस्टर'; 'Security Incidents'='सुरक्षा घटनाएँ'; 'Masters'='मूल जानकारी'
    'RST Action Board'='आरएसटी कार्य सूची'; 'InBound Details'='आवक विवरण'
    'Ticket & Unloading Board'='टिकट एवं उतराई कार्य'; 'Labour Mates (Meth)'='श्रमिक प्रमुख (मेठ)'
    'Master Registers'='मूल रजिस्टर'; 'Godowns & Stacks'='गोदाम एवं स्टैक'; 'Labour Workers List'='श्रमिक सूची'
    'Vehicles & Drivers'='वाहन एवं चालक'; 'Reports'='रिपोर्ट'; 'Unloading Reports'='उतराई रिपोर्ट'
    'Work'='कार्य'; 'Assigned Unloading'='सौंपी गई उतराई'; 'My Work Queue'='मेरी कार्य सूची'; 'Unloading Report'='उतराई रिपोर्ट'; 'Account'='खाता'
    'RST billing register'='आरएसटी बिल रजिस्टर'; 'Lab deduction rules'='गुणवत्ता कटौती नियम'; 'Home dashboard'='मुख्य कार्य सारांश'
    'BILLING SECTION'='बिल एवं भुगतान'; 'SIGNED IN AS'='वर्तमान उपयोगकर्ता'; 'Menu'='मेन्यू'; 'Accounts'='लेखा'
    'Admin billing workspace'='प्रशासन बिल प्रबंधन'; 'WORKSPACE'='कार्य क्षेत्र'; 'CONFIGURATION'='सेटिंग्स'
    'RST quality checks'='आरएसटी गुणवत्ता जाँच'; 'Lab reports'='प्रयोगशाला रिपोर्ट'; 'Test master'='जाँच सूची'; 'Item test mapping'='सामग्री अनुसार जाँच'
    'Quality control'='गुणवत्ता नियंत्रण'; 'Laboratory workspace'='प्रयोगशाला कार्य क्षेत्र'; 'Skip to content'='मुख्य सामग्री पर जाएँ'
    'Sign out &rarr;'='लॉग आउट &rarr;'; 'Home'='मुख्य पृष्ठ'; 'Operations'='संचालन'; 'Quality'='गुणवत्ता'; 'About'='हमारे बारे में'
    'Employee Sign In'='कर्मचारी लॉगिन'; 'Back to home'='मुख्य पृष्ठ पर लौटें'; 'About our workspace'='हमारे कार्य क्षेत्र के बारे में'; 'Quick Links'='त्वरित लिंक'
}
$records = @()
$backup = Join-Path $PSScriptRoot 'hindi-navigation-rollback.json'
if (Test-Path $backup) { throw 'Rollback record already exists. Do not overwrite it.' }
foreach ($file in Get-ChildItem (Join-Path $root 'Views/Shared') -Filter '_Layout*.cshtml') {
    $before = [IO.File]::ReadAllText($file.FullName)
    # Only exact HTML text nodes are translated; Razor expressions and attributes stay intact.
    $after = [regex]::Replace($before, '(?<=>)(\s*)([^<>]+?)(\s*)(?=<)', {
        param($m)
        $label = $m.Groups[2].Value
        if ($map.ContainsKey($label)) { return $m.Groups[1].Value + $map[$label] + $m.Groups[3].Value }
        return $m.Value
    })
    if ($after -cne $before) {
        $records += @{ Path = "Views/Shared/$($file.Name)"; Before = $before; After = $after }
        [IO.File]::WriteAllText($file.FullName, $after, [Text.UTF8Encoding]::new($false))
    }
}
$records | ConvertTo-Json -Depth 4 | Set-Content $backup -Encoding utf8
Write-Output "Translated $($records.Count) layouts. Rollback snapshot: $backup"
