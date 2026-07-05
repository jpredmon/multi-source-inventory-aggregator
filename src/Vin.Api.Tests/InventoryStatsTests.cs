using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Vin.Api.Data;
using Vin.Api.Dtos;
using Vin.Api.Models;
using Vin.Api.Services;

namespace Vin.Api.Tests;

[Collection("Database")]
public class InventoryStatsTests : IAsyncLifetime
{
    private VinDbContext _context = null!;
    private IDbContextTransaction _transaction = null!;
    private InventoryAggregationService _sut = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<VinDbContext>()
            .UseSqlServer(DatabaseFixture.ConnectionString)
            .Options;
        _context = new VinDbContext(options);
        _transaction = await _context.Database.BeginTransactionAsync();
        _sut = new InventoryAggregationService(_context);
    }

    public async Task DisposeAsync()
    {
        await _transaction.RollbackAsync();
        await _context.DisposeAsync();
    }

    // The one test whose correctness genuinely depends on the "DB is empty
    // at test start" guarantee (see DatabaseFixture.cs) — GetStatsAsync
    // aggregates the entire table with no per-VIN filter, unlike every test
    // in InventoryAggregationServiceTests.cs, which asserts against results
    // filtered to its own seeded VIN and would tolerate stray rows.
    [Fact]
    public async Task GetStatsAsync_ComputesCorrectCountsAndAverages()
    {
        var baseDate = new DateTime(2026, 1, 1);

        // 2 OnLot
        _context.DealerInventory.AddRange(
            new DealerInventory { Vin = "STATSVIN0000ONLOT1", StockNumber = "SO1", Cost = 10000m, DateAcquired = baseDate },
            new DealerInventory { Vin = "STATSVIN0000ONLOT2", StockNumber = "SO2", Cost = 20000m, DateAcquired = baseDate });

        // 1 Auctioned-only
        _context.DealerInventory.Add(new DealerInventory
        {
            Vin = "STATSVIN00AUCTION1", StockNumber = "SA1", Cost = 15000m, DateAcquired = baseDate
        });
        _context.AuctionRecords.Add(new AuctionRecord
        {
            Vin = "STATSVIN00AUCTION1", HammerPrice = 14000m, AuctionDate = baseDate.AddDays(5), Condition = "Good"
        });

        // 2 Sold — Cost=10000/SalePrice=12000 (margin 2000, 20 days) and
        // Cost=20000/SalePrice=25000 (margin 5000, 40 days)
        _context.DealerInventory.AddRange(
            new DealerInventory { Vin = "STATSVIN0000SOLD01", StockNumber = "SS1", Cost = 10000m, DateAcquired = baseDate },
            new DealerInventory { Vin = "STATSVIN0000SOLD02", StockNumber = "SS2", Cost = 20000m, DateAcquired = baseDate });
        _context.AuctionRecords.AddRange(
            new AuctionRecord { Vin = "STATSVIN0000SOLD01", HammerPrice = 9500m, AuctionDate = baseDate.AddDays(5), Condition = "Good" },
            new AuctionRecord { Vin = "STATSVIN0000SOLD02", HammerPrice = 19500m, AuctionDate = baseDate.AddDays(5), Condition = "Good" });
        _context.SaleRecords.AddRange(
            new SaleRecord { Vin = "STATSVIN0000SOLD01", SalePrice = 12000m, DaysOnLot = 20, SoldDate = baseDate.AddDays(20) },
            new SaleRecord { Vin = "STATSVIN0000SOLD02", SalePrice = 25000m, DaysOnLot = 40, SoldDate = baseDate.AddDays(40) });

        await _context.SaveChangesAsync();

        var stats = await _sut.GetStatsAsync();

        Assert.Equal(75000m, stats.TotalCost);
        Assert.Equal(15000m, stats.AverageCost);
        Assert.Equal(3500m, stats.AverageProfitMarginForSold);
        Assert.Equal(30d, stats.AverageDaysOnLotForSold);

        Assert.Equal(2, stats.CountsByStatus.Single(c => c.Status == VehicleStatus.OnLot).Count);
        Assert.Equal(1, stats.CountsByStatus.Single(c => c.Status == VehicleStatus.Auctioned).Count);
        Assert.Equal(2, stats.CountsByStatus.Single(c => c.Status == VehicleStatus.Sold).Count);
    }
}
