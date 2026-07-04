# Vin — Multi-Source Inventory Data Aggregation

ASP.NET Core 9 API that merges mock dealer, auction, and sales data by VIN
into a unified inventory view, with an Angular table and a React table —
two independent, interchangeable frontends against the same API, built to
directly compare Angular vs. React idioms.

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

## Running the React app

```bash
cd src/vin-web-react
npm install
npm run dev
```

Open `http://localhost:5173`. Same expectation as the Angular app — the API
must already be running on `http://localhost:5080`. Both frontends can run
at the same time; the API's CORS policy allows both origins.

## Resetting the database

```bash
dotnet ef database drop -p src/Vin.Api -s src/Vin.Api
dotnet ef database update -p src/Vin.Api -s src/Vin.Api
```
