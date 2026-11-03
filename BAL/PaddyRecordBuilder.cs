using RiceMillProject.Models;
namespace RiceMillProject.BAL;
public static class PaddyRecordBuilder
{
 public static List<PaddyRecordRow> Build(IEnumerable<PaddyRecordAssignment> assignments,IEnumerable<PaddyRecordLot> material,IEnumerable<PaddyRecordLocationLot>? savedLocations=null)
 {
  var lookup=material.ToLookup(l=>l.UnloadId);var savedLookup=(savedLocations??[]).ToLookup(l=>l.UnloadId);List<PaddyRecordRow> rows=[];
  foreach(var group in assignments.GroupBy(a=>a.RSTNumber))
  {
   var headers=group.DistinctBy(a=>a.UnloadId).ToList();var lots=headers.SelectMany(h=>lookup[h.UnloadId]).ToList();var paddy=lots.Where(l=>l.IsPaddy).ToList();
   if(paddy.Count==0 && !(lots.Count==0 && headers.Any(h=>h.GateIsPaddy)))continue;
   var actual=headers.SelectMany(h=>savedLookup[h.UnloadId].Any()?savedLookup[h.UnloadId]:lookup[h.UnloadId].Where(l=>l.IsPaddy).Select(l=>new PaddyRecordLocationLot(l.UnloadId,l.LocationId,l.LocationName,l.BagTypeId,l.BagTypeName,l.Bags))).ToList();
   rows.Add(new(){RSTNumber=group.Key,VehicleNumber=headers[0].VehicleNumber,PartyName=headers[0].PartyName,MethName=string.Join(", ",headers.Select(h=>h.MethName).Where(n=>!string.IsNullOrWhiteSpace(n)).Distinct().Order()),StackPP=headers.Sum(h=>h.StackPP),StackJute=headers.Sum(h=>h.StackJute),HaudiPP=headers.Sum(h=>h.HaudiPP),HaudiJute=headers.Sum(h=>h.HaudiJute),TotalBags=lots.Count>0?lots.Sum(l=>l.Bags):headers.Sum(h=>h.SupervisorTotalBags),NetWeightKg=headers.Select(h=>h.NetWeightKg).FirstOrDefault(w=>w.HasValue),PendingDetails=lots.Count==0,ItemTotals=paddy.GroupBy(l=>l.ItemId).ToDictionary(g=>g.Key,g=>g.Sum(l=>l.Bags)),LocationBagCounts=actual.GroupBy(l=>(l.LocationId,l.BagTypeId)).ToDictionary(g=>g.Key,g=>g.Sum(l=>l.Bags)),ActualLocations=actual.GroupBy(l=>l.LocationId).ToDictionary(g=>g.Key,g=>g.First().LocationName),ItemBags=paddy.GroupBy(l=>(l.LocationId,l.ItemId)).ToDictionary(g=>g.Key,g=>g.Sum(l=>l.Bags))});
  }
  return rows;
 }
 public static void ConfigureColumns(PaddyRecordPage page)
 {
  var f=page.Filter;
  if(!f.ColumnsChosen){f.SelectedItemIds=page.Items.Select(i=>i.Id).ToList();f.SelectedLocationIds=page.Locations.Select(l=>l.Id).ToList();f.SelectedBagTypeIds=page.BagTypes.Select(b=>b.Id).ToList();}
  if(!f.BagTypesChosen && f.SelectedBagTypeIds.Count==0)f.SelectedBagTypeIds=page.BagTypes.Select(b=>b.Id).ToList();
  f.SelectedBagTypeIds=f.SelectedBagTypeIds.Distinct().ToList();
  f.SelectedItemIds=f.SelectedItemIds.Distinct().ToList();f.SelectedLocationIds=f.SelectedLocationIds.Distinct().ToList();
  if(f.SelectedBagTypeIds.Any(id=>!page.BagTypes.Any(b=>b.Id==id)))throw new ArgumentException("Choose available bag types.");
  if(f.SelectedItemIds.Any(id=>!page.Items.Any(i=>i.Id==id)) || f.SelectedLocationIds.Any(id=>!page.Locations.Any(l=>l.Id==id)))throw new ArgumentException($"Choose {page.CategoryName} items and locations available in this company.");
 }
}
