using System.Text.Json;
using Vin.Api.Data;
using Vin.Api.Models;

namespace Vin.Api.Seed;

public static class DatabaseSeeder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task SeedAsync(VinDbContext context)
    {
        if (await Task.FromResult(context.DealerInventory.Any()))
        {
            return;
        }

        var seedDirectory = Path.Combine(AppContext.BaseDirectory, "Seed");

        var dealerRecords = await LoadAsync<DealerInventory>(seedDirectory, "dealer-inventory.json");
        var auctionRecords = await LoadAsync<AuctionRecord>(seedDirectory, "auction-feed.json");
        var saleRecords = await LoadAsync<SaleRecord>(seedDirectory, "sales-history.json");

        context.DealerInventory.AddRange(dealerRecords);
        context.AuctionRecords.AddRange(auctionRecords);
        context.SaleRecords.AddRange(saleRecords);

        await context.SaveChangesAsync();
    }

    private static async Task<List<T>> LoadAsync<T>(string seedDirectory, string fileName)
    {
        var path = Path.Combine(seedDirectory, fileName);
        await using var stream = File.OpenRead(path);
        var records = await JsonSerializer.DeserializeAsync<List<T>>(stream, JsonOptions);
        return records ?? [];
    }
}
