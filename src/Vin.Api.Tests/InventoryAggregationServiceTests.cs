using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Vin.Api.Data;
using Vin.Api.Dtos;
using Vin.Api.Models;
using Vin.Api.Services;

namespace Vin.Api.Tests;

[Collection("Database")]
public class InventoryAggregationServiceTests : IAsyncLifetime
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

    [Fact]
    public async Task Status_OnLot_WhenNoAuctionAndNoSale()
    {
        const string vin = "TESTVIN00000ONLOT";
        _context.DealerInventory.Add(new DealerInventory
        {
            Vin = vin,
            StockNumber = "S-ONLOT",
            Cost = 10000m,
            DateAcquired = new DateTime(2026, 1, 1)
        });
        await _context.SaveChangesAsync();

        var result = await _sut.GetByVinAsync(vin);

        Assert.NotNull(result);
        Assert.Equal(VehicleStatus.OnLot, result!.Status);
        Assert.Null(result.HammerPrice);
        Assert.Null(result.SalePrice);
        Assert.Null(result.ProfitMargin);
    }

    [Fact]
    public async Task Status_Auctioned_WhenAuctionExistsButNoSale()
    {
        const string vin = "TESTVIN00AUCTION1";
        _context.DealerInventory.Add(new DealerInventory
        {
            Vin = vin, StockNumber = "S-AUC", Cost = 10000m, DateAcquired = new DateTime(2026, 1, 1)
        });
        _context.AuctionRecords.Add(new AuctionRecord
        {
            Vin = vin, HammerPrice = 9000m, AuctionDate = new DateTime(2026, 2, 1), Condition = "Good"
        });
        await _context.SaveChangesAsync();

        var result = await _sut.GetByVinAsync(vin);

        Assert.NotNull(result);
        Assert.Equal(VehicleStatus.Auctioned, result!.Status);
        Assert.Equal(9000m, result.HammerPrice);
        Assert.Null(result.SalePrice);
        Assert.Null(result.ProfitMargin);
    }

    [Fact]
    public async Task Status_Sold_WhenSaleExists()
    {
        const string vin = "TESTVIN00000SOLD1";
        _context.DealerInventory.Add(new DealerInventory
        {
            Vin = vin, StockNumber = "S-SOLD", Cost = 10000m, DateAcquired = new DateTime(2026, 1, 1)
        });
        _context.AuctionRecords.Add(new AuctionRecord
        {
            Vin = vin, HammerPrice = 9000m, AuctionDate = new DateTime(2026, 2, 1), Condition = "Good"
        });
        _context.SaleRecords.Add(new SaleRecord
        {
            Vin = vin, SalePrice = 12500m, DaysOnLot = 30, SoldDate = new DateTime(2026, 3, 1)
        });
        await _context.SaveChangesAsync();

        var result = await _sut.GetByVinAsync(vin);

        Assert.NotNull(result);
        Assert.Equal(VehicleStatus.Sold, result!.Status);
        Assert.Equal(12500m, result.SalePrice);
        Assert.Equal(2500m, result.ProfitMargin);
    }

    [Fact]
    public async Task GetByVinAsync_ReturnsNull_WhenVinDoesNotExist()
    {
        var result = await _sut.GetByVinAsync("NOSUCHVIN000000000");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleAuctionRecordsForSameVin_ReturnsSingleRowUsingMostRecentAuction()
    {
        // Regression test for the actual duplicate-row bug this project found
        // and fixed this session: AuctionRecord has no uniqueness constraint
        // on Vin, so a VIN re-auctioned after failing to sell produces 2+
        // rows — the merge must resolve to exactly one, the most recent.
        const string vin = "TESTVIN00DUPBUG01";
        _context.DealerInventory.Add(new DealerInventory
        {
            Vin = vin, StockNumber = "S-DUP", Cost = 10000m, DateAcquired = new DateTime(2026, 1, 1)
        });
        _context.AuctionRecords.AddRange(
            new AuctionRecord { Vin = vin, HammerPrice = 9000m, AuctionDate = new DateTime(2026, 2, 1), Condition = "Fair" },
            new AuctionRecord { Vin = vin, HammerPrice = 9500m, AuctionDate = new DateTime(2026, 3, 1), Condition = "Good" });
        await _context.SaveChangesAsync();

        var results = await _sut.GetAllAsync();
        var matches = results.Where(v => v.Vin == vin).ToList();

        var match = Assert.Single(matches);
        Assert.Equal(9500m, match.HammerPrice);
        Assert.Equal(new DateTime(2026, 3, 1), match.AuctionDate);
        Assert.Equal("Good", match.Condition);
    }

    [Fact]
    public async Task GetAllAsync_WithIdenticalAuctionDatesForSameVin_UsesHighestIdAsTiebreaker()
    {
        const string vin = "TESTVIN00TIEBRK01";
        var sharedDate = new DateTime(2026, 2, 15);
        _context.DealerInventory.Add(new DealerInventory
        {
            Vin = vin, StockNumber = "S-TIE", Cost = 10000m, DateAcquired = new DateTime(2026, 1, 1)
        });
        await _context.SaveChangesAsync();

        // Two separate SaveChangesAsync calls (not one AddRange) so identity
        // assigns the second row a strictly higher Id — exercising the
        // ThenByDescending(Id) tiebreak for same-timestamp records.
        _context.AuctionRecords.Add(new AuctionRecord
        {
            Vin = vin, HammerPrice = 8000m, AuctionDate = sharedDate, Condition = "Fair"
        });
        await _context.SaveChangesAsync();

        _context.AuctionRecords.Add(new AuctionRecord
        {
            Vin = vin, HammerPrice = 8800m, AuctionDate = sharedDate, Condition = "Good"
        });
        await _context.SaveChangesAsync();

        var result = await _sut.GetByVinAsync(vin);

        Assert.NotNull(result);
        Assert.Equal(8800m, result!.HammerPrice);
        Assert.Equal("Good", result.Condition);
    }
}
