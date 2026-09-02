using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PixDynamicGallery.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUsbStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CostPerUsbSnapshot",
                table: "EventTransactions",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsbCount",
                table: "EventTransactions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UsbPurchases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UnitsCount = table.Column<int>(type: "integer", nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsbPurchases", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UsbPurchases_PurchaseDate",
                table: "UsbPurchases",
                column: "PurchaseDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UsbPurchases");

            migrationBuilder.DropColumn(
                name: "CostPerUsbSnapshot",
                table: "EventTransactions");

            migrationBuilder.DropColumn(
                name: "UsbCount",
                table: "EventTransactions");
        }
    }
}
