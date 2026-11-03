namespace RiceMillProject.Models;
public class PaddyRecordFilter
{
 public DateTime? FromDate {get;set;}
 public DateTime? ToDate {get;set;}
 public bool ColumnsChosen {get;set;}
 public bool BagTypesChosen {get;set;}
 public List<int> SelectedItemIds {get;set;}=[];
 public List<int> SelectedLocationIds {get;set;}=[];
 public List<int> SelectedBagTypeIds {get;set;}=[];
 public bool ShowStackPP {get;set;}=true;
 public bool ShowStackJute {get;set;}=true;
 public bool ShowHaudiPP {get;set;}=true;
 public bool ShowHaudiJute {get;set;}=true;
 public bool ShowTotalBags {get;set;}=true;
 public bool ShowWeight {get;set;}=true;
 public static readonly PaddyRecordOptionColumn[] OptionalColumns=[new("ShowTotalBags","Total Bags"),new("ShowWeight","Weight (KG)"),new("ShowMeth","Meth Name"),new("ShowLocations","Actual Unloading Location")];
 public bool IsColumnVisible(string key)=>key switch{"ShowStackPP"=>ShowStackPP,"ShowStackJute"=>ShowStackJute,"ShowHaudiPP"=>ShowHaudiPP,"ShowHaudiJute"=>ShowHaudiJute,"ShowTotalBags"=>ShowTotalBags,"ShowWeight"=>ShowWeight,"ShowMeth"=>ShowMeth,"ShowLocations"=>ShowLocations,_=>false};
 public bool ShowMeth {get;set;}=true;
 public bool ShowLocations {get;set;}=true;
}
public record PaddyRecordOptionColumn(string Key,string Label);
public record PaddyRecordOption(int Id,string Name);
public record PaddyRecordColumn(int LocationId,string LocationName,int BagTypeId,string BagTypeName);
public class PaddyRecordPage
{
 public string CategoryName {get;set;}="Paddy";
 public string ReportAction=>CategoryName=="Rice"?"RiceRecord":"PaddyRecord";
 public string PrintTitle=>CategoryName.ToUpperInvariant()+" PURCHASE REPORT";
 public PaddyRecordFilter Filter {get;set;}=new();
 public string SupervisorName {get;set;}="";
 public string OfficeName {get;set;}="";
 public List<PaddyRecordOption> Items {get;set;}=[];
 public List<PaddyRecordOption> Locations {get;set;}=[];
 public List<PaddyRecordOption> BagTypes {get;set;}=[];
 public List<PaddyRecordRow> Rows {get;set;}=[];
 public IEnumerable<PaddyRecordColumn> Columns=>Locations.SelectMany(l=>BagTypes.Select(b=>new PaddyRecordColumn(l.Id,l.Name,b.Id,b.Name)));
 public bool HasPaddyCategory {get;set;}
 public int VisibleColumnCount=>3+Filter.SelectedLocationIds.Count*Filter.SelectedBagTypeIds.Count+Filter.SelectedItemIds.Count+PaddyRecordFilter.OptionalColumns.Count(c=>Filter.IsColumnVisible(c.Key));
}
public class PaddyRecordRow
{
 public string RSTNumber {get;set;}="";
 public string VehicleNumber {get;set;}="";
 public string PartyName {get;set;}="";
 public long StackPP {get;set;}
 public long StackJute {get;set;}
 public long HaudiPP {get;set;}
 public long HaudiJute {get;set;}
 public long TotalBags {get;set;}
 public decimal? NetWeightKg {get;set;}
 public string MethName {get;set;}="";
 public bool PendingDetails {get;set;}
 public Dictionary<(int LocationId,int ItemId),long> ItemBags {get;set;}=[];
 public Dictionary<int,long> ItemTotals {get;set;}=[];
 public Dictionary<(int LocationId,int BagTypeId),long> LocationBagCounts {get;set;}=[];
 public Dictionary<int,string> ActualLocations {get;set;}=[];
 public long LocationBagsAt(int locationId,int bagTypeId)=>LocationBagCounts.GetValueOrDefault((locationId,bagTypeId));
 public long ItemTotal(int itemId)=>ItemTotals.GetValueOrDefault(itemId);
 public long BagsAt(int locationId,int itemId)=>ItemBags.GetValueOrDefault((locationId,itemId));
}
public record PaddyRecordAssignment(int UnloadId,string RSTNumber,string VehicleNumber,string PartyName,string MethName,bool GateIsPaddy,long StackPP,long StackJute,long HaudiPP,long HaudiJute,long SupervisorTotalBags,decimal? NetWeightKg);
public record PaddyRecordLot(int UnloadId,int ItemId,string ItemName,int LocationId,string LocationName,long Bags,bool IsPaddy,int BagTypeId=0,string BagTypeName="Unknown bag type");

public record PaddyRecordLocationLot(int UnloadId,int LocationId,string LocationName,int BagTypeId,string BagTypeName,long Bags);
