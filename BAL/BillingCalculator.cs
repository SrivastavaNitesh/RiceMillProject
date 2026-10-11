using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RiceMillProject.Models;

namespace RiceMillProject.BAL;

public static class BillingCalculator
{
    public static readonly IReadOnlyDictionary<string,string> Modes = new Dictionary<string,string>
    {
        ["None"]="कोई कटौती नहीं", ["Percent"]="आइटम राशि का प्रतिशत",
        ["PerQuintal"]="प्रति क्विंटल राशि", ["PerKg"]="प्रति किलो राशि", ["PerBag"]="प्रति बोरी राशि", ["Fixed"]="प्रति आइटम तय राशि"
    };
    public static decimal Money(decimal n) => decimal.Round(n,2,MidpointRounding.AwayFromZero);
    public static decimal Kg(decimal n) => decimal.Round(n,3,MidpointRounding.AwayFromZero);
    public static string Version(BillingSource source) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(source))));
    public static BillingForm NewForm(BillingSource source)
    {
        var form=new BillingForm { RSTNumber=source.Header.RSTNumber, SourceVersion=Version(source),
            WeightMode=source.Lots.Select(l=>l.ItemId).Distinct().Count()==1 ? "FinalNet" : "ByBags", PalledariAmount=0 };
        form.Items=source.Lots.GroupBy(l=>l.ItemId).Select(g=>new BillingItemInput {ItemId=g.Key}).ToList();
        form.Workers=source.Workers.Select(w=>new BillingWorkerInput {AllocationId=w.AllocationId,Rate=w.Rate}).ToList();
        foreach(var lot in source.Lots)
        {
            var locations=source.Locations.Where(l=>l.UnloadId==lot.UnloadId && (!lot.LocationId.HasValue || l.LocationId==lot.LocationId)).ToList();
            foreach(var location in locations) form.Allocations.Add(new() {DetailId=lot.DetailId,LocationId=location.LocationId,BagCount=locations.Count==1?lot.BagCount:0});
        }
        return form;
    }
    public static BillingCalculation Calculate(BillingSource source,BillingForm form,bool requirePrices=false)
    {
        var result=new BillingCalculation {Source=source,Notes=form.Notes,GstPercent=form.GstPercent};
        var issues=result.Issues;
        var groups=source.Lots.GroupBy(l=>l.ItemId).ToList();
        var net=source.Header.NetWeight??0;
        var totalBags=source.Lots.Sum(l=>(long)l.BagCount);
        if(net<=0 || source.Header.TareWeight is null) issues.Add("खाली गाड़ी का वजन और माल का सही नेट वजन दर्ज करें।");
        if(source.Header.TareWeight.HasValue && Math.Abs(source.Header.GrossWeight-source.Header.TareWeight.Value-net)>0.005m) issues.Add("भरी गाड़ी का वजन घटा खाली गाड़ी का वजन, दर्ज नेट वजन से नहीं मिल रहा।");
        if(groups.Count==0 || totalBags<=0) issues.Add("सुपरवाइजर के आइटम और बोरी विवरण दर्ज करें।");
        if(form.Items.Select(i=>i.ItemId).Distinct().Count()!=form.Items.Count || !form.Items.Select(i=>i.ItemId).Order().SequenceEqual(groups.Select(g=>g.Key).Order()))
            issues.Add("आइटम विवरण बदल गए हैं। बिल फॉर्म दोबारा खोलें।");
        if(form.Workers.Select(w=>w.AllocationId).Distinct().Count()!=form.Workers.Count || !form.Workers.Select(w=>w.AllocationId).Order().SequenceEqual(source.Workers.Select(w=>w.AllocationId).Order()))
            issues.Add("मेठ का रिकॉर्ड बदल गया है। बिल फॉर्म दोबारा खोलें।");
        // Only source records determine weights; posted mode/weights are never authoritative.
        var known=source.Lots.Where(l=>l.ActualWeightKg.HasValue).ToList();
        var unknown=source.Lots.Where(l=>!l.ActualWeightKg.HasValue).ToList();
        if(known.Any(l=>l.ActualWeightKg<=0 || l.ActualWeightKg>999999999999m || Kg(l.ActualWeightKg!.Value)!=l.ActualWeightKg))
            issues.Add("दर्ज वास्तविक वजन सही और अधिकतम तीन दशमलव में होना चाहिए।");
        var knownKg=known.Sum(l=>l.ActualWeightKg!.Value);
        var remainingKg=Kg(net)-knownKg;
        var remainingBags=unknown.Sum(l=>(long)l.BagCount);
        if(knownKg>Kg(net) || (unknown.Count==0 && knownKg!=Kg(net)) || (unknown.Count>0 && remainingKg<=0))
            issues.Add("दर्ज वास्तविक वजन RST के नेट वजन से मेल नहीं खाता।");
        result.WeightMode=groups.Count==1 ? "FinalNet" : unknown.Count==0 ? "Measured" : "ByBags";
        var unknownGroups=groups.Where(g=>g.Any(l=>!l.ActualWeightKg.HasValue)).Select(g=>g.Key).ToList();
        if(form.Allocations.GroupBy(a=>(a.DetailId,a.LocationId)).Any(g=>g.Count()>1) ||
           form.Allocations.Any(a=>a.BagCount<0 || !source.Lots.Any(l=>l.DetailId==a.DetailId && source.Locations.Any(loc=>loc.UnloadId==l.UnloadId && loc.LocationId==a.LocationId))))
            issues.Add("आइटम के स्थान का विवरण सही नहीं है। फॉर्म दोबारा खोलें।");
        decimal assigned=0;
        for(var index=0;index<groups.Count;index++)
        {
            var group=groups[index];var first=group.First();
            var input=form.Items.FirstOrDefault(i=>i.ItemId==group.Key)??new BillingItemInput();
            var bags=group.Sum(l=>l.BagCount);
            var itemKnown=group.Where(l=>l.ActualWeightKg.HasValue).Sum(l=>l.ActualWeightKg!.Value);
            var itemUnknownBags=group.Where(l=>!l.ActualWeightKg.HasValue).Sum(l=>(long)l.BagCount);
            var share=itemUnknownBags>0 && remainingBags>0
                ? group.Key==unknownGroups.Last() ? remainingKg-assigned : Kg(remainingKg*itemUnknownBags/remainingBags) : 0;
            assigned+=share;
            var gross=Kg(itemKnown+share);
            var estimated=groups.Count>1 && group.Any(l=>!l.ActualWeightKg.HasValue);
            var bagKg=Kg(group.Sum(l=>(decimal)l.BagCount*l.DeductionWeightGrams)/1000m);
            var payable=Kg(gross-bagKg);
            if(payable<=0 || bagKg<0) issues.Add($"{first.ItemName}: बोरी की कटौती आइटम के वजन से कम होनी चाहिए।");
            if(input.Rate<0 || input.Rate>99999999.99m || Money(input.Rate)!=input.Rate || (requirePrices && input.Rate<=0)) issues.Add($"{first.ItemName}: दर शून्य से अधिक और अधिकतम दो दशमलव में भरें।");
            if(input.RateBasis is not ("Kg" or "Quintal" or "Bag")) issues.Add($"{first.ItemName}: दर की सही इकाई चुनें।");
            var qty=input.RateBasis=="Bag" ? bags : input.RateBasis=="Kg" ? Math.Max(0,payable) : Math.Max(0,payable)/100m;
            var line=new BillingItemLine {IsEstimatedWeight=estimated,CategoryId=first.CategoryId,CategoryName=first.CategoryName,ItemId=group.Key,ItemName=first.ItemName,Bags=bags,GrossKg=gross,BagDeductionKg=bagKg,PayableKg=payable,Rate=input.Rate,RateBasis=input.RateBasis,BaseAmount=Money(qty*Math.Clamp(input.Rate,0,99999999.99m))};
            var labs=source.LabResults.Where(l=>l.ItemId==group.Key).ToList();
            foreach(var lab in labs)
            {
                if(string.IsNullOrWhiteSpace(lab.ResultValue)){issues.Add($"{first.ItemName}: {lab.TestName} का परिणाम भरना बाकी है।");continue;}
                if(lab.RuleId is null)
                {
                    if(lab.MasterDeduction.GetValueOrDefault()>0) issues.Add($"{first.ItemName}: {lab.TestName} की कटौती सीमा और दर सेट करें।");
                    else line.DeductionNotes.Add($"{lab.TestName}: कोई कटौती नहीं।");
                    continue;
                }
                if(!Modes.ContainsKey(lab.DeductionMode)){issues.Add($"{lab.TestName}: कटौती का सही नियम चुनें।");continue;}
                if(lab.DeductionMode=="None"){line.DeductionNotes.Add($"{lab.TestName}: कोई कटौती नहीं।");continue;}
                if(!decimal.TryParse(lab.ResultValue.Trim().TrimEnd('%'),NumberStyles.AllowLeadingSign|NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out var value))
                {issues.Add($"{first.ItemName}: {lab.TestName} की कटौती के लिए परिणाम संख्या में होना चाहिए।");continue;}
                decimal deduction=0;
                if(value>=lab.Threshold)
                    deduction=Money(lab.DeductionMode switch
                    {
                        "Percent"=>line.BaseAmount*lab.DeductionValue/100m,
                        "PerQuintal"=>Math.Max(0,payable)/100m*lab.DeductionValue,
                        "PerKg"=>Math.Max(0,payable)*lab.DeductionValue,
                        "PerBag"=>bags*lab.DeductionValue,
                        "Fixed"=>lab.DeductionValue,_=>0
                    });
                line.LabDeductionAmount+=deduction;
                line.DeductionNotes.Add($"{lab.TestName}: परिणाम {lab.ResultValue}; सीमा {lab.Threshold}; {Modes[lab.DeductionMode]} {lab.DeductionValue}; कटौती ₹{deduction:0.00}।");
            }
            if(line.LabDeductionAmount>line.BaseAmount && input.Rate>0) issues.Add($"{first.ItemName}: लैब कटौती आइटम की राशि से अधिक है।");
            line.Amount=Money(line.BaseAmount-line.LabDeductionAmount);
            var allocations=new List<(int LocationId,string Name,int Count)>();
            foreach(var lot in group)
            {
                if(lot.LocationId.HasValue)
                {
                    allocations.Add((lot.LocationId.Value,source.Locations.FirstOrDefault(l=>l.UnloadId==lot.UnloadId && l.LocationId==lot.LocationId)?.LocationName??"दर्ज स्थान",lot.BagCount));
                    continue;
                }
                var rows=form.Allocations.Where(a=>a.DetailId==lot.DetailId).ToList();
                if(rows.Sum(a=>(long)a.BagCount)!=lot.BagCount)
                {
                    allocations.Add((0,"स्थान दर्ज नहीं",lot.BagCount));continue;
                }
                foreach(var row in rows.Where(a=>a.BagCount>0))
                    allocations.Add((row.LocationId,source.Locations.FirstOrDefault(l=>l.UnloadId==lot.UnloadId && l.LocationId==row.LocationId)?.LocationName??"Unknown",row.BagCount));
            }
            var locationsGrouped=allocations.GroupBy(a=>a.LocationId).ToList();decimal locationKg=0;
            for(var n=0;n<locationsGrouped.Count;n++)
            {
                var location=locationsGrouped[n];var count=location.Sum(l=>l.Count);
                var weight=n==locationsGrouped.Count-1 ? gross-locationKg : bags>0?Kg(gross*count/bags):0;
                locationKg+=weight;line.Locations.Add(new(){LocationId=location.Key,LocationName=location.First().Name,Bags=count,WeightKg=weight});
            }
            result.Items.Add(line);
        }
        foreach(var worker in source.Workers)
        {
            var input=form.Workers.FirstOrDefault(w=>w.AllocationId==worker.AllocationId);
            var rate=input?.Rate??0;
            if(!form.PalledariAmount.HasValue && (rate<0 || rate>999999.99m || Money(rate)!=rate)) issues.Add($"{worker.WorkerName}: दर शून्य या अधिक और अधिकतम दो दशमलव में भरें।");
            result.Workers.Add(new(){AllocationId=worker.AllocationId,UnloadId=worker.UnloadId,WorkerId=worker.WorkerId,WorkerName=worker.WorkerName,WorkType=worker.WorkType,BagTypeId=worker.BagTypeId,BagTypeName=worker.BagTypeName,BagCount=worker.BagCount,Rate=rate,Amount=Money(Math.Clamp(rate,0,999999.99m)*worker.BagCount)});
        }
        if(form.GstPercent<0 || form.GstPercent>100 || Money(form.GstPercent)!=form.GstPercent) issues.Add("GST 0 से 100 के बीच और अधिकतम दो दशमलव में भरें।");
        result.ItemAmount=result.Items.Sum(i=>i.BaseAmount);
        result.LabDeduction=result.Items.Sum(i=>i.LabDeductionAmount);
        if(form.PalledariAmount is <0 or >999999999.99m || (form.PalledariAmount.HasValue && Money(form.PalledariAmount.Value)!=form.PalledariAmount))
            issues.Add("पल्लेदारी राशि 0 या उससे अधिक और अधिकतम दो दशमलव में भरें।");
        result.PalledariAmount=form.PalledariAmount;
        result.WorkerAmount=form.PalledariAmount.HasValue ? Math.Clamp(form.PalledariAmount.Value,0,999999999.99m) : result.Workers.Sum(w=>w.Amount);
        result.Subtotal=Money(result.ItemAmount-result.LabDeduction+result.WorkerAmount);
        result.GstAmount=Money(result.Subtotal*Math.Clamp(form.GstPercent,0,100)/100m);
        result.Total=Money(result.Subtotal+result.GstAmount);
        if((requirePrices && result.Total<0) || result.Total>999999999999.99m) issues.Add("बिल की कुल राशि मान्य सीमा में नहीं है।");
        result.Issues=issues.Distinct().ToList();
        return result;
    }
}
