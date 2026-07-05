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

    public async Task<InventoryStatsDto> GetStatsAsync()
    {
        // Reuses BuildQuery() as a subquery — same principle as GetAllAsync/
        // GetByVinAsync: build on the one merge query rather than
        // re-deriving the join logic here. Everything below is genuine SQL
        // aggregation (GROUP BY / SUM / AVG / COUNT), the first real
        // aggregate-function usage in this project — everything before this
        // point has been joins and null-coalescing, not rollups.
        var baseQuery = BuildQuery();

        // GroupBy(v => v.Status) translates to a real SQL GROUP BY over the
        // derived table's computed Status expression (the CASE from
        // BuildQuery()) — counting vehicles per status bucket in one query.
        var countsByStatus = await baseQuery
            .GroupBy(v => v.Status)
            .Select(g => new StatusCountDto { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        // GroupBy(v => 1) is the standard EF Core idiom for "compute several
        // aggregates over the entire set in one round trip." The constant
        // key means there's exactly one group, so SQL Server emits a plain
        // single-row aggregate SELECT rather than a real GROUP BY.
        var overall = await baseQuery
            .GroupBy(v => 1)
            .Select(g => new { TotalCost = g.Sum(v => v.Cost), AverageCost = g.Average(v => v.Cost) })
            .FirstOrDefaultAsync();

        // Same idiom, filtered to Sold vehicles only — ProfitMargin/DaysOnLot
        // are null for anything not sold, so averaging over the whole set
        // would silently skew these toward zero. Filter first, then average
        // only over rows where these fields are actually meaningful.
        var soldOnly = await baseQuery
            .Where(v => v.Status == VehicleStatus.Sold)
            .GroupBy(v => 1)
            .Select(g => new
            {
                AverageProfitMargin = g.Average(v => v.ProfitMargin),
                AverageDaysOnLot = g.Average(v => v.DaysOnLot)
            })
            .FirstOrDefaultAsync();

        return new InventoryStatsDto
        {
            CountsByStatus = countsByStatus,
            TotalCost = overall?.TotalCost ?? 0m,
            AverageCost = overall?.AverageCost ?? 0m,
            AverageProfitMarginForSold = soldOnly?.AverageProfitMargin,
            AverageDaysOnLotForSold = soldOnly?.AverageDaysOnLot
        };
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
            // left side of the sale join below and the auction subquery
            // below always returns at most one row.
            from dealer in _context.DealerInventory
            // AuctionRecord has no uniqueness constraint on Vin — nothing
            // stops a vehicle from being auctioned, failing to sell, and
            // getting re-listed later, which produces 2+ AuctionRecord rows
            // for one Vin. The old `join ... into auctionGroup` pattern
            // assumed at most one match per side; with 2+ matches it would
            // silently emit one output row per match (a duplicate vehicle
            // in the response), not an error. This correlated subquery
            // fixes that: for each dealer row, pull every AuctionRecord for
            // this Vin, order by AuctionDate descending (most recent
            // auction wins as the business rule), break ties by Id
            // descending (most recently inserted), and take only the first.
            // A `let x = query.FirstOrDefault()` here looked equivalent but
            // wasn't: EF Core inlined a separate correlated scalar subquery
            // for every property later read off `auction` (HammerPrice,
            // AuctionDate, Condition, plus a fourth EXISTS for Status) —
            // four correlated subqueries per row instead of one, confirmed
            // by reading the generated SQL.
            //
            // This pattern — a second `from` (SelectMany) over a correlated,
            // ordered, `Take(1)` + `DefaultIfEmpty()` subquery — was expected
            // to compile to a SQL Server OUTER APPLY. It doesn't: EF Core 9
            // recognizes this exact "top-1-per-partition" shape and rewrites
            // it as a single windowed derived table —
            // `ROW_NUMBER() OVER (PARTITION BY Vin ORDER BY AuctionDate DESC,
            // Id DESC) ... WHERE row <= 1` — joined once via a plain LEFT
            // JOIN, confirmed by reading the generated SQL. That's actually
            // better than an APPLY here: one windowed scan over the whole
            // table plus one join, instead of a correlated subquery
            // re-executed per outer row. It's also the identical SQL shape
            // hand-written as a T-SQL view elsewhere in this project for
            // the same "most recent auction per VIN" problem — EF Core
            // independently arrived at the same construct a human would
            // write by hand.
            from auction in _context.AuctionRecords
                .Where(a => a.Vin == dealer.Vin)
                .OrderByDescending(a => a.AuctionDate)
                .ThenByDescending(a => a.Id)
                .Take(1)
                .DefaultIfEmpty()
            // SaleRecord has this identical latent-multiplicity risk —
            // nothing stops 2+ sale rows per Vin either — but it's
            // deliberately left as a plain LEFT JOIN this round, unfixed.
            // That's a conscious scope decision (this pass targets the
            // auction side only), not an oversight.
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
                // The first genuine arithmetic in this query: sale.SalePrice
                // minus dealer.Cost. EF Core translates this straight into a
                // SQL subtraction inside the same SELECT — no separate
                // computation step, no fetching rows into memory first.
                ProfitMargin = sale != null ? sale.SalePrice - dealer.Cost : null,
                 // The derived Status field: sale wins over auction wins over
                // "neither" (OnLot). Between this and ProfitMargin above,
                // that's the entirety of the query's actual business logic —
                // everything else is structural (joins, null-propagation).
                Status = sale != null
                    ? VehicleStatus.Sold
                    : auction != null
                        ? VehicleStatus.Auctioned
                        : VehicleStatus.OnLot
            };
    }
}
