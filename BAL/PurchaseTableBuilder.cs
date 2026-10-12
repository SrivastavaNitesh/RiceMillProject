using RiceMillProject.Models;

namespace RiceMillProject.BAL;

public static class PurchaseTableBuilder
{
    private static PurchaseCell Text(object? value, int rows = 1, int cols = 1, bool heading = false) => new(value, rows, cols, heading);
    private static object Weight(IEnumerable<PurchaseLot> source)
    {
        var lots = source.ToList();
        if (lots.Any(l => !l.WeightKg.HasValue)) return "Pending";
        var weight = lots.Sum(l => l.WeightKg!.Value);
        return lots.Any(l => l.Estimated) ? weight.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + " *" : weight;
    }
    public static PurchaseTable Register(PurchasePage page)
    {
        var t = new PurchaseTable { HeaderRows = 3, ColumnCount = 17 + page.Columns.Count * 2 };
        var head = new List<PurchaseCell>();
        foreach (var title in new[] { "Inward number", "RST number / weighing date", "Party name", "Party mobile", "Driver name", "Vehicle number", "Item category", "Item name", "Gross kg (RST)", "Net kg (RST)", "Tare kg (RST)" }) head.Add(Text(title, 3, heading: true));
        foreach (var loc in page.Columns.GroupBy(c => (c.LocationId, c.LocationName))) head.Add(Text(loc.Key.LocationName, cols: loc.Count() * 2, heading: true));
        foreach (var title in new[] { "Item total kg", "Item total bags", "Supervisor", "Meth", "Lab technician", "Lab deduction" }) head.Add(Text(title, 3, heading: true));
        t.Rows.Add(head);
        t.Rows.Add(page.Columns.Select(c => Text(c.BagTypeName, cols: 2, heading: true)).ToList());
        t.Rows.Add(page.Columns.SelectMany(c => new[] { Text("Bags", heading: true), Text("Weight kg", heading: true) }).ToList());
        foreach (var company in page.Headers.GroupBy(h => (h.CompanyId, h.CompanyName)))
        {
            foreach (var h in company)
            {
                var items = h.Items.Count == 0 ? new List<PurchaseItem> { new() { ItemName = "Unloading pending", Deduction = "Pending" } } : h.Items;
                for (var n = 0; n < items.Count; n++)
                {
                    var item = items[n];
                    var row = new List<PurchaseCell>();
                    if (n == 0)
                        foreach (var value in new[] { h.InwardNo, h.RSTNumber + "\n" + h.WeighingDate.ToString("dd-MM-yyyy"), h.PartyName, h.PartyMobile, h.DriverName, h.VehicleNumber }) row.Add(Text(value, items.Count));
                    row.Add(Text(item.CategoryName)); row.Add(Text(item.ItemName));
                    if (n == 0)
                        foreach (var value in new decimal?[] { h.GrossWeight, h.NetWeight, h.TareWeight }) row.Add(Text(value.HasValue ? value.Value : "Pending", items.Count));
                    foreach (var c in page.Columns)
                    {
                        var lots = item.Lots.Where(l => (l.LocationId ?? 0) == c.LocationId && l.BagTypeId == c.BagTypeId).ToList();
                        row.Add(Text(lots.Sum(l => l.BagCount))); row.Add(Text(Weight(lots)));
                    }
                    row.Add(Text(item.Lots.Count == 0 ? "Pending" : Weight(item.Lots)));
                    row.Add(Text(item.Bags)); row.Add(Text(item.Supervisors)); row.Add(Text(item.Meths));
                    row.Add(Text(item.Technicians)); row.Add(Text(item.Deduction)); t.Rows.Add(row);
                }
            }
            var allLots = company.SelectMany(h => h.Items).SelectMany(i => i.Lots).ToList();
            var total = new List<PurchaseCell> { Text("Total", cols: 8, heading: true), Text(company.Sum(h => h.GrossWeight)), Text(company.Any(h => !h.NetWeight.HasValue) ? "Pending" : company.Sum(h => h.NetWeight)), Text(company.Any(h => !h.TareWeight.HasValue) ? "Pending" : company.Sum(h => h.TareWeight)) };
            foreach (var c in page.Columns)
            {
                var lots = allLots.Where(l => (l.LocationId ?? 0) == c.LocationId && l.BagTypeId == c.BagTypeId).ToList();
                total.Add(Text(lots.Sum(l => l.BagCount))); total.Add(Text(Weight(lots)));
            }
            total.Add(Text(Weight(allLots))); total.Add(Text(allLots.Sum(l => l.BagCount)));
            total.Add(Text("", cols: 4)); t.Rows.Add(total);
        }
        return t;
    }

    public static PurchaseTable Location(PurchasePage page)
    {
        var lots = page.Lots.Where(l => (l.LocationId ?? 0) == page.Filter.LocationId).ToList();
        var bags = lots.Select(l => (l.BagTypeId, l.BagTypeName)).Distinct().OrderBy(x => x.BagTypeName).ToList();
        var t = new PurchaseTable { HeaderRows = 2, ColumnCount = 4 + bags.Count * 2 };
        var head = new List<PurchaseCell> { Text("Item category", 2, heading: true), Text("Item name", 2, heading: true) };
        head.AddRange(bags.Select(b => Text(b.BagTypeName, cols: 2, heading: true)));
        head.Add(Text("Total weight kg", 2, heading: true)); head.Add(Text("Total bags", 2, heading: true));
        t.Rows.Add(head); t.Rows.Add(bags.SelectMany(b => new[] { Text("Bags", heading: true), Text("Weight kg", heading: true) }).ToList());
        foreach (var item in lots.GroupBy(l => (l.CategoryId, l.ItemId)).OrderBy(g => g.First().CategoryName).ThenBy(g => g.First().ItemName))
        {
            var row = new List<PurchaseCell> { Text(item.First().CategoryName), Text(item.First().ItemName) };
            foreach (var bag in bags)
            {
                var group = item.Where(l => l.BagTypeId == bag.BagTypeId).ToList();
                row.Add(Text(group.Sum(l => l.BagCount))); row.Add(Text(Weight(group)));
            }
            row.Add(Text(Weight(item))); row.Add(Text(item.Sum(l => l.BagCount))); t.Rows.Add(row);
        }
        var total = new List<PurchaseCell> { Text("Total", cols: 2, heading: true) };
        foreach (var bag in bags)
        {
            var group = lots.Where(l => l.BagTypeId == bag.BagTypeId).ToList();
            total.Add(Text(group.Sum(l => l.BagCount))); total.Add(Text(Weight(group)));
        }
        total.Add(Text(Weight(lots))); total.Add(Text(lots.Sum(l => l.BagCount))); t.Rows.Add(total);
        return t;
    }

    public static PurchaseTable Locations(PurchasePage page)
    {
        var t = new PurchaseTable { HeaderRows = 1, ColumnCount = 5 };
        t.Rows.Add(new[] { "Location", "Capacity", "Capacity unit", "Unloaded purchase weight kg", "Total bags" }.Select(x => Text(x, heading: true)).ToList());
        foreach (var location in page.Locations)
        {
            var lots = page.Lots.Where(l => l.LocationId == location.LocationId).ToList();
            t.Rows.Add([Text(location.LocationName), Text(location.Capacity), Text(location.CapacityUnit), Text(Weight(lots)), Text(lots.Sum(l => l.BagCount))]);
        }
        return t;
    }
}
