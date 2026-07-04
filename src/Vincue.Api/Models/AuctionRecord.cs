namespace Vin.Api.Models;

public class AuctionRecord
{
    public int Id { get; set; }
    // Same Vin type/shape as DealerInventory.Vin, but there is no foreign key
    // constraint linking them — no [ForeignKey], no navigation property, nothing
    // in VinDbContext ties these tables together at the schema level. Vin is
    // just a natural key three independent tables happen to share, matching how
    // real disconnected systems (dealer DMS, auction house feed, sales CRM) actually
    // relate: no shared database, no referential integrity, just a common field.
    // The join only happens later, at query time, in InventoryAggregationService.
    public string Vin { get; set; } = string.Empty;
    public decimal HammerPrice { get; set; }
    // This is the "Z" (UTC) timestamp source — see DealerInventory.DateAcquired's
    // comment for why storing this as DateTime instead of DateTimeOffset matters.
    public DateTime AuctionDate { get; set; }
    public string Condition { get; set; } = string.Empty;
}
