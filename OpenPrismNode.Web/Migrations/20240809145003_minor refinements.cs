using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenPrismNode.Web.Migrations
{
    /// <inheritdoc />
    public partial class minorrefinements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BlockEntity_BlockEntity_PreviousBlockBlockHeight_PreviousBl~",
                table: "BlockEntity");

            migrationBuilder.DropForeignKey(
                name: "FK_BlockEntity_EpochEntity_EpochNumber",
                table: "BlockEntity");

            migrationBuilder.DropForeignKey(
                name: "FK_EpochEntity_PrismNetworkEntities_NetworkType",
                table: "EpochEntity");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EpochEntity",
                table: "EpochEntity");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BlockEntity",
                table: "BlockEntity");

            migrationBuilder.RenameTable(
                name: "EpochEntity",
                newName: "EpochEntities");

            migrationBuilder.RenameTable(
                name: "BlockEntity",
                newName: "BlockEntities");

            migrationBuilder.RenameIndex(
                name: "IX_EpochEntity_NetworkType",
                table: "EpochEntities",
                newName: "IX_EpochEntities_NetworkType");

            migrationBuilder.RenameIndex(
                name: "IX_BlockEntity_PreviousBlockBlockHeight_PreviousBlockBlockHash~",
                table: "BlockEntities",
                newName: "IX_BlockEntities_PreviousBlockBlockHeight_PreviousBlockBlockHa~");

            migrationBuilder.RenameIndex(
                name: "IX_BlockEntity_EpochNumber",
                table: "BlockEntities",
                newName: "IX_BlockEntities_EpochNumber");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EpochEntities",
                table: "EpochEntities",
                column: "EpochNumber");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BlockEntities",
                table: "BlockEntities",
                columns: new[] { "BlockHeight", "BlockHashPrefix" });

            migrationBuilder.AddForeignKey(
                name: "FK_BlockEntities_BlockEntities_PreviousBlockBlockHeight_Previo~",
                table: "BlockEntities",
                columns: new[] { "PreviousBlockBlockHeight", "PreviousBlockBlockHashPrefix" },
                principalTable: "BlockEntities",
                principalColumns: new[] { "BlockHeight", "BlockHashPrefix" });

            migrationBuilder.AddForeignKey(
                name: "FK_BlockEntities_EpochEntities_EpochNumber",
                table: "BlockEntities",
                column: "EpochNumber",
                principalTable: "EpochEntities",
                principalColumn: "EpochNumber",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EpochEntities_PrismNetworkEntities_NetworkType",
                table: "EpochEntities",
                column: "NetworkType",
                principalTable: "PrismNetworkEntities",
                principalColumn: "NetworkType",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BlockEntities_BlockEntities_PreviousBlockBlockHeight_Previo~",
                table: "BlockEntities");

            migrationBuilder.DropForeignKey(
                name: "FK_BlockEntities_EpochEntities_EpochNumber",
                table: "BlockEntities");

            migrationBuilder.DropForeignKey(
                name: "FK_EpochEntities_PrismNetworkEntities_NetworkType",
                table: "EpochEntities");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EpochEntities",
                table: "EpochEntities");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BlockEntities",
                table: "BlockEntities");

            migrationBuilder.RenameTable(
                name: "EpochEntities",
                newName: "EpochEntity");

            migrationBuilder.RenameTable(
                name: "BlockEntities",
                newName: "BlockEntity");

            migrationBuilder.RenameIndex(
                name: "IX_EpochEntities_NetworkType",
                table: "EpochEntity",
                newName: "IX_EpochEntity_NetworkType");

            migrationBuilder.RenameIndex(
                name: "IX_BlockEntities_PreviousBlockBlockHeight_PreviousBlockBlockHa~",
                table: "BlockEntity",
                newName: "IX_BlockEntity_PreviousBlockBlockHeight_PreviousBlockBlockHash~");

            migrationBuilder.RenameIndex(
                name: "IX_BlockEntities_EpochNumber",
                table: "BlockEntity",
                newName: "IX_BlockEntity_EpochNumber");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EpochEntity",
                table: "EpochEntity",
                column: "EpochNumber");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BlockEntity",
                table: "BlockEntity",
                columns: new[] { "BlockHeight", "BlockHashPrefix" });

            migrationBuilder.AddForeignKey(
                name: "FK_BlockEntity_BlockEntity_PreviousBlockBlockHeight_PreviousBl~",
                table: "BlockEntity",
                columns: new[] { "PreviousBlockBlockHeight", "PreviousBlockBlockHashPrefix" },
                principalTable: "BlockEntity",
                principalColumns: new[] { "BlockHeight", "BlockHashPrefix" });

            migrationBuilder.AddForeignKey(
                name: "FK_BlockEntity_EpochEntity_EpochNumber",
                table: "BlockEntity",
                column: "EpochNumber",
                principalTable: "EpochEntity",
                principalColumn: "EpochNumber",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EpochEntity_PrismNetworkEntities_NetworkType",
                table: "EpochEntity",
                column: "NetworkType",
                principalTable: "PrismNetworkEntities",
                principalColumn: "NetworkType",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
