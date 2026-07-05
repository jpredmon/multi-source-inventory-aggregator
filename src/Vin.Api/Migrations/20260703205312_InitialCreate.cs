using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vin.Api.Migrations
{
    /// <inheritdoc />
    ///  // "Migration" base class + the timestamped class name (20260703205312_InitialCreate)
    // is how EF Core orders migrations and knows which ones a given database has
    // already applied — that timestamp prefix is literally a sortable "when was
    // this written" key, recorded per-database in the __EFMigrationsHistory table
    // you saw rows for in the SQL logging demo earlier.
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        ///  // Up() is what runs when you go FORWARD — `dotnet ef database update`,
        // or (in this app) db.Database.Migrate() in Program.cs at startup.
        // This method is generated code — you (or whoever ran the plan) never
        // hand-wrote CREATE TABLE statements; `dotnet ef migrations add InitialCreate`
        // read the three entity classes (Models/*.cs) via reflection and produced
        // this file automatically, translating C# properties into column
        // definitions.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuctionRecords",
                columns: table => new
                {
                      // This one line is where int Id { get; set; } (a plain
                    // property in AuctionRecord.cs) became an auto-incrementing
                    // primary key column — the "SqlServer:Identity", "1, 1"
                    // annotation means "start at 1, increment by 1," and is
                    // exactly what EF's convention (a property literally named
                    // "Id") triggered automatically, with zero explicit config
                    // anywhere in AuctionRecord.cs.
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                          // nvarchar(max), nullable: false — this is the "no explicit
                    // length" default from the entity walkthrough, made concrete:
                    // a plain `string Vin { get; set; } = string.Empty;` becomes
                    // an unbounded, NOT NULL text column. If you'd wanted
                    // nvarchar(17) instead (VINs are always 17 characters), that
                    // would need a [MaxLength(17)] attribute on the entity property,
                    // which this project doesn't have — this file is proof that
                    // absence has a real, visible schema consequence.
                    Vin = table.Column<string>(type: "nvarchar(max)", nullable: false),
                      // decimal(18,2) — the EF Core default precision/scale
                    // mentioned back in the entity walkthrough, now visible as an
                    // actual column type rather than a hypothetical.
                    HammerPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                      // datetime2 — SQL Server's modern date/time type, with no
                    // timezone/offset component. This is the migration-level
                    // proof of the timestamp-truncation point from the entity
                    // walkthrough: whatever offset a DateTime carried in C#
                    // (or didn't — plain DateTime rarely does), this column type
                    // has no way to store one.
                    AuctionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Condition = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuctionRecords", x => x.Id);
                });

                 // Same shape, repeated for DealerInventory and SaleRecords — one
            // CreateTable call per entity, generated from AuctionRecord.cs,
            // DealerInventory.cs, and SaleRecord.cs respectively. Notice: no
            // foreign keys, no AddForeignKey calls anywhere in this file — this
            // is the migration-level confirmation of what the design spec said
            // in words: Vin is a natural key the three tables share, never an
            // enforced relational constraint.

            migrationBuilder.CreateTable(
                name: "DealerInventory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Vin = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StockNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DateAcquired = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DealerInventory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SaleRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Vin = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SalePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DaysOnLot = table.Column<int>(type: "int", nullable: false),
                    SoldDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleRecords", x => x.Id);
                });
        }

        /// <inheritdoc />
        ///  // Down() is the undo — what runs on `dotnet ef database update <previous>`
        // or a migration rollback. EF generates this as the mechanical inverse of
        // Up(): three CreateTables going forward, three DropTables going back.
        // Nobody wrote this by hand either.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuctionRecords");

            migrationBuilder.DropTable(
                name: "DealerInventory");

            migrationBuilder.DropTable(
                name: "SaleRecords");
        }
    }
}
