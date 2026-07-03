namespace Vin.Api.Dtos;

public enum VehicleStatus
{
    OnLot,
    Auctioned,
    Sold
}

public class VehicleSummaryDto
{
    public string Vin { get; set; } = string.Empty;
    public string StockNumber { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public DateTime DateAcquired { get; set; }

    public decimal? HammerPrice { get; set; }
    public DateTime? AuctionDate { get; set; }
    public string? Condition { get; set; }

    public decimal? SalePrice { get; set; }
    public int? DaysOnLot { get; set; }
    public DateTime? SoldDate { get; set; }

    public VehicleStatus Status { get; set; }
}
