using Microsoft.EntityFrameworkCore;

namespace Vin.Api.Data;

public class VinDbContext : DbContext
{
    public VinDbContext(DbContextOptions<VinDbContext> options) : base(options)
    {
    }
}
