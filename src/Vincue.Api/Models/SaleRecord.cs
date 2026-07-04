namespace Vin.Api.Models;

public class SaleRecord
{
    public int Id { get; set; }
    public string Vin { get; set; } = string.Empty;
    public decimal SalePrice { get; set; }
    // Plain int, computed upstream by whatever system produced sales-history.json
    // (not calculated here from DateAcquired/SoldDate) — this service trusts the
    // source's math rather than recomputing it, consistent with "each entity
    // mirrors its source's native shape exactly, no normalization at write time."
    public int DaysOnLot { get; set; }
    // This is the "-05:00" offset source — the third and last of the three
    // timestamp shapes (date-only, UTC Z, and this fixed-offset one) that all
    // collapse into the same DateTime type on write.
    public DateTime SoldDate { get; set; }
}

/* The pattern across all three: every entity has its own Id (own identity, own table), duplicates Vin as a plain string with no relational enforcement, and stores dates as bare DateTime. 
That last point is worth sitting with — it means the timezone-mismatch problem the spec calls out as a goal isn't actually solved anywhere in this codebase yet; it's silently swallowed at the entity layer before the aggregation service ever sees it. 
Confirm that with me later — it's a legitimate finding, not a memory gap. */