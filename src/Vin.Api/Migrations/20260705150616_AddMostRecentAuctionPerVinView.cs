using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vin.Api.Migrations
{
    /// <inheritdoc />
    // Unlike every other migration in this project, this one is hand-written
    // raw SQL rather than scaffolded from an entity change — `dotnet ef
    // migrations add` only generated the empty Up()/Down() shell; the view
    // body below was written by hand via migrationBuilder.Sql(...).
    //
    // This view deliberately duplicates, in T-SQL, the exact "most recent
    // auction record per VIN" logic InventoryAggregationService.BuildQuery()
    // already computes in LINQ (which itself compiles to this same
    // ROW_NUMBER()-over-a-partition shape — see that file's comments). It is
    // NOT consumed anywhere by the C# code. That's a conscious choice, not a
    // missed opportunity to have the API select from this view instead: the
    // point is an independent, SSMS-queryable BI/reporting artifact that
    // proves the same result two different ways — LINQ and hand-written SQL
    // — not to DRY the two together.
    public partial class AddMostRecentAuctionPerVinView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The ORDER BY inside this window function (AuctionDate DESC,
            // Id DESC) must stay identical to the tiebreak used in
            // InventoryAggregationService.BuildQuery()'s
            // .OrderByDescending(AuctionDate).ThenByDescending(Id) — if the
            // two ever diverge, the API and this view could disagree on
            // which record is canonical for a VIN with same-timestamp
            // auction records.
            migrationBuilder.Sql(@"
CREATE VIEW dbo.MostRecentAuctionPerVin AS
SELECT
    Id,
    Vin,
    HammerPrice,
    AuctionDate,
    Condition
FROM (
    SELECT
        Id,
        Vin,
        HammerPrice,
        AuctionDate,
        Condition,
        ROW_NUMBER() OVER (
            PARTITION BY Vin
            ORDER BY AuctionDate DESC, Id DESC
        ) AS RowNum
    FROM dbo.AuctionRecords
) AS Ranked
WHERE RowNum = 1;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW dbo.MostRecentAuctionPerVin;");
        }
    }
}
