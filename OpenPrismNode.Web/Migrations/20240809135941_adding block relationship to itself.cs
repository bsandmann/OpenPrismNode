using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenPrismNode.Web.Migrations
{
    /// <inheritdoc />
    public partial class addingblockrelationshiptoitself : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PreviousBlockBlockHashPrefix",
                table: "BlockEntity",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PreviousBlockBlockHeight",
                table: "BlockEntity",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PreviousBlockHashPrefix",
                table: "BlockEntity",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PreviousBlockHeight",
                table: "BlockEntity",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlockEntity_PreviousBlockBlockHeight_PreviousBlockBlockHash~",
                table: "BlockEntity",
                columns: new[] { "PreviousBlockBlockHeight", "PreviousBlockBlockHashPrefix" });

            migrationBuilder.AddForeignKey(
                name: "FK_BlockEntity_BlockEntity_PreviousBlockBlockHeight_PreviousBl~",
                table: "BlockEntity",
                columns: new[] { "PreviousBlockBlockHeight", "PreviousBlockBlockHashPrefix" },
                principalTable: "BlockEntity",
                principalColumns: new[] { "BlockHeight", "BlockHashPrefix" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BlockEntity_BlockEntity_PreviousBlockBlockHeight_PreviousBl~",
                table: "BlockEntity");

            migrationBuilder.DropIndex(
                name: "IX_BlockEntity_PreviousBlockBlockHeight_PreviousBlockBlockHash~",
                table: "BlockEntity");

            migrationBuilder.DropColumn(
                name: "PreviousBlockBlockHashPrefix",
                table: "BlockEntity");

            migrationBuilder.DropColumn(
                name: "PreviousBlockBlockHeight",
                table: "BlockEntity");

            migrationBuilder.DropColumn(
                name: "PreviousBlockHashPrefix",
                table: "BlockEntity");

            migrationBuilder.DropColumn(
                name: "PreviousBlockHeight",
                table: "BlockEntity");
        }
    }
}
