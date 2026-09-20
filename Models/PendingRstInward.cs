namespace RiceMillProject.Models;

public class PendingRstInward
{
    public string InwardNo { get; set; } = "";
    public int PartyId { get; set; }
    public string PartyName { get; set; } = "";
    public string VehicleNumber { get; set; } = "";
    public string DriverName { get; set; } = "";
    public string DriverMobile { get; set; } = "";
}
