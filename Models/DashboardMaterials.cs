namespace RiceMillProject.Models;
public class DashboardMaterialPage
{
 public DateTime FromDate {get;set;}
 public DateTime ToDate {get;set;}
 public int? CategoryId {get;set;}
 public int? ItemId {get;set;}
 public string Mode {get;set;}="item";
 public List<DashboardMaterialCategory> Categories {get;set;}=[];
 public List<DashboardStockLine> Stock {get;set;}=[];
 public List<DashboardChargeLine> Charges {get;set;}=[];
 public List<string> Notices {get;set;}=[];
 public decimal TotalPalledari=>Charges.Sum(r=>r.Amount);
 public string CategoryName=>Categories.FirstOrDefault(c=>c.CategoryId==CategoryId)?.CategoryName??"All categories";
}
public class DashboardMaterialCategory
{
 public int CategoryId {get;set;}
 public string CategoryName {get;set;}="";
 public decimal Bags {get;set;}
 public decimal WeightKg {get;set;}
 public int PendingWeights {get;set;}
 public decimal Palledari {get;set;}
}
public class DashboardStockLine
{
 public int UnloadId {get;set;}
 public string RSTNumber {get;set;}="";
 public int CategoryId {get;set;}
 public string CategoryName {get;set;}="";
 public int ItemId {get;set;}
 public string ItemName {get;set;}="";
 public int BagTypeId {get;set;}
 public string BagTypeName {get;set;}="";
 public decimal ReceivedBags {get;set;}
 public decimal OutwardBags {get;set;}
 public int Direction {get;set;}=1;
 public decimal Bags=>Direction*ReceivedBags-OutwardBags;
 public decimal? ReceivedKg {get;set;}
 public decimal OutwardKg {get;set;}
 public decimal? WeightKg=>Direction*ReceivedKg-OutwardKg;
 public bool Estimated {get;set;}
}
public class DashboardChargeLine
{
 public string RSTNumber {get;set;}="";
 public int CategoryId {get;set;}
 public string CategoryName {get;set;}="";
 public int ItemId {get;set;}
 public string ItemName {get;set;}="";
 public string BagTypeName {get;set;}="";
 public string WorkType {get;set;}="";
 public decimal Bags {get;set;}
 public decimal Rate {get;set;}
 public decimal Amount {get;set;}
 public bool Allocated {get;set;}
}
public record DashboardSavedCharge(int UnloadId,string RSTNumber,int BagTypeId,string BagTypeName,string WorkType,decimal Bags,decimal Rate,decimal Amount);
public record DashboardReturnedStock(int UnloadId,int BagTypeId,decimal Bags,decimal? WeightKg);
