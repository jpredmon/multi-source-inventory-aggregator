namespace Vin.Api.Models;

public class DealerInventory
{
    public int Id { get; set; }
    public string Vin { get; set; } = string.Empty;
    public string StockNumber { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public DateTime DateAcquired { get; set; }
}
