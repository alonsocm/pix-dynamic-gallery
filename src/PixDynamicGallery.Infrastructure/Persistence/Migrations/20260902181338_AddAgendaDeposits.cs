using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PixDynamicGallery.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAgendaDeposits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgendaDeposits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgendaEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TransferredTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgendaDeposits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgendaDeposits_AgendaEntries_AgendaEntryId",
                        column: x => x.AgendaEntryId,
                        principalTable: "AgendaEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgendaDeposits_AgendaEntryId_PaymentDate",
                table: "AgendaDeposits",
                columns: new[] { "AgendaEntryId", "PaymentDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgendaDeposits");
        }
    }
}
