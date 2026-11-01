using RiceMillProject.Models;
namespace RiceMillProject.BAL;
public static class DashboardMaterialBuilder
{
 public static void ApplyReturns(List<DashboardStockLine> stock,IEnumerable<DashboardReturnedStock> returns,List<string> notices)
 {
  foreach(var r in returns)
  {
   var lots=stock.Where(l=>l.UnloadId==r.UnloadId && l.BagTypeId==r.BagTypeId).OrderBy(l=>l.CategoryId).ThenBy(l=>l.ItemId).ToList();
   var total=lots.Sum(l=>l.ReceivedBags);
   if(total<=0){notices.Add($"Return for unloading {r.UnloadId} has no matching material bags.");continue;}
   decimal assignedBags=0,assignedKg=0;
   for(int i=0;i<lots.Count;i++)
   {
    var l=lots[i];var bags=i==lots.Count-1?r.Bags-assignedBags:decimal.Round(r.Bags*l.ReceivedBags/total,4);assignedBags+=bags;
    var kg=r.WeightKg.HasValue?(i==lots.Count-1?r.WeightKg.Value-assignedKg:decimal.Round(r.WeightKg.Value*l.ReceivedBags/total,3)):(l.ReceivedKg.HasValue?l.ReceivedKg.Value*bags/l.ReceivedBags:0);
    assignedKg+=kg;l.OutwardBags+=bags;l.OutwardKg+=kg;l.Estimated|=lots.Count>1||!r.WeightKg.HasValue;
    if(l.Bags<0 || l.WeightKg<0)notices.Add($"{l.RSTNumber}: outward quantity exceeds recorded material; review the stock entries.");
   }
  }
 }
 public static List<DashboardChargeLine> AllocateCharges(List<DashboardStockLine> stock,IEnumerable<DashboardSavedCharge> work)
 {
  List<DashboardChargeLine> result=[];
  foreach(var w in work)
  {
   var lots=stock.Where(l=>l.UnloadId==w.UnloadId && l.BagTypeId==w.BagTypeId && l.ReceivedBags>0)
    .GroupBy(l=>(l.CategoryId,l.CategoryName,l.ItemId,l.ItemName)).Select(g=>new{g.Key,Bags=g.Sum(l=>l.ReceivedBags)}).OrderBy(g=>g.Key.CategoryId).ThenBy(g=>g.Key.ItemId).ToList();
   if(lots.Count==0){result.Add(new(){RSTNumber=w.RSTNumber,CategoryName="Unallocated material",ItemName="Unallocated material",Bags=w.Bags,Rate=w.Rate,Amount=w.Amount,BagTypeName=w.BagTypeName,WorkType=w.WorkType});continue;}
   decimal total=lots.Sum(l=>l.Bags),allocated=0,assignedBags=0;
   for(int i=0;i<lots.Count;i++)
   {
    var l=lots[i];var amount=i==lots.Count-1?w.Amount-allocated:decimal.Round(w.Amount*l.Bags/total,2,MidpointRounding.AwayFromZero);
    var bags=i==lots.Count-1?w.Bags-assignedBags:decimal.Round(w.Bags*l.Bags/total,4);allocated+=amount;assignedBags+=bags;
    result.Add(new(){RSTNumber=w.RSTNumber,CategoryId=l.Key.CategoryId,CategoryName=l.Key.CategoryName,ItemId=l.Key.ItemId,ItemName=l.Key.ItemName,Bags=bags,Rate=w.Rate,Amount=amount,BagTypeName=w.BagTypeName,WorkType=w.WorkType,Allocated=lots.Count>1});
   }
  }
  return result;
 }
 public static void Summarize(DashboardMaterialPage page)
 {
  foreach(var id in page.Stock.Select(l=>l.CategoryId).Concat(page.Charges.Select(l=>l.CategoryId)).Distinct())
   if(!page.Categories.Any(c=>c.CategoryId==id))page.Categories.Add(new(){CategoryId=id,CategoryName=page.Stock.FirstOrDefault(l=>l.CategoryId==id)?.CategoryName??page.Charges.First(l=>l.CategoryId==id).CategoryName});
  foreach(var c in page.Categories)
  {
   var rows=page.Stock.Where(l=>l.CategoryId==c.CategoryId).ToList();c.Bags=rows.Sum(l=>l.Bags);c.WeightKg=rows.Sum(l=>l.WeightKg??0);c.PendingWeights=rows.Count(l=>!l.WeightKg.HasValue);c.Palledari=page.Charges.Where(l=>l.CategoryId==c.CategoryId).Sum(l=>l.Amount);
  }
  page.Categories=page.Categories.OrderBy(c=>c.CategoryName).ToList();
 }
}
