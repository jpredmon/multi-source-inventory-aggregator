namespace Vin.Api.Dtos;

// The first endpoint in this API returning real SQL aggregate functions —
// SUM/AVG/COUNT/GROUP BY — rather than joins and null-coalescing. Everything
// else in this project so far has been about combining rows 1:1; this is
// about collapsing many rows into a few summary numbers.
public class InventoryStatsDto
{
    public List<StatusCountDto> CountsByStatus { get; set; } = [];
    public decimal TotalCost { get; set; }
    public decimal AverageCost { get; set; }
    // Nullable, not defaulted to 0 — zero Sold vehicles is a genuinely
    // possible state (e.g. right after a fresh seed with nothing sold yet),
    // and 0 would misleadingly read as "average margin is zero dollars"
    // instead of "there's nothing to average."
    public decimal? AverageProfitMarginForSold { get; set; }
    public double? AverageDaysOnLotForSold { get; set; }
}

public class StatusCountDto
{
    public VehicleStatus Status { get; set; }
    public int Count { get; set; }
}
