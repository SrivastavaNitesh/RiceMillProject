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
        ["None"]="No deduction", ["Percent"]="Percent of item amount",
        ["PerQuintal"]="Amount per quintal", ["PerKg"]="Amount per kg", ["PerBag"]="Amount per bag", ["Fixed"]="Fixed amount per item"
    };
    public static decimal Money(decimal n) => decimal.Round(n,2,MidpointRounding.AwayFromZero);
    public static decimal Kg(decimal n) => decimal.Round(n,3,MidpointRounding.AwayFromZero);
    public static string Version(BillingSource source) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(source))));
    public static BillingForm NewForm(BillingSource source)
    {
        var form=new BillingForm { RSTNumber=source.Header.RSTNumber, SourceVersion=Version(source) };
        form.Items=source.Lots.GroupBy(l=>l.ItemId).Select(g=>new BillingItemInput {ItemId=g.Key}).ToList();
        form.Workers=source.Workers.Select(w=>new BillingWorkerInput {AllocationId=w.AllocationId,Rate=w.Rate}).ToList();
        foreach(var lot in source.Lots)
        {
            var locations=source.Locations.Where(l=>l.UnloadId==lot.UnloadId).ToList();
            foreach(var location in locations) form.Allocations.Add(new() {DetailId=lot.DetailId,LocationId=location.LocationId,BagCount=locations.Count==1?lot.BagCount:0});
        }
        return form;
    }
    public static BillingCalculation Calculate(BillingSource source,BillingForm form,bool requirePrices=false)
    {
        var result=new BillingCalculation {Source=source,WeightMode=form.WeightMode,Notes=form.Notes,GstPercent=form.GstPercent};
        var issues=result.Issues;
        var groups=source.Lots.GroupBy(l=>l.ItemId).ToList();
        var net=source.Header.NetWeight??0;
        var totalBags=source.Lots.Sum(l=>(long)l.BagCount);
        if(net<=0 || source.Header.TareWeight is null) issues.Add("Final tare and positive net weight are required.");
        if(source.Header.TareWeight.HasValue && Math.Abs(source.Header.GrossWeight-source.Header.TareWeight.Value-net)>0.005m) issues.Add("Gross minus tare does not match the recorded net weight.");
        if(groups.Count==0 || totalBags<=0) issues.Add("Supervisor item and bag details are not complete.");
        if(!source.Header.LabComplete) issues.Add("All item lab tests must be complete before bill generation.");
        if(source.UnloadStatuses.Any(s=>s is not ("Unloaded" or "Verified"))) issues.Add("Some unloading work is still pending.");
        foreach(var unload in source.Lots.GroupBy(l=>l.UnloadId))
        {
            if(unload.Sum(l=>(long)l.BagCount)!=unload.First().SupervisorTotalBags || unload.First().MethTotalBags!=unload.First().SupervisorTotalBags)
                issues.Add($"Supervisor and Meth bag counts do not reconcile for unloading {unload.Key}.");
            if(source.Workers.Where(w=>w.UnloadId==unload.Key).Sum(w=>(long)w.BagCount)!=unload.First().MethTotalBags)
                issues.Add($"Worker bag counts do not reconcile for unloading {unload.Key}.");
        }
        if(form.Items.Select(i=>i.ItemId).Distinct().Count()!=form.Items.Count || !form.Items.Select(i=>i.ItemId).Order().SequenceEqual(groups.Select(g=>g.Key).Order()))
            issues.Add("Item selections changed. Reload the billing form.");
        if(form.Workers.Select(w=>w.AllocationId).Distinct().Count()!=form.Workers.Count || !form.Workers.Select(w=>w.AllocationId).Order().SequenceEqual(source.Workers.Select(w=>w.AllocationId).Order()))
            issues.Add("Worker allocations changed. Reload the billing form.");
        if(form.WeightMode is not ("ByBags" or "Measured")) issues.Add("Select a valid weight allocation method.");
        if(form.WeightMode=="Measured" && (form.Items.Any(i=>i.MeasuredWeight is null or <=0 || i.MeasuredWeight>999999999999m || Kg(i.MeasuredWeight.Value)!=i.MeasuredWeight) || form.Items.Sum(i=>i.MeasuredWeight??0)!=Kg(net)))
            issues.Add("Enter every item's measured kg to 3 decimals; their sum must equal net weight.");
        if(form.Allocations.GroupBy(a=>(a.DetailId,a.LocationId)).Any(g=>g.Count()>1) ||
           form.Allocations.Any(a=>a.BagCount<0 || !source.Lots.Any(l=>l.DetailId==a.DetailId && source.Locations.Any(loc=>loc.UnloadId==l.UnloadId && loc.LocationId==a.LocationId))))
            issues.Add("Invalid or duplicate item-location allocation.");
        decimal assigned=0;
        for(var index=0;index<groups.Count;index++)
        {
            var group=groups[index];var first=group.First();
            var input=form.Items.FirstOrDefault(i=>i.ItemId==group.Key)??new BillingItemInput();
            var bags=group.Sum(l=>l.BagCount);
            var gross=form.WeightMode=="Measured" ? Math.Clamp(input.MeasuredWeight??0,0,999999999999m) : totalBags>0 ? index==groups.Count-1 ? Kg(net)-assigned : Kg(net*bags/totalBags) : 0;
            assigned+=gross;
            var bagKg=Kg(group.Sum(l=>(decimal)l.BagCount*l.DeductionWeightGrams)/1000m);
            var payable=Kg(gross-bagKg);
            if(payable<=0 || bagKg<0) issues.Add($"{first.ItemName}: bag deduction must be less than allocated weight.");
            if(input.Rate<0 || input.Rate>99999999.99m || Money(input.Rate)!=input.Rate || (requirePrices && input.Rate<=0)) issues.Add($"{first.ItemName}: enter a positive rate with at most 2 decimals.");
            if(input.RateBasis is not ("Kg" or "Quintal" or "Bag")) issues.Add($"{first.ItemName}: invalid rate basis.");
            var qty=input.RateBasis=="Bag" ? bags : input.RateBasis=="Kg" ? Math.Max(0,payable) : Math.Max(0,payable)/100m;
            var line=new BillingItemLine {CategoryId=first.CategoryId,CategoryName=first.CategoryName,ItemId=group.Key,ItemName=first.ItemName,Bags=bags,GrossKg=gross,BagDeductionKg=bagKg,PayableKg=payable,Rate=input.Rate,RateBasis=input.RateBasis,BaseAmount=Money(qty*Math.Clamp(input.Rate,0,99999999.99m))};
            var labs=source.LabResults.Where(l=>l.ItemId==group.Key).ToList();
            if(labs.Count==0) issues.Add($"{first.ItemName}: no lab tests mapped.");
            foreach(var lab in labs)
            {
                if(lab.ResultValue is null){issues.Add($"{first.ItemName}: {lab.TestName} result is missing.");continue;}
                if(lab.RuleId is null)
                {
                    if(lab.MasterDeduction.GetValueOrDefault()>0) issues.Add($"{first.ItemName}: configure the numeric billing rule for {lab.TestName}.");
                    else line.DeductionNotes.Add($"{lab.TestName}: no master deduction.");
                    continue;
                }
                if(!Modes.ContainsKey(lab.DeductionMode)){issues.Add($"{lab.TestName}: invalid deduction rule.");continue;}
                if(lab.DeductionMode=="None"){line.DeductionNotes.Add($"{lab.TestName}: no deduction.");continue;}
                if(!decimal.TryParse(lab.ResultValue.Trim().TrimEnd('%'),NumberStyles.AllowLeadingSign|NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out var value))
                {issues.Add($"{first.ItemName}: {lab.TestName} needs a numeric lab result for its billing rule.");continue;}
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
                line.DeductionNotes.Add($"{lab.TestName}: result {lab.ResultValue}; limit {lab.Threshold}; {Modes[lab.DeductionMode]} {lab.DeductionValue}; deduction {deduction:0.00}.");
            }
            if(line.LabDeductionAmount>line.BaseAmount) issues.Add($"{first.ItemName}: lab deduction exceeds item amount.");
            line.Amount=Money(line.BaseAmount-line.LabDeductionAmount);
            var allocations=new List<(int LocationId,string Name,int Count)>();
            foreach(var lot in group)
            {
                var rows=form.Allocations.Where(a=>a.DetailId==lot.DetailId).ToList();
                if(rows.Sum(a=>(long)a.BagCount)!=lot.BagCount)
                {
                    issues.Add($"{first.ItemName}, {lot.BagTypeName}: allocate all {lot.BagCount} bags to unloading locations.");
                    allocations.Add((0,"Allocation pending",lot.BagCount));continue;
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
            if(rate<0 || rate>999999.99m || Money(rate)!=rate) issues.Add($"{worker.WorkerName}: worker rate must be a non-negative amount with at most 2 decimals.");
            result.Workers.Add(new(){AllocationId=worker.AllocationId,UnloadId=worker.UnloadId,WorkerId=worker.WorkerId,WorkerName=worker.WorkerName,WorkType=worker.WorkType,BagTypeId=worker.BagTypeId,BagTypeName=worker.BagTypeName,BagCount=worker.BagCount,Rate=rate,Amount=Money(Math.Clamp(rate,0,999999.99m)*worker.BagCount)});
        }
        if(form.GstPercent<0 || form.GstPercent>100 || Money(form.GstPercent)!=form.GstPercent) issues.Add("GST must be between 0 and 100 with at most 2 decimals.");
        result.ItemAmount=result.Items.Sum(i=>i.BaseAmount);
        result.LabDeduction=result.Items.Sum(i=>i.LabDeductionAmount);
        result.WorkerAmount=result.Workers.Sum(w=>w.Amount);
        result.Subtotal=Money(result.ItemAmount-result.LabDeduction+result.WorkerAmount);
        result.GstAmount=Money(result.Subtotal*Math.Clamp(form.GstPercent,0,100)/100m);
        result.Total=Money(result.Subtotal+result.GstAmount);
        if(result.Total<0 || result.Total>999999999999.99m) issues.Add("Bill total is outside the supported amount range.");
        result.Issues=issues.Distinct().ToList();
        return result;
    }
}
