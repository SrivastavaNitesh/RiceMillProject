namespace RiceMillProject.Models;

public record PurchaseCell(object? Value, int RowSpan = 1, int ColSpan = 1, bool Heading = false);
public class PurchaseTable
{
    public List<List<PurchaseCell>> Rows { get; } = [];
    public int HeaderRows { get; set; }
    public int ColumnCount { get; set; }
}
