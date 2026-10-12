using System.Globalization;
using RiceMillProject.Models;

namespace RiceMillProject.BAL;

public static class PurchaseReportBuilder
{
    public static DateTime Today => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "India Standard Time").Date;

    public static void Normalize(PurchaseFilter f, string mode)
    {
        f.Mode = mode;
        var today = Today;
        f.Year ??= today.Year;
        f.Month ??= today.Month;
        f.Date ??= today;
        f.From ??= new DateTime(today.Year, today.Month, 1);
        f.To ??= today;
        if (f.Year is < 1900 or > 9998 || f.Month is < 1 or > 12)
            throw new ArgumentException("Select a valid year and month.");
        if (mode == "Monthly")
        {
            f.From = new DateTime(f.Year.Value, f.Month.Value, 1);
            f.To = f.From.Value.AddMonths(1).AddDays(-1);
        }
        if (mode == "Daily") f.From = f.To = f.Date.Value.Date;
        if (f.From > f.To || f.From.Value.Year < 1900 || f.To.Value.Year > 9998)
            throw new ArgumentException("Select a valid date range; From must not be after To.");
        if (f.CompanyId <= 0) f.CompanyId = null;
        if (f.PartyId <= 0) f.PartyId = null;
        if (f.LocationId <= 0) f.LocationId = null;
    }

    public static void Build(PurchasePage page, List<PurchaseLot> lots, List<PurchaseLab> labs)
    {
        var lotLookup = lots.ToLookup(x => x.RSTNumber);
        var labLookup = labs.ToLookup(x => (x.RSTNumber, x.CategoryId, x.ItemId));
        foreach (var h in page.Headers)
        {
            var rows = lotLookup[h.RSTNumber].OrderBy(x => x.DetailId).ToList();
            Allocate(h, rows, page.Notices);
            h.Items = rows.GroupBy(x => (x.CategoryId, x.ItemId)).Select(g =>
            {
                var tests = labLookup[(h.RSTNumber, g.Key.CategoryId, g.Key.ItemId)].ToList();
                return new PurchaseItem
                {
                    ItemId = g.Key.ItemId, CategoryId = g.Key.CategoryId,
                    ItemName = g.First().ItemName, CategoryName = g.First().CategoryName,
                    Lots = g.ToList(), Technicians = PurchaseItem.Names(tests.Select(x => x.RecordedBy)),
                    Deduction = tests.Count == 0 ? "Not applicable" : string.Join("; ", tests.Select(Deduction))
                };
            }).OrderBy(x => x.CategoryName).ThenBy(x => x.ItemName).ToList();
        }
        var bagTypes = page.BagTypes.Count > 0 ? page.BagTypes : lots.Select(l => new PurchaseOption(l.BagTypeId, l.BagTypeName)).Distinct().ToList();
        page.Columns = page.Locations.SelectMany(l => bagTypes.Select(b => new PurchaseColumn(l.LocationId, l.LocationName, b.Id, b.Name)))
            .Concat(lots.Select(x => new PurchaseColumn(x.LocationId ?? 0, x.LocationName, x.BagTypeId, x.BagTypeName)))
            .Distinct().OrderBy(x => x.LocationName).ThenBy(x => x.BagTypeName).ToList();
    }

    public static void Allocate(PurchaseHeader h, List<PurchaseLot> lots, List<string> notices)
    {
        foreach (var lot in lots) { lot.WeightKg = lot.ActualWeightKg; lot.Estimated = false; }
        if (lots.Count == 0) return;
        var known = lots.Where(x => x.ActualWeightKg.HasValue).Sum(x => x.ActualWeightKg!.Value);
        var unknown = lots.Where(x => !x.ActualWeightKg.HasValue).ToList();
        if (lots.Any(x => x.BagCount < 0 || x.ActualWeightKg < 0) ||
            (h.NetWeight.HasValue && (known > h.NetWeight || (unknown.Count == 0 && Math.Abs(known - h.NetWeight.Value) > .005m))))
        {
            notices.Add($"{h.RSTNumber}: actual item weights do not reconcile with net weight.");
            return;
        }
        if (!h.NetWeight.HasValue) return;
        var bags = unknown.Sum(x => (long)x.BagCount);
        if (unknown.Count == 0 || bags <= 0) return;
        var remaining = h.NetWeight.Value - known;
        decimal assigned = 0;
        for (var n = 0; n < unknown.Count; n++)
        {
            var lot = unknown[n];
            lot.WeightKg = n == unknown.Count - 1 ? remaining - assigned : BillingCalculator.Kg(remaining * lot.BagCount / bags);
            assigned += lot.WeightKg.Value;
            lot.Estimated = lots.Count > 1;
        }
    }

    public static string Deduction(PurchaseLab lab)
    {
        var prefix = lab.TestName + ": ";
        if (!lab.ReportId.HasValue || string.IsNullOrWhiteSpace(lab.ResultValue)) return prefix + "Pending";
        if (!lab.RuleId.HasValue)
            return lab.MasterDeduction.GetValueOrDefault() > 0
                ? $"{prefix}{lab.MasterDeduction:0.###} {lab.MasterUnit} (master deduction; billing rule not configured)"
                : prefix + "0 (no deduction)";
        if (lab.DeductionMode == "None") return prefix + "0 (no deduction)";
        if (!decimal.TryParse(lab.ResultValue.Trim().TrimEnd('%'), NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
            return prefix + "Result requires review";
        if (result < lab.Threshold) return prefix + "0 (below threshold)";
        var unit = lab.DeductionMode switch
        {
            "Percent" => "% of item amount", "PerQuintal" => "INR / quintal", "PerKg" => "INR / kg",
            "PerBag" => "INR / bag", "Fixed" => "INR / item", _ => "(rule requires review)"
        };
        return $"{prefix}{lab.DeductionValue:0.######} {unit}";
    }
}
