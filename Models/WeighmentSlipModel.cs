namespace RiceMillProject.Models;
public class WeighmentSlipModel
{
 public int InwardId {get;set;}
 public string Stage {get;set;}="first";
 public string RstNumber {get;set;}="";
 public string InwardNo {get;set;}="";
 public DateTime? InwardAt {get;set;}
 public DateTime? WeighedAt {get;set;}
 public DateTime? FinalAt {get;set;}
 public DateTime? UnloadAt {get;set;}
 public string Party {get;set;}="";
 public string Vehicle {get;set;}="";
 public string Driver {get;set;}="";
 public string DriverMobile {get;set;}="";
 public string Category {get;set;}="";
 public decimal Gross {get;set;}
 public decimal? Tare {get;set;}
 public decimal? Net {get;set;}
 public decimal Charge {get;set;}
 public string Status {get;set;}="";
 public Dictionary<string,long> Bags {get;set;}=new();
}
