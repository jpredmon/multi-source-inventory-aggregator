using System.Text.Json.Serialization;

namespace Vin.Api.Dtos;
// Without this attribute, System.Text.Json serializes enums as their
// underlying int by default — the JSON would show "status": 2 instead of
// "status": "Sold". This is the fix from the review cycle mentioned earlier:
// the Angular template checks vehicle.status against string literals
// ('OnLot' | 'Auctioned' | 'Sold' in inventory.model.ts), so an int would
// have silently broken every status-dependent bit of the UI without erroring
// anywhere — a mismatch caught by review + live verification, not by a type
// error, since JSON has no compile-time contract between backend and frontend.
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VehicleStatus
{
    OnLot,
    Auctioned,
    Sold
}
// This class exists purely as the *shape of the merge result* — it isn't an
// entity (no EF mapping, no table), and it isn't one of the three source
// models. It's the one shape all three sources get flattened into.
public class VehicleSummaryDto
{
    // Always present — DealerInventory is the anchor, every VIN has one.
    public string Vin { get; set; } = string.Empty;
    public string StockNumber { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public DateTime DateAcquired { get; set; }
    // Nullable (note the `?`) because auction data is optional — a vehicle
    // still on the lot has never been auctioned. This nullability is the
    // DTO-level expression of "left join, not inner join."

    public decimal? HammerPrice { get; set; }
    public DateTime? AuctionDate { get; set; }
    public string? Condition { get; set; }
    // Same story, one layer further out: even an auctioned vehicle might not
    // be sold yet.

    public decimal? SalePrice { get; set; }
    public int? DaysOnLot { get; set; }
    public DateTime? SoldDate { get; set; }
     // Computed, not stored anywhere — derived at query time from which of
    // the two optional joins actually matched. See BuildQuery() below.

    public VehicleStatus Status { get; set; }
}
