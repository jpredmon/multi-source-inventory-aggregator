# Multi-Source Inventory Data Aggregation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build an ASP.NET Core 9 API that merges three differently-shaped mock inventory sources (dealer, auction, sales) by VIN into a unified view, with a minimal Angular table to display it.

**Architecture:** Three EF Core entities mirror each source's native shape exactly. A single aggregation service performs a query-time left-join merge (dealer inventory as anchor) projected into one DTO, exposed via two read-only endpoints. Angular calls the API once and renders a static table.

**Tech Stack:** .NET 9, ASP.NET Core Web API (controllers), EF Core 9, SQL Server LocalDB, Angular (current version via local devDependency), TypeScript.

## Global Constraints

- Target framework: net9.0 (already installed SDK).
- Database: SQL Server LocalDB, connection string name `VinDb`.
- No authentication, no write/update endpoints — read-only aggregation only.
- No automated test suite in this pass (spec explicitly deprioritizes it for the 2.5-day budget); every task instead ends with a manual, exact verification command and expected output.
- Angular CLI must be a local devDependency of `src/vin-web`, not the machine's stale global v9.0.5.
- API runs on fixed port `http://localhost:5080` (no HTTPS profile) — avoids dev-cert trust prompts for a local-only practice project.
- Angular dev server runs on the Angular CLI default, `http://localhost:4200`.

---

### Task 1: Solution and Web API scaffold

**Files:**
- Create: `vin.sln`
- Create: `src/Vin.Api/` (via `dotnet new webapi`)
- Modify: `src/Vin.Api/Properties/launchSettings.json`
- Delete: `src/Vin.Api/WeatherForecast.cs`
- Delete: `src/Vin.Api/Controllers/WeatherForecastController.cs`

**Interfaces:**
- Produces: a running ASP.NET Core app listening on `http://localhost:5080`, ready for later tasks to add services/controllers.

- [ ] **Step 1: Create the solution file**

```bash
cd "C:/dev/claude-practice/vin"
dotnet new sln -n vin
```

- [ ] **Step 2: Scaffold the Web API project with controllers (not minimal APIs)**

```bash
dotnet new webapi -n Vin.Api -o src/Vin.Api -controllers --no-https
dotnet sln vin.sln add src/Vin.Api/Vin.Api.csproj
```

- [ ] **Step 3: Fix the launch profile to a single, fixed HTTP port**

Open `src/Vin.Api/Properties/launchSettings.json` and replace its contents:

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5080",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

- [ ] **Step 4: Delete the scaffolded weather forecast sample**

```bash
rm src/Vin.Api/WeatherForecast.cs
rm src/Vin.Api/Controllers/WeatherForecastController.cs
```

- [ ] **Step 5: Verify it builds and runs on the fixed port**

```bash
dotnet build vin.sln
dotnet run --project src/Vin.Api
```

Expected: build succeeds with 0 errors; console shows `Now listening on: http://localhost:5080`. Stop the process (Ctrl+C) once confirmed.

- [ ] **Step 6: Commit**

```bash
git add vin.sln src/Vin.Api
git commit -m "feat: scaffold Vin.Api web project"
```

---

### Task 2: EF Core + LocalDB wiring

**Files:**
- Modify: `src/Vin.Api/Vin.Api.csproj` (add packages)
- Create: `src/Vin.Api/Data/VinDbContext.cs`
- Modify: `src/Vin.Api/appsettings.json` (add connection string)
- Modify: `src/Vin.Api/Program.cs` (register DbContext)
- Create: `dotnet-tools.json` (local tool manifest, repo root)

**Interfaces:**
- Produces: `VinDbContext` (empty, no `DbSet`s yet — added in Task 3), registered in DI, connectable to LocalDB.

- [ ] **Step 1: Add EF Core SQL Server + Design packages**

```bash
cd "C:/dev/claude-practice/vin"
dotnet add src/Vin.Api package Microsoft.EntityFrameworkCore.SqlServer
dotnet add src/Vin.Api package Microsoft.EntityFrameworkCore.Design
```

- [ ] **Step 2: Install dotnet-ef as a local (repo-scoped) tool**

```bash
dotnet new tool-manifest
dotnet tool install --local dotnet-ef
```

- [ ] **Step 3: Add the LocalDB connection string**

Open `src/Vin.Api/appsettings.json` and add a `ConnectionStrings` section (keep the existing `Logging`/`AllowedHosts` keys):

```json
{
  "ConnectionStrings": {
    "VinDb": "Server=(localdb)\\mssqllocaldb;Database=VinInventory;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

- [ ] **Step 4: Create the empty DbContext**

Create `src/Vin.Api/Data/VinDbContext.cs`:

```csharp
using Microsoft.EntityFrameworkCore;

namespace Vin.Api.Data;

public class VinDbContext : DbContext
{
    public VinDbContext(DbContextOptions<VinDbContext> options) : base(options)
    {
    }
}
```

- [ ] **Step 5: Register the DbContext in Program.cs**

Open `src/Vin.Api/Program.cs`. Add this using at the top:

```csharp
using Microsoft.EntityFrameworkCore;
using Vin.Api.Data;
```

Add this line before `var app = builder.Build();`:

```csharp
builder.Services.AddDbContext<VinDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("VinDb")));
```

- [ ] **Step 6: Verify it builds**

```bash
dotnet build vin.sln
```

Expected: build succeeds with 0 errors.

- [ ] **Step 7: Commit**

```bash
git add .
git commit -m "feat: wire up EF Core and LocalDB connection"
```

---

### Task 3: Entity models and initial migration

**Files:**
- Create: `src/Vin.Api/Models/DealerInventory.cs`
- Create: `src/Vin.Api/Models/AuctionRecord.cs`
- Create: `src/Vin.Api/Models/SaleRecord.cs`
- Modify: `src/Vin.Api/Data/VinDbContext.cs` (add `DbSet`s)
- Create: `src/Vin.Api/Migrations/*` (via `dotnet ef migrations add`)

**Interfaces:**
- Produces: `DealerInventory { Id, Vin, StockNumber, Cost, DateAcquired }`, `AuctionRecord { Id, Vin, HammerPrice, AuctionDate, Condition }`, `SaleRecord { Id, Vin, SalePrice, DaysOnLot, SoldDate }`. Later tasks (seeding, aggregation service) depend on these exact property names and types.

- [ ] **Step 1: Create the DealerInventory entity**

Create `src/Vin.Api/Models/DealerInventory.cs`:

```csharp
namespace Vin.Api.Models;

public class DealerInventory
{
    public int Id { get; set; }
    public string Vin { get; set; } = string.Empty;
    public string StockNumber { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public DateTime DateAcquired { get; set; }
}
```

- [ ] **Step 2: Create the AuctionRecord entity**

Create `src/Vin.Api/Models/AuctionRecord.cs`:

```csharp
namespace Vin.Api.Models;

public class AuctionRecord
{
    public int Id { get; set; }
    public string Vin { get; set; } = string.Empty;
    public decimal HammerPrice { get; set; }
    public DateTime AuctionDate { get; set; }
    public string Condition { get; set; } = string.Empty;
}
```

- [ ] **Step 3: Create the SaleRecord entity**

Create `src/Vin.Api/Models/SaleRecord.cs`:

```csharp
namespace Vin.Api.Models;

public class SaleRecord
{
    public int Id { get; set; }
    public string Vin { get; set; } = string.Empty;
    public decimal SalePrice { get; set; }
    public int DaysOnLot { get; set; }
    public DateTime SoldDate { get; set; }
}
```

- [ ] **Step 4: Add DbSets to VinDbContext**

Replace the contents of `src/Vin.Api/Data/VinDbContext.cs`:

```csharp
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
```

- [ ] **Step 5: Create and apply the initial migration**

```bash
cd "C:/dev/claude-practice/vin"
dotnet ef migrations add InitialCreate -p src/Vin.Api -s src/Vin.Api
dotnet ef database update -p src/Vin.Api -s src/Vin.Api
```

Expected: migration files appear under `src/Vin.Api/Migrations/`, and the second command ends with `Done.` (creates the `VinInventory` database and its three tables in LocalDB).

- [ ] **Step 6: Verify the tables exist**

```bash
dotnet ef dbcontext info -p src/Vin.Api -s src/Vin.Api
```

Expected: prints `VinDbContext` info with no errors, confirming the context connects to the created database.

- [ ] **Step 7: Commit**

```bash
git add .
git commit -m "feat: add DealerInventory, AuctionRecord, SaleRecord entities and initial migration"
```

---

### Task 4: Seed JSON data and startup seeding

**Files:**
- Create: `src/Vin.Api/Seed/dealer-inventory.json`
- Create: `src/Vin.Api/Seed/auction-feed.json`
- Create: `src/Vin.Api/Seed/sales-history.json`
- Create: `src/Vin.Api/Seed/DatabaseSeeder.cs`
- Modify: `src/Vin.Api/Vin.Api.csproj` (copy JSON to output)
- Modify: `src/Vin.Api/Program.cs` (run migration + seed on startup)

**Interfaces:**
- Consumes: `VinDbContext` from Task 2/3 (`DealerInventory`, `AuctionRecords`, `SaleRecords` DbSets).
- Produces: `DatabaseSeeder.SeedAsync(VinDbContext context)` — idempotent, called once at startup.

Twelve dealer vehicles. Eight of those get an auction record. Six of those eight get a sale record. This yields the three status tiers (OnLot / Auctioned / Sold) plus the timestamp-format mismatch called out in the spec: dealer dates are date-only, auction dates are UTC `Z` timestamps, sale dates use a `-05:00` offset.

- [ ] **Step 1: Create the dealer inventory seed file**

Create `src/Vin.Api/Seed/dealer-inventory.json`:

```json
[
  { "vin": "1G1ZD5ST0LF123456", "stockNumber": "D1001", "cost": 14250.00, "dateAcquired": "2025-01-15" },
  { "vin": "1HGCV1F34LA123457", "stockNumber": "D1002", "cost": 16800.00, "dateAcquired": "2025-01-18" },
  { "vin": "3VW2B7AJ9FM123458", "stockNumber": "D1003", "cost": 9800.00,  "dateAcquired": "2025-01-22" },
  { "vin": "5YJ3E1EA8KF123459", "stockNumber": "D1004", "cost": 32250.00, "dateAcquired": "2025-02-01" },
  { "vin": "1FTFW1ET5DFC12345", "stockNumber": "D1005", "cost": 21400.00, "dateAcquired": "2025-02-05" },
  { "vin": "2T1BURHE0JC123456", "stockNumber": "D1006", "cost": 13100.00, "dateAcquired": "2025-02-10" },
  { "vin": "JN8AZ2NF0K9123457", "stockNumber": "D1007", "cost": 18700.00, "dateAcquired": "2025-02-14" },
  { "vin": "1C4RJFAG3FC123458", "stockNumber": "D1008", "cost": 24600.00, "dateAcquired": "2025-02-20" },
  { "vin": "4T1BF1FK5FU123459", "stockNumber": "D1009", "cost": 15950.00, "dateAcquired": "2025-03-01" },
  { "vin": "WBA8E9C50GK123460", "stockNumber": "D1010", "cost": 27300.00, "dateAcquired": "2025-03-06" },
  { "vin": "5FNRL6H90KB123461", "stockNumber": "D1011", "cost": 19850.00, "dateAcquired": "2025-03-12" },
  { "vin": "1N4AL3AP0JC123462", "stockNumber": "D1012", "cost": 11200.00, "dateAcquired": "2025-03-18" }
]
```

- [ ] **Step 2: Create the auction feed seed file**

Create `src/Vin.Api/Seed/auction-feed.json` (8 of the 12 VINs above — the last 4 dealer VINs stay auction-less, i.e. status `OnLot`):

```json
[
  { "vin": "1G1ZD5ST0LF123456", "hammerPrice": 12800.00, "auctionDate": "2025-01-10T14:30:00Z", "condition": "Good" },
  { "vin": "1HGCV1F34LA123457", "hammerPrice": 15400.00, "auctionDate": "2025-01-14T16:00:00Z", "condition": "Excellent" },
  { "vin": "3VW2B7AJ9FM123458", "hammerPrice": 8900.00,  "auctionDate": "2025-01-19T13:15:00Z", "condition": "Fair" },
  { "vin": "5YJ3E1EA8KF123459", "hammerPrice": 30100.00, "auctionDate": "2025-01-28T15:45:00Z", "condition": "Excellent" },
  { "vin": "1FTFW1ET5DFC12345", "hammerPrice": 19700.00, "auctionDate": "2025-02-02T12:00:00Z", "condition": "Good" },
  { "vin": "2T1BURHE0JC123456", "hammerPrice": 11950.00, "auctionDate": "2025-02-07T17:30:00Z", "condition": "Good" },
  { "vin": "JN8AZ2NF0K9123457", "hammerPrice": 17200.00, "auctionDate": "2025-02-11T14:00:00Z", "condition": "Fair" },
  { "vin": "1C4RJFAG3FC123458", "hammerPrice": 22900.00, "auctionDate": "2025-02-17T16:20:00Z", "condition": "Excellent" }
]
```

- [ ] **Step 3: Create the sales history seed file**

Create `src/Vin.Api/Seed/sales-history.json` (6 of the 8 auctioned VINs — the last 2 stay auction-only, i.e. status `Auctioned`):

```json
[
  { "vin": "1G1ZD5ST0LF123456", "salePrice": 16995.00, "daysOnLot": 22, "soldDate": "2025-02-06T09:15:00-05:00" },
  { "vin": "1HGCV1F34LA123457", "salePrice": 19495.00, "daysOnLot": 15, "soldDate": "2025-02-02T11:00:00-05:00" },
  { "vin": "3VW2B7AJ9FM123458", "salePrice": 11995.00, "daysOnLot": 30, "soldDate": "2025-02-21T10:30:00-05:00" },
  { "vin": "5YJ3E1EA8KF123459", "salePrice": 35995.00, "daysOnLot": 18, "soldDate": "2025-02-19T14:45:00-05:00" },
  { "vin": "1FTFW1ET5DFC12345", "salePrice": 24495.00, "daysOnLot": 25, "soldDate": "2025-03-02T13:00:00-05:00" },
  { "vin": "2T1BURHE0JC123456", "salePrice": 15495.00, "daysOnLot": 20, "soldDate": "2025-03-02T16:00:00-05:00" }
]
```

- [ ] **Step 4: Configure the csproj to copy the seed JSON to the output directory**

Open `src/Vin.Api/Vin.Api.csproj` and add this `ItemGroup` inside the `<Project>` element:

```xml
<ItemGroup>
  <None Update="Seed\*.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

- [ ] **Step 5: Write the seeder**

Create `src/Vin.Api/Seed/DatabaseSeeder.cs`:

```csharp
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
```

- [ ] **Step 6: Call the seeder on startup**

Open `src/Vin.Api/Program.cs`. Add this using:

```csharp
using Vin.Api.Seed;
```

Add this block right after `var app = builder.Build();` and before `app.Run()` (or before the `if (app.Environment.IsDevelopment())` block if present):

```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<VinDbContext>();
    db.Database.Migrate();
    await DatabaseSeeder.SeedAsync(db);
}
```

- [ ] **Step 7: Verify seeding runs without error, twice**

Run the app, confirm clean startup, stop it, then run it again to confirm the `context.DealerInventory.Any()` check prevents re-seeding on the second run:

```bash
cd "C:/dev/claude-practice/vin"
dotnet run --project src/Vin.Api &
sleep 5
taskkill //F //IM dotnet.exe //T
dotnet run --project src/Vin.Api &
sleep 5
taskkill //F //IM dotnet.exe //T
```

Expected: both runs print `Now listening on: http://localhost:5080` with no unhandled exceptions in the console output. (Row-level verification — confirming exactly 12 vehicles come through the merge — happens in Task 5's curl check once the endpoint exists.)

- [ ] **Step 8: Commit**

```bash
git add .
git commit -m "feat: add mock source seed data and startup seeding"
```

---

### Task 5: Aggregation service and inventory controller

**Files:**
- Create: `src/Vin.Api/Dtos/VehicleSummaryDto.cs`
- Create: `src/Vin.Api/Services/IInventoryAggregationService.cs`
- Create: `src/Vin.Api/Services/InventoryAggregationService.cs`
- Create: `src/Vin.Api/Controllers/InventoryController.cs`
- Modify: `src/Vin.Api/Program.cs` (register service + CORS)

**Interfaces:**
- Consumes: `VinDbContext` (Task 2/3), seeded data (Task 4).
- Produces: `IInventoryAggregationService.GetAllAsync(): Task<List<VehicleSummaryDto>>` and `GetByVinAsync(string vin): Task<VehicleSummaryDto?>` — the Angular service (Task 8) depends on this DTO's JSON shape.

- [ ] **Step 1: Create the DTO and status enum**

Create `src/Vin.Api/Dtos/VehicleSummaryDto.cs`:

```csharp
namespace Vin.Api.Dtos;

public enum VehicleStatus
{
    OnLot,
    Auctioned,
    Sold
}

public class VehicleSummaryDto
{
    public string Vin { get; set; } = string.Empty;
    public string StockNumber { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public DateTime DateAcquired { get; set; }

    public decimal? HammerPrice { get; set; }
    public DateTime? AuctionDate { get; set; }
    public string? Condition { get; set; }

    public decimal? SalePrice { get; set; }
    public int? DaysOnLot { get; set; }
    public DateTime? SoldDate { get; set; }

    public VehicleStatus Status { get; set; }
}
```

- [ ] **Step 2: Define the service interface**

Create `src/Vin.Api/Services/IInventoryAggregationService.cs`:

```csharp
using Vin.Api.Dtos;

namespace Vin.Api.Services;

public interface IInventoryAggregationService
{
    Task<List<VehicleSummaryDto>> GetAllAsync();
    Task<VehicleSummaryDto?> GetByVinAsync(string vin);
}
```

- [ ] **Step 3: Implement the query-time merge**

Create `src/Vin.Api/Services/InventoryAggregationService.cs`:

```csharp
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
```

- [ ] **Step 4: Create the controller**

Create `src/Vin.Api/Controllers/InventoryController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using Vin.Api.Dtos;
using Vin.Api.Services;

namespace Vin.Api.Controllers;

[ApiController]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryAggregationService _service;

    public InventoryController(IInventoryAggregationService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<VehicleSummaryDto>>> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{vin}")]
    public async Task<ActionResult<VehicleSummaryDto>> GetByVin(string vin)
    {
        var result = await _service.GetByVinAsync(vin);
        return result is null ? NotFound() : Ok(result);
    }
}
```

- [ ] **Step 5: Register the service and CORS in Program.cs**

Open `src/Vin.Api/Program.cs`. Add this using:

```csharp
using Vin.Api.Services;
```

Add this line near the other `builder.Services.Add...` calls:

```csharp
builder.Services.AddScoped<IInventoryAggregationService, InventoryAggregationService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularDev", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});
```

Add this line after `var app = builder.Build();` and before `app.MapControllers();`:

```csharp
app.UseCors("AngularDev");
```

- [ ] **Step 6: Verify both endpoints manually**

```bash
cd "C:/dev/claude-practice/vin"
dotnet run --project src/Vin.Api &
sleep 5
curl -s http://localhost:5080/api/inventory | head -c 500
echo
curl -s http://localhost:5080/api/inventory/1G1ZD5ST0LF123456
echo
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5080/api/inventory/DOESNOTEXIST
taskkill //F //IM dotnet.exe //T
```

Expected: first curl returns a JSON array of 12 vehicles; second returns one vehicle with non-null `hammerPrice` and `salePrice` (that VIN is one of the 6 fully-sold records); third prints `404`.

- [ ] **Step 7: Commit**

```bash
git add .
git commit -m "feat: add inventory aggregation service and controller"
```

---

### Task 6: VS Code debugging config for the API

**Files:**
- Create: `.vscode/launch.json`
- Create: `.vscode/tasks.json`
- Create: `.vscode/extensions.json`

**Interfaces:**
- Produces: `.vscode/extensions.json` — Task 9 appends the Angular Language Service recommendation to this same file.

- [ ] **Step 1: Create tasks.json (build task the debugger depends on)**

Create `.vscode/tasks.json`:

```json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "build-api",
      "type": "process",
      "command": "dotnet",
      "args": ["build", "${workspaceFolder}/src/Vin.Api/Vin.Api.csproj"],
      "problemMatcher": "$msCompile",
      "group": {
        "kind": "build",
        "isDefault": true
      }
    }
  ]
}
```

- [ ] **Step 2: Create launch.json**

Create `.vscode/launch.json`:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Debug Vin.Api",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build-api",
      "program": "${workspaceFolder}/src/Vin.Api/bin/Debug/net9.0/Vin.Api.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/Vin.Api",
      "stopAtEntry": false,
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  ]
}
```

- [ ] **Step 3: Create extensions.json**

Create `.vscode/extensions.json`:

```json
{
  "recommendations": [
    "ms-dotnettools.csdevkit"
  ]
}
```

- [ ] **Step 4: Verify the build task runs**

```bash
dotnet build "C:/dev/claude-practice/vin/src/Vin.Api/Vin.Api.csproj"
```

Expected: succeeds with 0 errors (this is exactly what the `build-api` task and `preLaunchTask` will invoke). Also confirm all three files are valid JSON:

```bash
node -e "JSON.parse(require('fs').readFileSync('.vscode/launch.json'))" && echo OK
node -e "JSON.parse(require('fs').readFileSync('.vscode/tasks.json'))" && echo OK
node -e "JSON.parse(require('fs').readFileSync('.vscode/extensions.json'))" && echo OK
```

Expected: prints `OK` three times.

- [ ] **Step 5: Commit**

```bash
git add .vscode
git commit -m "chore: add VS Code debug config for Vin.Api"
```

---

### Task 7: Angular app scaffold

**Files:**
- Create: `src/vin-web/` (via Angular CLI, run from `src/`)

**Interfaces:**
- Produces: a runnable Angular app at `src/vin-web`, serving on `http://localhost:4200`, with `@angular/cli` as a local devDependency.

- [ ] **Step 1: Scaffold the Angular app with a current CLI, no routing, no SSR**

```bash
cd "C:/dev/claude-practice/vin/src"
npx -y @angular/cli@latest new vin-web --routing=false --style=css --ssr=false --skip-git
```

- [ ] **Step 2: Pin the Angular CLI as a local devDependency**

```bash
cd "C:/dev/claude-practice/vin/src/vin-web"
npm install --save-dev @angular/cli
```

- [ ] **Step 3: Verify the default app runs on port 4200**

```bash
cd "C:/dev/claude-practice/vin/src/vin-web"
npx ng serve &
sleep 15
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:4200
taskkill //F //IM node.exe //T
```

Expected: prints `200`.

- [ ] **Step 4: Commit**

```bash
cd "C:/dev/claude-practice/vin"
git add src/vin-web
git commit -m "feat: scaffold vin-web Angular app"
```

---

### Task 8: Angular inventory feature (service, model, table)

**Files:**
- Create: `src/vin-web/src/app/inventory/inventory.model.ts`
- Create: `src/vin-web/src/app/inventory/inventory.service.ts`
- Create: `src/vin-web/src/app/inventory/inventory-table/inventory-table.component.ts`
- Create: `src/vin-web/src/app/inventory/inventory-table/inventory-table.component.html`
- Create: `src/vin-web/src/app/inventory/inventory-table/inventory-table.component.css`
- Modify: `src/vin-web/src/app/app.config.ts` (provide `HttpClient`)
- Modify: `src/vin-web/src/app/app.ts` (host the table component)
- Modify: `src/vin-web/src/app/app.html`

**Interfaces:**
- Consumes: `GET http://localhost:5080/api/inventory` (Task 5), returning JSON matching `VehicleSummaryDto`.
- Produces: `InventoryTableComponent`, standalone, rendered from `app.html`.

- [ ] **Step 1: Create the TS model matching VehicleSummaryDto**

Create `src/vin-web/src/app/inventory/inventory.model.ts`:

```typescript
export type VehicleStatus = 'OnLot' | 'Auctioned' | 'Sold';

export interface VehicleSummary {
  vin: string;
  stockNumber: string;
  cost: number;
  dateAcquired: string;

  hammerPrice: number | null;
  auctionDate: string | null;
  condition: string | null;

  salePrice: number | null;
  daysOnLot: number | null;
  soldDate: string | null;

  status: VehicleStatus;
}
```

- [ ] **Step 2: Create the inventory service**

Create `src/vin-web/src/app/inventory/inventory.service.ts`:

```typescript
import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import type { VehicleSummary } from './inventory.model';

const API_BASE_URL = 'http://localhost:5080/api/inventory';

@Injectable({ providedIn: 'root' })
export class InventoryService {
  constructor(private readonly http: HttpClient) {}

  getAll(): Observable<VehicleSummary[]> {
    return this.http.get<VehicleSummary[]>(API_BASE_URL);
  }
}
```

- [ ] **Step 3: Provide HttpClient in app.config.ts**

Open `src/vin-web/src/app/app.config.ts`. Add this import:

```typescript
import { provideHttpClient } from '@angular/common/http';
```

Add `provideHttpClient()` to the `providers` array in `appConfig`.

- [ ] **Step 4: Create the table component**

Create `src/vin-web/src/app/inventory/inventory-table/inventory-table.component.ts`:

```typescript
import { CommonModule } from '@angular/common';
import { Component, type OnInit } from '@angular/core';

import { InventoryService } from '../inventory.service';
import type { VehicleSummary } from '../inventory.model';

@Component({
  selector: 'app-inventory-table',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './inventory-table.component.html',
  styleUrl: './inventory-table.component.css'
})
export class InventoryTableComponent implements OnInit {
  vehicles: VehicleSummary[] = [];

  constructor(private readonly inventoryService: InventoryService) {}

  ngOnInit(): void {
    this.inventoryService.getAll().subscribe((vehicles) => {
      this.vehicles = vehicles;
    });
  }
}
```

- [ ] **Step 5: Create the table template**

Create `src/vin-web/src/app/inventory/inventory-table/inventory-table.component.html`:

```html
<table>
  <thead>
    <tr>
      <th>VIN</th>
      <th>Stock #</th>
      <th>Cost</th>
      <th>Date Acquired</th>
      <th>Hammer Price</th>
      <th>Auction Date</th>
      <th>Condition</th>
      <th>Sale Price</th>
      <th>Days On Lot</th>
      <th>Sold Date</th>
      <th>Status</th>
    </tr>
  </thead>
  <tbody>
    <tr *ngFor="let vehicle of vehicles">
      <td>{{ vehicle.vin }}</td>
      <td>{{ vehicle.stockNumber }}</td>
      <td>{{ vehicle.cost | currency }}</td>
      <td>{{ vehicle.dateAcquired | date: 'shortDate' }}</td>
      <td>{{ vehicle.hammerPrice != null ? (vehicle.hammerPrice | currency) : '—' }}</td>
      <td>{{ vehicle.auctionDate != null ? (vehicle.auctionDate | date: 'shortDate') : '—' }}</td>
      <td>{{ vehicle.condition ?? '—' }}</td>
      <td>{{ vehicle.salePrice != null ? (vehicle.salePrice | currency) : '—' }}</td>
      <td>{{ vehicle.daysOnLot ?? '—' }}</td>
      <td>{{ vehicle.soldDate != null ? (vehicle.soldDate | date: 'shortDate') : '—' }}</td>
      <td>{{ vehicle.status }}</td>
    </tr>
  </tbody>
</table>
```

- [ ] **Step 6: Add minimal readable styling**

Create `src/vin-web/src/app/inventory/inventory-table/inventory-table.component.css`:

```css
table {
  border-collapse: collapse;
  width: 100%;
  font-family: sans-serif;
  font-size: 14px;
}

th, td {
  border: 1px solid #ccc;
  padding: 6px 10px;
  text-align: left;
}

th {
  background-color: #f2f2f2;
}
```

- [ ] **Step 7: Host the table in the root component**

Open `src/vin-web/src/app/app.ts`. Add this import:

```typescript
import { InventoryTableComponent } from './inventory/inventory-table/inventory-table.component';
```

Add `InventoryTableComponent` to the component's `imports` array.

Open `src/vin-web/src/app/app.html` and replace its entire contents:

```html
<h1>Vin Inventory</h1>
<app-inventory-table></app-inventory-table>
```

- [ ] **Step 8: Verify end-to-end in the browser**

```bash
cd "C:/dev/claude-practice/vin"
dotnet run --project src/Vin.Api &
cd src/vin-web
npx ng serve &
sleep 15
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:4200
taskkill //F //IM dotnet.exe //T
taskkill //F //IM node.exe //T
```

Then manually open `http://localhost:4200` in a browser and confirm: a table with 12 rows renders, the 4 non-auctioned vehicles show `—` for auction/sale columns and status `OnLot`, the 2 auctioned-but-unsold vehicles show `—` only in sale columns and status `Auctioned`, and the 6 sold vehicles have every column populated with status `Sold`.

- [ ] **Step 9: Commit**

```bash
cd "C:/dev/claude-practice/vin"
git add src/vin-web
git commit -m "feat: add inventory table component wired to the API"
```

---

### Task 9: Angular VS Code tooling and README

**Files:**
- Modify: `.vscode/extensions.json`
- Create: `README.md`

**Interfaces:**
- None (final task — no downstream consumers).

- [ ] **Step 1: Add Angular Language Service to the recommended extensions**

Replace the contents of `.vscode/extensions.json`:

```json
{
  "recommendations": [
    "ms-dotnettools.csdevkit",
    "angular.ng-template"
  ]
}
```

- [ ] **Step 2: Write the README**

Create `README.md`:

```markdown
# Vin — Multi-Source Inventory Data Aggregation

ASP.NET Core 9 API that merges mock dealer, auction, and sales data by VIN
into a unified inventory view, with an Angular table to display it.

## Prerequisites

- .NET 9 SDK
- Node.js 22+
- SQL Server LocalDB (ships with SQL Server Express/Developer or Visual Studio)

## Running the API

```bash
dotnet ef database update -p src/Vin.Api -s src/Vin.Api
dotnet run --project src/Vin.Api
```

API listens on `http://localhost:5080`. On first run it seeds `VinInventory`
in LocalDB from the JSON files in `src/Vin.Api/Seed/`.

- `GET /api/inventory` — all vehicles, merged across sources
- `GET /api/inventory/{vin}` — single vehicle, 404 if VIN not found

## Running the Angular app

```bash
cd src/vin-web
npm install
npx ng serve
```

Open `http://localhost:4200`. The app expects the API to already be running
on `http://localhost:5080`.

## Resetting the database

```bash
dotnet ef database drop -p src/Vin.Api -s src/Vin.Api
dotnet ef database update -p src/Vin.Api -s src/Vin.Api
```
```

- [ ] **Step 3: Verify the JSON is valid**

```bash
node -e "JSON.parse(require('fs').readFileSync('.vscode/extensions.json'))" && echo OK
```

Expected: prints `OK`.

- [ ] **Step 4: Commit**

```bash
git add .vscode README.md
git commit -m "docs: add README and Angular VS Code tooling recommendation"
```
