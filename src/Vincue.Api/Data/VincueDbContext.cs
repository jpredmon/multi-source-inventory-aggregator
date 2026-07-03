using Microsoft.EntityFrameworkCore;
using Vin.Api.Models;

namespace Vin.Api.Data;

public class VinDbContext : DbContext
{
    public VinDbContext(DbContextOptions<VinDbContext> options) : base(options)
    {
    }

    public DbSet<DealerInventory> DealerInventory => Set<DealerInventory>();
    public DbSet<AuctionRecord> AuctionRecords => Set<AuctionRecord>();
    public DbSet<SaleRecord> SaleRecords => Set<SaleRecord>();
}
