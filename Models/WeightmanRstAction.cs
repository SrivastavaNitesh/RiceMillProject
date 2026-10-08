namespace RiceMillProject.Models;

public class WeightmanRstAction
{
    public string? RSTNumber { get; set; }

    public decimal? CurrentGrossWeight { get; set; }

    public string? ActionType { get; set; } = "CONTINUE";

    public string? NewRSTNumber { get; set; }
}

public class WeightmanRstActionResult
{
    public long ActionId { get; set; }
    public string? NewRSTNumber { get; set; }
    public decimal ReceivedWeight { get; set; }
}
