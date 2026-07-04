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