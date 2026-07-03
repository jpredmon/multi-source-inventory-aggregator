namespace Vin.Api.Models;

public class AuctionRecord
{
    public int Id { get; set; }
    public string Vin { get; set; } = string.Empty;
    public decimal HammerPrice { get; set; }
    public DateTime AuctionDate { get; set; }
    public string Condition { get; set; } = string.Empty;
}
