# Multi-Source Inventory Data Aggregation — Design

## Purpose

A backend API (ASP.NET Core + C#) that ingests mock inventory data from three
differently-shaped sources and normalizes them into a unified dashboard view,
merged by VIN. A minimal Angular frontend displays the result.

**Primary goal:** rebuild hands-on confidence in .NET/C#/Angular by writing
real queries, real API endpoints, and real data transformations, and hitting
(and solving) real friction — schema mismatches, timestamp/timezone handling,
null handling.

**Secondary goal:** produce concrete interview talking points — e.g. merging
three conflicting schemas by VIN without N+1 queries, normalizing
timestamps across systems that disagree on timezone — backed by working
code rather than described in the abstract.

**Time budget:** ~2.5 days. Scope is intentionally contained. The project
does not need to be finished — a working core is enough to talk about.

**Stack decision:** built on current .NET 9 + EF Core + a current Angular
version, not the legacy .NET Framework 4.8 + EF6 stack Vin (the company)
may actually run. This was a deliberate tradeoff: the 2.5-day budget favors
spending time on the aggregation/API logic (which transfers conceptually to
.NET Framework in conversation) over legacy tooling friction (IIS Express,
Web.config, EF6 quirks, weaker CLI scaffolding). Revisit this decision if a
tech screen or job posting confirms .NET Framework 4.8 is actually in scope.

## Solution Structure

```
vin/
├── src/
│   ├── Vin.Api/              # ASP.NET Core 9 Web API
│   │   ├── Data/                # DbContext, entity configs
│   │   ├── Models/              # Entities: DealerInventory, AuctionRecord, SaleRecord
│   │   ├── Dtos/                # VehicleSummaryDto (unified response shape)
│   │   ├── Services/            # InventoryAggregationService (query-time merge)
│   │   ├── Controllers/         # InventoryController
│   │   └── Seed/                # JSON seed files + startup seeding logic
│   └── vin-web/              # Angular app (current CLI, local devDependency)
├── vin.sln
└── README.md
```

- .NET 9 SDK for the API.
- SQL Server LocalDB for persistence, EF Core 9 as the ORM.
- Angular scaffolded locally into `src/vin-web` using a current Angular
  CLI installed via `npx`/local devDependency — not the stale global v9.0.5
  CLI already on this machine, and not a change to that global install.
- Two independently runnable apps: API (Kestrel) and Angular dev server,
  with CORS enabling the Angular dev server to call the API.

## Data Model & Seed Data

Three EF Core entities, each mirroring its source's native shape exactly —
no normalization happens at write time:

```csharp
// DealerInventory — anchor table, every vehicle starts here
Vin, StockNumber, Cost, DateAcquired

// AuctionRecord — 0 or 1 per VIN, optional
Vin, HammerPrice, AuctionDate, Condition

// SaleRecord — 0 or 1 per VIN, optional
Vin, SalePrice, DaysOnLot, SoldDate
```

- `Vin` is the shared join key across all three tables. It is not enforced
  as a foreign key — the sources are independent systems in real life, and
  VIN is just a natural key they happen to share, matching how disconnected
  systems actually behave.
- Three JSON seed files live under `Seed/`:
  `dealer-inventory.json`, `auction-feed.json`, `sales-history.json`.
- On startup, `Database.Migrate()` runs, then a seeding check loads the JSON
  files into LocalDB if the tables are empty (idempotent — safe to restart
  the app without duplicating data).
- Seed data deliberately includes messy-data cases:
  - A VIN in dealer inventory with no auction or sale record (still on lot).
  - A VIN with an auction record but no sale record (bought at auction, not
    yet resold).
  - At least one timestamp inconsistency across sources (format or
    timezone) to exercise real normalization logic.
- ~15–20 vehicles total. This project is about the query/API pattern, not
  data volume.

## API Design

Primary endpoint:

```
GET /api/inventory
→ VehicleSummaryDto[]
```

```csharp
VehicleSummaryDto {
  Vin, StockNumber, Cost, DateAcquired,      // always present (dealer is anchor)
  HammerPrice?, AuctionDate?, Condition?,     // null if never auctioned
  SalePrice?, DaysOnLot?, SoldDate?,          // null if not sold
  Status                                      // computed: OnLot | Auctioned | Sold
}
```

Secondary endpoint:

```
GET /api/inventory/{vin}
→ VehicleSummaryDto, or 404 if the VIN doesn't exist in DealerInventory
```

**Merge strategy:** left join from `DealerInventory`, since dealer inventory
is the anchor — every vehicle is on the lot before it's ever auctioned or
sold. `AuctionRecord` and `SaleRecord` are optional overlays; a vehicle
with no match in either simply has null fields and a status of `OnLot`.

**Avoiding N+1 (the core talking point):** `InventoryAggregationService`
builds the merge as a single LINQ query using a `GroupJoin`/`SelectMany`
left-join pattern across the three `DbSet`s, projecting directly into
`VehicleSummaryDto` inside the query. EF Core translates this into one SQL
query with two LEFT JOINs, rather than fetching all dealer records and then
querying auction/sale data per VIN in a loop (1 query vs. 1 + N + N).

**Scope boundaries:** read-only. No auth, no write/update endpoints.
CORS is enabled for the Angular dev server's origin (`localhost:4200` by
default).

## Angular Dashboard

Minimal single feature area, no routing:

```
vin-web/src/app/
├── inventory/
│   ├── inventory.service.ts     # HttpClient call to GET /api/inventory
│   ├── inventory.model.ts       # TS interface mirroring VehicleSummaryDto
│   └── inventory-table/
│       ├── inventory-table.component.ts
│       └── inventory-table.component.html
└── app.component.ts             # hosts <app-inventory-table>
```

- On load, `InventoryTableComponent` calls the service once and renders a
  single table: VIN, Stock #, Cost, Date Acquired, Hammer Price, Auction
  Date, Condition, Sale Price, Days on Lot, Sold Date, Status.
- Null fields render as `—`, visually surfacing the "not every VIN has
  every source" story.
- No sorting, filtering, or pagination. No state management library.
- Basic CSS only, for readability — this is not a design exercise.

## Out of Scope

- Authentication/authorization.
- Write or update endpoints (read-only aggregation is the entire scope).
- Pagination, real-time updates, sorting/filtering in the UI.
- Automated tests — given the 2.5-day budget, working code takes priority
  over test coverage.
- Docker and CI — local dev only.
- Upgrading the machine's global Angular CLI — the project uses a local,
  per-project CLI install instead.

## Testing

Not included in this pass (see Out of Scope). If time remains after the
core aggregation and dashboard work, a small set of unit tests around
`InventoryAggregationService`'s merge logic (null-handling cases, status
computation) would be the highest-value addition — not full endpoint or
UI test coverage.
