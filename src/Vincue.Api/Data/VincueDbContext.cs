using Microsoft.EntityFrameworkCore;
using Vin.Api.Models;

namespace Vin.Api.Data;
// This is the "unit of work" for the whole database — one class that both
// (a) describes the schema (via the DbSets below) and (b) tracks every
// entity loaded through it so SaveChanges knows what changed. There's exactly
// one of these types in the app; everything (seeder, aggregation service)
// talks to the database only through it, never through raw SQL.

public class VinDbContext : DbContext
{
     // This constructor doesn't configure anything itself — it just forwards
    // DbContextOptions to the base DbContext class. The actual configuration
    // (which database, which connection string) is supplied from outside,
    // in Program.cs:
    //
    //   builder.Services.AddDbContext<VinDbContext>(options =>
    //       options.UseSqlServer(builder.Configuration.GetConnectionString("VinDb")));
    //
    // That's dependency injection: VinDbContext doesn't know or care that
    // it's talking to SQL Server, or where the connection string comes from
    // (appsettings.json's ConnectionStrings:VinDb). Swapping to a different
    // database, or pointing at a different connection string per environment,
    // never touches this file.
    public VinDbContext(DbContextOptions<VinDbContext> options) : base(options)
    {
    }
    // Each DbSet<T> is both "the C# handle you LINQ-query against" and "the
    // thing EF Core's migrations read to know what tables should exist."
    // `Set<T>()` is just the modern way to expose a DbSet without needing a
    // backing field or an auto-property setter — functionally identical to
    // `public DbSet<DealerInventory> DealerInventory { get; set; }`, just
    // avoids a nullable-reference warning since there's no constructor-time
    // assignment.

    public DbSet<DealerInventory> DealerInventory => Set<DealerInventory>();
    // Naming note: EF Core doesn't care that this property is "AuctionRecords"
    // (plural) while the entity class is "AuctionRecord" (singular) — the
    // DbSet property name has no required relationship to the class name.
    // It only matters for readability (`_context.AuctionRecords` reads better
    // than `_context.AuctionRecord` when you're querying a collection).
    public DbSet<AuctionRecord> AuctionRecords => Set<AuctionRecord>();
    public DbSet<SaleRecord> SaleRecords => Set<SaleRecord>();

    // Vin is the natural key every merge query filters/joins/partitions on,
    // but it has no uniqueness constraint (see AuctionRecord.cs) — this is a
    // plain non-unique index purely to speed up lookups, not an integrity
    // constraint.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DealerInventory>()
            .HasIndex(d => d.Vin)
            .HasDatabaseName("IX_DealerInventory_Vin");

        modelBuilder.Entity<SaleRecord>()
            .HasIndex(s => s.Vin)
            .HasDatabaseName("IX_SaleRecords_Vin");

        // Composite, ordered to match how BuildQuery()'s ROW_NUMBER-based
        // dedupe (and the MostRecentAuctionPerVin view) use this table:
        // filter by Vin, then order by AuctionDate.
        //
        // KNOWN LIMITATION, confirmed by testing against ~4,000 synthetic
        // rows (see DatabaseSeeder.SeedBulkDevDataAsync) and reading actual
        // execution plans: this index does NOT get used — AuctionRecords
        // still shows a Clustered Index Scan, even for a single-VIN lookup.
        // Reason: SQL Server generally can't push an equality predicate
        // through a ROW_NUMBER() OVER (PARTITION BY ...) window function
        // boundary, even when the predicate only touches the partition key
        // (which would be provably safe here, since each VIN's ranking is
        // independent of every other VIN's). The optimizer computes the
        // ranking for the entire table first, then filters — so this index
        // never gets a chance to help. By contrast, the plain Vin indexes
        // on DealerInventory and SaleRecords DO produce real Index Seeks,
        // confirmed the same way, since those are ordinary equality
        // lookups with no window function in the way.
        //
        // Fixing this for real would require giving GetByVinAsync a
        // genuinely different query shape than GetAllAsync (a true
        // correlated subquery filtered by Vin before ranking, just for the
        // single-VIN path) — a real architectural fork, deliberately not
        // pursued this round. Documented here as a known, honest limitation
        // rather than silently left unverified.
        modelBuilder.Entity<AuctionRecord>()
            .HasIndex(a => new { a.Vin, a.AuctionDate })
            .HasDatabaseName("IX_AuctionRecords_Vin_AuctionDate");
    }
}
/* The class itself is almost entirely boilerplate — the interesting part is how it's wired up, in Program.cs:

builder.Services.AddDbContext<VinDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("VinDb")));

This registers VinDbContext in the DI container as scoped (one instance per HTTP request — AddDbContext's default lifetime). That's why InventoryAggregationService can just take a VinDbContext in its constructor and get a working, request-scoped instance without ever calling new VinDbContext(...) — the framework builds and disposes it per request.

Then at startup, before the app starts handling requests:

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<VinDbContext>();
    db.Database.Migrate();
    await DatabaseSeeder.SeedAsync(db);
} */