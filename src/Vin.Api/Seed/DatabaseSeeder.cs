using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vin.Api.Data;
using Vin.Api.Models;

namespace Vin.Api.Seed;

public static class DatabaseSeeder
{
     // JSON keys are camelCase ("vin", "stockNumber") but the C# model properties
    // are PascalCase ("Vin", "StockNumber"). Without this, System.Text.Json's
    // default matching is case-sensitive and every property would deserialize
    // to its default value (0, null, etc.) with no error — a classic silent-failure trap.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task SeedAsync(VinDbContext context)
    {
         // Idempotency guard. Program.cs calls this on every app startup (see step 6
        // in Program.cs), and dev workflows restart the app constantly. Without this
        // check, every restart would re-insert all 12+8+6 rows, duplicating data.
        // `.Any()` issues `SELECT TOP(1) 1 FROM DealerInventory` under the hood —
        // cheap existence check, not a full table read.
        if (await Task.FromResult(context.DealerInventory.Any()))
        {
            return;
        }
         // Note: Task.FromResult() here is a no-op — .Any() is already synchronous
        // (EF has no async .Any() being awaited inside), so wrapping it does nothing
        // except make the line look async when it isn't. Harmless, flagged in review,
        // left as-is — not worth a diff for a practice project.

        // AppContext.BaseDirectory = the app's output folder (bin/Debug/net9.0/...).
        // The seed JSON files aren't compiled into the DLL — they're plain files
        // copied there at build time by the csproj's
        // <None Update="Seed\*.json"><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
        // so this path only resolves at runtime, from wherever the app is actually running.
        var seedDirectory = Path.Combine(AppContext.BaseDirectory, "Seed");
         // One generic method loads all three shapes — each source's DTO shape
        // is completely different (see Models/*.cs), but the load mechanics
        // (open file, deserialize array, return list) are identical, so it's
        // parameterized by <T> instead of copy-pasted three times.
        var dealerRecords = await LoadAsync<DealerInventory>(seedDirectory, "dealer-inventory.json");
        var auctionRecords = await LoadAsync<AuctionRecord>(seedDirectory, "auction-feed.json");
        var saleRecords = await LoadAsync<SaleRecord>(seedDirectory, "sales-history.json");
        // AddRange just stages these in EF's change tracker (in-memory) — nothing
        // hits the database yet. Each entity here is now tracked as "Added".
        context.DealerInventory.AddRange(dealerRecords);
        context.AuctionRecords.AddRange(auctionRecords);
        context.SaleRecords.AddRange(saleRecords);
        // The one and only DB round trip. EF Core batches all tracked "Added"
        // entities into a single transaction (SQL Server batches the INSERTs),
        // rather than one round trip per row across three separate SaveChanges calls.
        await context.SaveChangesAsync();
    }

    // Prefix marking synthetic bulk-dev VINs, distinct from the real
    // 17-character-shaped demo VINs — lets this method tell "already seeded"
    // apart from the unrelated real-data seed above, and keeps the two
    // datasets visually distinguishable if ever inspected side by side.
    private const string BulkVinPrefix = "BULKDEV";

    // Deliberately separate from SeedAsync() and never called by it — this
    // exists purely so the Vin indexes added in this project have enough
    // rows to actually change SQL Server's query plan from a scan to a
    // seek, which the realistic 12-vehicle demo dataset is too small to
    // ever trigger. Opt-in only (see Program.cs), gated behind an
    // environment variable, so it never runs for a normal dev/demo session.
    public static async Task SeedBulkDevDataAsync(VinDbContext context, int vehicleCount = 5000)
    {
        // Same idempotency shape as SeedAsync(), scoped to the bulk prefix
        // specifically so this check doesn't collide with the real dataset.
        if (await context.DealerInventory.AnyAsync(d => d.Vin.StartsWith(BulkVinPrefix)))
        {
            return;
        }

        var dealers = new List<DealerInventory>(vehicleCount);
        var auctions = new List<AuctionRecord>();
        var sales = new List<SaleRecord>();
        var baseDate = new DateTime(2024, 1, 1);

        for (var i = 0; i < vehicleCount; i++)
        {
            var vin = $"{BulkVinPrefix}{i:D10}";
            dealers.Add(new DealerInventory
            {
                Vin = vin,
                StockNumber = $"BULK{i}",
                Cost = 10000m + i,
                DateAcquired = baseDate.AddDays(i % 300)
            });

            // ~70% of synthetic vehicles get at least one auction record.
            if (i % 10 < 7)
            {
                auctions.Add(new AuctionRecord
                {
                    Vin = vin,
                    HammerPrice = 9000m + i,
                    AuctionDate = baseDate.AddDays((i % 300) + 5),
                    Condition = "Good"
                });

                // A slice of those get a second, earlier record too — the
                // same re-auctioned-vehicle shape as the real dataset's
                // JN8AZ2NF0K9123457, so the ROW_NUMBER dedupe logic is
                // also exercised at volume, not just correctness-checked
                // on one row.
                if (i % 10 == 0)
                {
                    auctions.Add(new AuctionRecord
                    {
                        Vin = vin,
                        HammerPrice = 8500m + i,
                        AuctionDate = baseDate.AddDays((i % 300) - 5),
                        Condition = "Fair"
                    });
                }
            }

            // ~50% of auctioned vehicles get sold.
            if (i % 10 < 5)
            {
                sales.Add(new SaleRecord
                {
                    Vin = vin,
                    SalePrice = 12000m + i,
                    DaysOnLot = 20 + (i % 15),
                    SoldDate = baseDate.AddDays((i % 300) + 15)
                });
            }
        }

        context.DealerInventory.AddRange(dealers);
        context.AuctionRecords.AddRange(auctions);
        context.SaleRecords.AddRange(sales);
        await context.SaveChangesAsync();
    }

    private static async Task<List<T>> LoadAsync<T>(string seedDirectory, string fileName)
    {
        var path = Path.Combine(seedDirectory, fileName);
         // Streaming read (File.OpenRead + DeserializeAsync from the stream),
        // not File.ReadAllText + JsonSerializer.Deserialize(string). For 12-20
        // rows the difference is irrelevant, but it's the pattern you'd want
        // for a real feed file that might be megabytes.
        await using var stream = File.OpenRead(path);
        var records = await JsonSerializer.DeserializeAsync<List<T>>(stream, JsonOptions);
         // DeserializeAsync returns null for literal JSON `null` (not for `[]`,
        // which deserializes to an empty list). The `??` guards that edge case
        // so callers never have to null-check the returned list.
        return records ?? [];
    }
}


/* Key things to notice for the interview-talking-points angle:
- PropertyNameCaseInsensitive — a one-line fix for a bug class (camelCase/PascalCase mismatch) that fails silently, not loudly.
- The idempotency check (.Any() before seeding) is the same pattern you'd use for any "seed once, safe to rerun" startup logic — same shape as an EF Core migration check.
- AddRange + one SaveChangesAsync vs. three separate calls is the same "batch, don't loop" principle as the aggregation service's single-query merge — this file and InventoryAggregationService are both answers to "avoid N round trips." */