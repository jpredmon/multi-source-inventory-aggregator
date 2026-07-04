using Microsoft.EntityFrameworkCore;
using Vin.Api.Data;
using Vin.Api.Dtos;

namespace Vin.Api.Services;


public class InventoryAggregationService : IInventoryAggregationService
{
    // Same DI story as the controller: this class doesn't construct its own
    // database access, it's handed a ready-to-use, request-scoped
    // VinDbContext by the container.
    private readonly VinDbContext _context;

    public InventoryAggregationService(VinDbContext context)
    {
        _context = context;
    }

    public async Task<List<VehicleSummaryDto>> GetAllAsync()
    {
        // BuildQuery() below returns an IQueryable — nothing has hit the
        // database yet at this point. ToListAsync() is what actually
        // triggers execution and materializes the results.
        return await BuildQuery().ToListAsync();
    }

    public async Task<VehicleSummaryDto?> GetByVinAsync(string vin)
    {
         // Same unexecuted IQueryable, but here EF adds a WHERE clause and
        // caps it at one row — FirstOrDefaultAsync translates to
        // "... WHERE Vin = @vin" plus a TOP(1), not "fetch everything and
        // filter in C#."
        return await BuildQuery().FirstOrDefaultAsync(v => v.Vin == vin);
    }

    // This method never runs against real data by itself — it only builds
    // an expression tree describing the query. EF Core's LINQ provider reads
    // that tree and translates it into a single SQL statement the first time
    // something (ToListAsync, FirstOrDefaultAsync) actually enumerates it.

    private IQueryable<VehicleSummaryDto> BuildQuery()
    {
        return
         // Start from DealerInventory — the anchor table. Every row here
            // produces exactly one output row, guaranteed, because it's the
            // left side of both joins below.
            from dealer in _context.DealerInventory
             // "join ... into auctionGroup" is LINQ's syntax for a SQL LEFT
            // JOIN when followed by DefaultIfEmpty(). Read it as: for each
            // dealer row, find every AuctionRecord with a matching Vin and
            // group them (auctionGroup) — a dealer row with no match gets
            // an empty group, not a dropped row.
            join auction in _context.AuctionRecords on dealer.Vin equals auction.Vin into auctionGroup
             // DefaultIfEmpty() flattens that group back to zero-or-one items,
            // substituting `null` when the group was empty. This is the part
            // that actually converts "grouped join" into "left join": without
            // this line, a dealer row with no auction match would vanish
            // entirely (that's what a plain inner join does).
            from auction in auctionGroup.DefaultIfEmpty()
            // Same left-join pattern again, independently, against SaleRecord.
            // Because this is a second GroupJoin off of `dealer` (not chained
            // off `auction`), a vehicle can have a null auction AND a null
            // sale, or an auction but no sale, etc. — all four combinations
            // the design spec calls out are representable here.
            join sale in _context.SaleRecords on dealer.Vin equals sale.Vin into saleGroup
            from sale in saleGroup.DefaultIfEmpty()
            // The projection. This runs once per output row, and EF Core
            // translates the null-checks below directly into SQL CASE
            // expressions / IS NULL checks — not into C# code running after
            // the fact, since the whole "select new Dto {...}" expression is
            // still part of the same unexecuted query tree.
            select new VehicleSummaryDto
            {
                Vin = dealer.Vin,
                StockNumber = dealer.StockNumber,
                Cost = dealer.Cost,
                DateAcquired = dealer.DateAcquired,
                 // `auction != null ? auction.HammerPrice : null` — this is
                // the null-check pattern EF requires here instead of `?.`,
                // because auction is a *query variable* from DefaultIfEmpty(),
                // and EF's LINQ-to-SQL translator needs an explicit ternary
                // to turn into a CASE/IS NULL check in the generated SQL;
                // `auction?.HammerPrice` isn't guaranteed to translate the
                // same way across all EF versions/providers.
                HammerPrice = auction != null ? auction.HammerPrice : null,
                AuctionDate = auction != null ? auction.AuctionDate : null,
                Condition = auction != null ? auction.Condition : null,
                SalePrice = sale != null ? sale.SalePrice : null,
                DaysOnLot = sale != null ? sale.DaysOnLot : null,
                SoldDate = sale != null ? sale.SoldDate : null,
                 // The derived Status field: sale wins over auction wins over
                // "neither" (OnLot). This is the one piece of actual business
                // logic in the entire query — everything else is structural
                // (joins, null-propagation).
                Status = sale != null
                    ? VehicleStatus.Sold
                    : auction != null
                        ? VehicleStatus.Auctioned
                        : VehicleStatus.OnLot
            };
    }
}
