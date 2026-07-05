using Microsoft.EntityFrameworkCore;
using Vin.Api.Data;

namespace Vin.Api.Tests;

// A separate database from the real dev DB (VinInventory) — tests never touch
// production data. Real migrations (not EnsureCreated()) run once per test
// run, so the test schema — indexes, the hand-written view — is byte-for-byte
// what production actually runs.
public class DatabaseFixture : IAsyncLifetime
{
    public const string ConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=VinInventoryTest;Trusted_Connection=True;MultipleActiveResultSets=true";

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<VinDbContext>().UseSqlServer(ConnectionString).Options;
        await using var context = new VinDbContext(options);
        await context.Database.MigrateAsync();
    }

    // Nothing to tear down — the database persists across test runs (Migrate
    // is idempotent), and every test rolls back its own transaction, so
    // there's no accumulated state to clean up here.
    public Task DisposeAsync() => Task.CompletedTask;
}

// All DB-touching test classes share this one collection, which forces
// xUnit to run them sequentially rather than in parallel. This isn't just
// about avoiding cross-test data contamination: every query in
// InventoryAggregationService.BuildQuery() does a full scan of
// AuctionRecords regardless of any Vin filter (see VinDbContext.cs's
// comments on the composite index) — under default READ COMMITTED
// isolation, concurrent transactions touching that table risk lock
// blocking, not just data races. Sequential execution is load-bearing here,
// not just tidy.
[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}
