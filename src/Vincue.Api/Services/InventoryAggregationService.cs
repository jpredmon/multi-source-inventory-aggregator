using Microsoft.EntityFrameworkCore;
using Vin.Api.Data;
using Vin.Api.Dtos;

namespace Vin.Api.Services;

public class InventoryAggregationService : IInventoryAggregationService
{
    private readonly VinDbContext _context;

    public InventoryAggregationService(VinDbContext context)
    {
        _context = context;
    }

    public async Task<List<VehicleSummaryDto>> GetAllAsync()
    {
        return await BuildQuery().ToListAsync();
    }

    public async Task<VehicleSummaryDto?> GetByVinAsync(string vin)
    {
        return await BuildQuery().FirstOrDefaultAsync(v => v.Vin == vin);
    }

    private IQueryable<VehicleSummaryDto> BuildQuery()
    {
        return
            from dealer in _context.DealerInventory
            join auction in _context.AuctionRecords on dealer.Vin equals auction.Vin into auctionGroup
            from auction in auctionGroup.DefaultIfEmpty()
            join sale in _context.SaleRecords on dealer.Vin equals sale.Vin into saleGroup
            from sale in saleGroup.DefaultIfEmpty()
            select new VehicleSummaryDto
            {
                Vin = dealer.Vin,
                StockNumber = dealer.StockNumber,
                Cost = dealer.Cost,
                DateAcquired = dealer.DateAcquired,
                HammerPrice = auction != null ? auction.HammerPrice : null,
                AuctionDate = auction != null ? auction.AuctionDate : null,
                Condition = auction != null ? auction.Condition : null,
                SalePrice = sale != null ? sale.SalePrice : null,
                DaysOnLot = sale != null ? sale.DaysOnLot : null,
                SoldDate = sale != null ? sale.SoldDate : null,
                Status = sale != null
                    ? VehicleStatus.Sold
                    : auction != null
                        ? VehicleStatus.Auctioned
                        : VehicleStatus.OnLot
            };
    }
}
