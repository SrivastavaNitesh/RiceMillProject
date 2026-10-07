using System.ComponentModel.DataAnnotations;

namespace RiceMillProject.Models;

public class WeightmanRstAction
{
    [Required]
    public string RSTNumber { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 999999999999)]
    public decimal CurrentGrossWeight { get; set; }

    [Required]
    public string ActionType { get; set; } = "CONTINUE";

    public string? NewRSTNumber { get; set; }
}

public class WeightmanRstActionResult
{
    public long ActionId { get; set; }
    public string? NewRSTNumber { get; set; }
    public decimal ReceivedWeight { get; set; }
}
