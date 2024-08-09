using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenPrismNode.Web.Migrations
{
    /// <inheritdoc />
    public partial class initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PrismNetworkEntities",
                columns: table => new
                {
                    NetworkType = table.Column<int>(type: "integer", nullable: false),
                    LastSynced = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrismNetworkEntities", x => x.NetworkType);
                });

            migrationBuilder.CreateTable(
                name: "EpochEntity",
                columns: table => new
                {
                    EpochNumber = table.Column<int>(type: "integer", nullable: false),
                    NetworkType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EpochEntity", x => x.EpochNumber);
                    table.ForeignKey(
                        name: "FK_EpochEntity_PrismNetworkEntities_NetworkType",
                        column: x => x.NetworkType,
                        principalTable: "PrismNetworkEntities",
                        principalColumn: "NetworkType",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BlockEntity",
                columns: table => new
                {
                    BlockHeight = table.Column<int>(type: "integer", nullable: false),
                    BlockHashPrefix = table.Column<int>(type: "integer", nullable: false),
                    BlockHash = table.Column<byte[]>(type: "bytea", nullable: false),
                    TimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TxCount = table.Column<int>(type: "integer", nullable: false),
                    LastParsedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EpochNumber = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlockEntity", x => new { x.BlockHeight, x.BlockHashPrefix });
                    table.ForeignKey(
                        name: "FK_BlockEntity_EpochEntity_EpochNumber",
                        column: x => x.EpochNumber,
                        principalTable: "EpochEntity",
                        principalColumn: "EpochNumber",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlockEntity_EpochNumber",
                table: "BlockEntity",
                column: "EpochNumber");

            migrationBuilder.CreateIndex(
                name: "IX_EpochEntity_NetworkType",
                table: "EpochEntity",
                column: "NetworkType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlockEntity");

            migrationBuilder.DropTable(
                name: "EpochEntity");

            migrationBuilder.DropTable(
                name: "PrismNetworkEntities");
        }
    }
}
