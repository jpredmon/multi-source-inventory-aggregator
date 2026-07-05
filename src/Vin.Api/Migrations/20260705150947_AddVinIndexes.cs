using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vin.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVinIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Vin",
                table: "SaleRecords",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Vin",
                table: "DealerInventory",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Vin",
                table: "AuctionRecords",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_SaleRecords_Vin",
                table: "SaleRecords",
                column: "Vin");

            migrationBuilder.CreateIndex(
                name: "IX_DealerInventory_Vin",
                table: "DealerInventory",
                column: "Vin");

            migrationBuilder.CreateIndex(
                name: "IX_AuctionRecords_Vin_AuctionDate",
                table: "AuctionRecords",
                columns: new[] { "Vin", "AuctionDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SaleRecords_Vin",
                table: "SaleRecords");

            migrationBuilder.DropIndex(
                name: "IX_DealerInventory_Vin",
                table: "DealerInventory");

            migrationBuilder.DropIndex(
                name: "IX_AuctionRecords_Vin_AuctionDate",
                table: "AuctionRecords");

            migrationBuilder.AlterColumn<string>(
                name: "Vin",
                table: "SaleRecords",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Vin",
                table: "DealerInventory",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Vin",
                table: "AuctionRecords",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
