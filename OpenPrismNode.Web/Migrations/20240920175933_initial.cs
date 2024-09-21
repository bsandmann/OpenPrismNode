using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

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
                name: "LedgerEntities",
                columns: table => new
                {
                    Ledger = table.Column<int>(type: "integer", nullable: false),
                    LastSynced = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LedgerEntities", x => x.Ledger);
                });

            migrationBuilder.CreateTable(
                name: "OperationStatusEntities",
                columns: table => new
                {
                    OperationStatusEntityId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OperationStatusId = table.Column<byte[]>(type: "bytea", nullable: false),
                    OperationHash = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    OperationType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationStatusEntities", x => x.OperationStatusEntityId);
                });

            migrationBuilder.CreateTable(
                name: "StakeAddressEntities",
                columns: table => new
                {
                    StakeAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StakeAddressEntities", x => x.StakeAddress);
                });

            migrationBuilder.CreateTable(
                name: "WalletAddressEntities",
                columns: table => new
                {
                    WalletAddress = table.Column<string>(type: "character varying(114)", maxLength: 114, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletAddressEntities", x => x.WalletAddress);
                });

            migrationBuilder.CreateTable(
                name: "WalletEntities",
                columns: table => new
                {
                    WalletEntityId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Mnemonic = table.Column<string>(type: "text", nullable: false),
                    Passphrase = table.Column<string>(type: "text", nullable: false),
                    WalletName = table.Column<string>(type: "text", nullable: false),
                    WalletId = table.Column<string>(type: "text", nullable: false),
                    IsSyncedInitially = table.Column<bool>(type: "boolean", nullable: false),
                    SyncProgress = table.Column<decimal>(type: "numeric", nullable: true),
                    IsInSync = table.Column<bool>(type: "boolean", nullable: true),
                    LastKnownBalance = table.Column<long>(type: "bigint", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSynced = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FundingAddress = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletEntities", x => x.WalletEntityId);
                });

            migrationBuilder.CreateTable(
                name: "EpochEntities",
                columns: table => new
                {
                    EpochNumber = table.Column<int>(type: "integer", nullable: false),
                    Ledger = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EpochEntities", x => new { x.EpochNumber, x.Ledger });
                    table.ForeignKey(
                        name: "FK_EpochEntities_LedgerEntities_Ledger",
                        column: x => x.Ledger,
                        principalTable: "LedgerEntities",
                        principalColumn: "Ledger",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WalletTransactionEntities",
                columns: table => new
                {
                    WalletTransactionEntityId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TransactionId = table.Column<string>(type: "text", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Depth = table.Column<long>(type: "bigint", nullable: false),
                    Fee = table.Column<long>(type: "bigint", nullable: false),
                    WalletEntityId = table.Column<int>(type: "integer", nullable: false),
                    OperationStatusEntityId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletTransactionEntities", x => x.WalletTransactionEntityId);
                    table.ForeignKey(
                        name: "FK_WalletTransactionEntities_OperationStatusEntities_Operation~",
                        column: x => x.OperationStatusEntityId,
                        principalTable: "OperationStatusEntities",
                        principalColumn: "OperationStatusEntityId");
                    table.ForeignKey(
                        name: "FK_WalletTransactionEntities_WalletEntities_WalletEntityId",
                        column: x => x.WalletEntityId,
                        principalTable: "WalletEntities",
                        principalColumn: "WalletEntityId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BlockEntities",
                columns: table => new
                {
                    BlockHeight = table.Column<int>(type: "integer", nullable: false),
                    BlockHashPrefix = table.Column<int>(type: "integer", nullable: false),
                    BlockHash = table.Column<byte[]>(type: "bytea", nullable: false),
                    TimeUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TxCount = table.Column<short>(type: "smallint", nullable: false),
                    LastParsedOnUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    EpochNumber = table.Column<short>(type: "smallint", nullable: false),
                    Ledger = table.Column<int>(type: "integer", nullable: false),
                    IsFork = table.Column<bool>(type: "boolean", nullable: false),
                    PreviousBlockHeight = table.Column<int>(type: "integer", nullable: true),
                    PreviousBlockHashPrefix = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlockEntities", x => new { x.BlockHeight, x.BlockHashPrefix });
                    table.ForeignKey(
                        name: "FK_BlockEntities_BlockEntities_PreviousBlockHeight_PreviousBlo~",
                        columns: x => new { x.PreviousBlockHeight, x.PreviousBlockHashPrefix },
                        principalTable: "BlockEntities",
                        principalColumns: new[] { "BlockHeight", "BlockHashPrefix" });
                    table.ForeignKey(
                        name: "FK_BlockEntities_EpochEntities_EpochNumber_Ledger",
                        columns: x => new { x.EpochNumber, x.Ledger },
                        principalTable: "EpochEntities",
                        principalColumns: new[] { "EpochNumber", "Ledger" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransactionEntities",
                columns: table => new
                {
                    TransactionHash = table.Column<byte[]>(type: "bytea", nullable: false),
                    BlockHeight = table.Column<int>(type: "integer", nullable: false),
                    BlockHashPrefix = table.Column<int>(type: "integer", nullable: false),
                    Fees = table.Column<int>(type: "integer", nullable: false),
                    Size = table.Column<short>(type: "smallint", nullable: false),
                    Index = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionEntities", x => new { x.TransactionHash, x.BlockHeight, x.BlockHashPrefix });
                    table.ForeignKey(
                        name: "FK_TransactionEntities_BlockEntities_BlockHeight_BlockHashPref~",
                        columns: x => new { x.BlockHeight, x.BlockHashPrefix },
                        principalTable: "BlockEntities",
                        principalColumns: new[] { "BlockHeight", "BlockHashPrefix" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CreateDidEntities",
                columns: table => new
                {
                    OperationHash = table.Column<byte[]>(type: "bytea", nullable: false),
                    SigningKeyId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OperationSequenceNumber = table.Column<int>(type: "integer", nullable: false),
                    TransactionHash = table.Column<byte[]>(type: "bytea", nullable: false),
                    BlockHeight = table.Column<int>(type: "integer", nullable: false),
                    BlockHashPrefix = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreateDidEntities", x => x.OperationHash);
                    table.ForeignKey(
                        name: "FK_CreateDidEntities_TransactionEntities_TransactionHash_Block~",
                        columns: x => new { x.TransactionHash, x.BlockHeight, x.BlockHashPrefix },
                        principalTable: "TransactionEntities",
                        principalColumns: new[] { "TransactionHash", "BlockHeight", "BlockHashPrefix" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UtxoEntities",
                columns: table => new
                {
                    UtxoEntityId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Index = table.Column<short>(type: "smallint", nullable: false),
                    IsOutgoing = table.Column<bool>(type: "boolean", nullable: false),
                    Value = table.Column<long>(type: "bigint", nullable: false),
                    TransactionHash = table.Column<byte[]>(type: "bytea", nullable: false),
                    BlockHeight = table.Column<int>(type: "integer", nullable: false),
                    BlockHashPrefix = table.Column<int>(type: "integer", nullable: false),
                    StakeAddress = table.Column<string>(type: "character varying(64)", nullable: true),
                    WalletAddress = table.Column<string>(type: "character varying(114)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UtxoEntities", x => x.UtxoEntityId);
                    table.ForeignKey(
                        name: "FK_UtxoEntities_StakeAddressEntities_StakeAddress",
                        column: x => x.StakeAddress,
                        principalTable: "StakeAddressEntities",
                        principalColumn: "StakeAddress",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UtxoEntities_TransactionEntities_TransactionHash_BlockHeigh~",
                        columns: x => new { x.TransactionHash, x.BlockHeight, x.BlockHashPrefix },
                        principalTable: "TransactionEntities",
                        principalColumns: new[] { "TransactionHash", "BlockHeight", "BlockHashPrefix" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UtxoEntities_WalletAddressEntities_WalletAddress",
                        column: x => x.WalletAddress,
                        principalTable: "WalletAddressEntities",
                        principalColumn: "WalletAddress",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DeactivateDidEntities",
                columns: table => new
                {
                    OperationHash = table.Column<byte[]>(type: "bytea", nullable: false),
                    PreviousOperationHash = table.Column<byte[]>(type: "bytea", nullable: false),
                    SigningKeyId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Did = table.Column<byte[]>(type: "bytea", nullable: false),
                    OperationSequenceNumber = table.Column<int>(type: "integer", nullable: false),
                    TransactionHash = table.Column<byte[]>(type: "bytea", nullable: false),
                    BlockHeight = table.Column<int>(type: "integer", nullable: false),
                    BlockHashPrefix = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeactivateDidEntities", x => x.OperationHash);
                    table.ForeignKey(
                        name: "FK_DeactivateDidEntities_CreateDidEntities_Did",
                        column: x => x.Did,
                        principalTable: "CreateDidEntities",
                        principalColumn: "OperationHash",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeactivateDidEntities_TransactionEntities_TransactionHash_B~",
                        columns: x => new { x.TransactionHash, x.BlockHeight, x.BlockHashPrefix },
                        principalTable: "TransactionEntities",
                        principalColumns: new[] { "TransactionHash", "BlockHeight", "BlockHashPrefix" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UpdateDidEntities",
                columns: table => new
                {
                    OperationHash = table.Column<byte[]>(type: "bytea", nullable: false),
                    PreviousOperationHash = table.Column<byte[]>(type: "bytea", nullable: false),
                    SigningKeyId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Did = table.Column<byte[]>(type: "bytea", nullable: false),
                    OperationSequenceNumber = table.Column<int>(type: "integer", nullable: false),
                    TransactionHash = table.Column<byte[]>(type: "bytea", nullable: false),
                    BlockHeight = table.Column<int>(type: "integer", nullable: false),
                    BlockHashPrefix = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpdateDidEntities", x => x.OperationHash);
                    table.ForeignKey(
                        name: "FK_UpdateDidEntities_CreateDidEntities_Did",
                        column: x => x.Did,
                        principalTable: "CreateDidEntities",
                        principalColumn: "OperationHash",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UpdateDidEntities_TransactionEntities_TransactionHash_Block~",
                        columns: x => new { x.TransactionHash, x.BlockHeight, x.BlockHashPrefix },
                        principalTable: "TransactionEntities",
                        principalColumns: new[] { "TransactionHash", "BlockHeight", "BlockHashPrefix" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatchedContextEntity",
                columns: table => new
                {
                    PatchedContextEntityId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContextListJson = table.Column<string>(type: "jsonb", nullable: false),
                    UpdateOperationOrder = table.Column<short>(type: "smallint", nullable: true),
                    UpdateDidEntityOperationHash = table.Column<byte[]>(type: "bytea", nullable: true),
                    CreateDidEntityOperationHash = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatchedContextEntity", x => x.PatchedContextEntityId);
                    table.ForeignKey(
                        name: "FK_PatchedContextEntity_CreateDidEntities_CreateDidEntityOpera~",
                        column: x => x.CreateDidEntityOperationHash,
                        principalTable: "CreateDidEntities",
                        principalColumn: "OperationHash",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatchedContextEntity_UpdateDidEntities_UpdateDidEntityOpera~",
                        column: x => x.UpdateDidEntityOperationHash,
                        principalTable: "UpdateDidEntities",
                        principalColumn: "OperationHash",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PrismPublicKeyEntities",
                columns: table => new
                {
                    PrismPublicKeyEntityId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PublicKey = table.Column<byte[]>(type: "bytea", nullable: false),
                    KeyId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PrismKeyUsage = table.Column<int>(type: "integer", nullable: false),
                    Curve = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    UpdateOperationOrder = table.Column<short>(type: "smallint", nullable: true),
                    CreateDidEntityOperationHash = table.Column<byte[]>(type: "bytea", nullable: true),
                    UpdateDidEntityOperationHash = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrismPublicKeyEntities", x => x.PrismPublicKeyEntityId);
                    table.ForeignKey(
                        name: "FK_PrismPublicKeyEntities_CreateDidEntities_CreateDidEntityOpe~",
                        column: x => x.CreateDidEntityOperationHash,
                        principalTable: "CreateDidEntities",
                        principalColumn: "OperationHash",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PrismPublicKeyEntities_UpdateDidEntities_UpdateDidEntityOpe~",
                        column: x => x.UpdateDidEntityOperationHash,
                        principalTable: "UpdateDidEntities",
                        principalColumn: "OperationHash",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PrismPublicKeyRemoveEntity",
                columns: table => new
                {
                    PrismPublicKeyRemoveEntityId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KeyId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UpdateOperationOrder = table.Column<short>(type: "smallint", nullable: false),
                    UpdateDidEntityOperationHash = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrismPublicKeyRemoveEntity", x => x.PrismPublicKeyRemoveEntityId);
                    table.ForeignKey(
                        name: "FK_PrismPublicKeyRemoveEntity_UpdateDidEntities_UpdateDidEntit~",
                        column: x => x.UpdateDidEntityOperationHash,
                        principalTable: "UpdateDidEntities",
                        principalColumn: "OperationHash",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PrismServiceEntities",
                columns: table => new
                {
                    PrismServiceEntityId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServiceId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UriString = table.Column<string>(type: "text", nullable: true),
                    JsonData = table.Column<string>(type: "jsonb", nullable: true),
                    ListOfUrisJson = table.Column<string>(type: "jsonb", nullable: true),
                    Removed = table.Column<bool>(type: "boolean", nullable: false),
                    Updated = table.Column<bool>(type: "boolean", nullable: false),
                    UpdateOperationOrder = table.Column<short>(type: "smallint", nullable: true),
                    UpdateDidEntityOperationHash = table.Column<byte[]>(type: "bytea", nullable: true),
                    CreateDidEntityOperationHash = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrismServiceEntities", x => x.PrismServiceEntityId);
                    table.ForeignKey(
                        name: "FK_PrismServiceEntities_CreateDidEntities_CreateDidEntityOpera~",
                        column: x => x.CreateDidEntityOperationHash,
                        principalTable: "CreateDidEntities",
                        principalColumn: "OperationHash",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PrismServiceEntities_UpdateDidEntities_UpdateDidEntityOpera~",
                        column: x => x.UpdateDidEntityOperationHash,
                        principalTable: "UpdateDidEntities",
                        principalColumn: "OperationHash",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlockEntities_EpochNumber",
                table: "BlockEntities",
                column: "EpochNumber");

            migrationBuilder.CreateIndex(
                name: "IX_BlockEntities_EpochNumber_Ledger",
                table: "BlockEntities",
                columns: new[] { "EpochNumber", "Ledger" });

            migrationBuilder.CreateIndex(
                name: "IX_BlockEntities_PreviousBlockHeight_PreviousBlockHashPrefix",
                table: "BlockEntities",
                columns: new[] { "PreviousBlockHeight", "PreviousBlockHashPrefix" });

            migrationBuilder.CreateIndex(
                name: "IX_CreateDidEntities_TransactionHash_BlockHeight_BlockHashPref~",
                table: "CreateDidEntities",
                columns: new[] { "TransactionHash", "BlockHeight", "BlockHashPrefix" });

            migrationBuilder.CreateIndex(
                name: "IX_DeactivateDidEntities_Did",
                table: "DeactivateDidEntities",
                column: "Did",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeactivateDidEntities_TransactionHash_BlockHeight_BlockHash~",
                table: "DeactivateDidEntities",
                columns: new[] { "TransactionHash", "BlockHeight", "BlockHashPrefix" });

            migrationBuilder.CreateIndex(
                name: "IX_EpochEntities_Ledger",
                table: "EpochEntities",
                column: "Ledger");

            migrationBuilder.CreateIndex(
                name: "IX_PatchedContextEntity_ContextListJson",
                table: "PatchedContextEntity",
                column: "ContextListJson")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_PatchedContextEntity_CreateDidEntityOperationHash",
                table: "PatchedContextEntity",
                column: "CreateDidEntityOperationHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatchedContextEntity_UpdateDidEntityOperationHash",
                table: "PatchedContextEntity",
                column: "UpdateDidEntityOperationHash");

            migrationBuilder.CreateIndex(
                name: "IX_PrismPublicKeyEntities_CreateDidEntityOperationHash",
                table: "PrismPublicKeyEntities",
                column: "CreateDidEntityOperationHash");

            migrationBuilder.CreateIndex(
                name: "IX_PrismPublicKeyEntities_UpdateDidEntityOperationHash",
                table: "PrismPublicKeyEntities",
                column: "UpdateDidEntityOperationHash");

            migrationBuilder.CreateIndex(
                name: "IX_PrismPublicKeyRemoveEntity_UpdateDidEntityOperationHash",
                table: "PrismPublicKeyRemoveEntity",
                column: "UpdateDidEntityOperationHash");

            migrationBuilder.CreateIndex(
                name: "IX_PrismServiceEntities_CreateDidEntityOperationHash",
                table: "PrismServiceEntities",
                column: "CreateDidEntityOperationHash");

            migrationBuilder.CreateIndex(
                name: "IX_PrismServiceEntities_UpdateDidEntityOperationHash",
                table: "PrismServiceEntities",
                column: "UpdateDidEntityOperationHash");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionEntities_BlockHeight_BlockHashPrefix",
                table: "TransactionEntities",
                columns: new[] { "BlockHeight", "BlockHashPrefix" });

            migrationBuilder.CreateIndex(
                name: "IX_TransactionEntities_TransactionHash",
                table: "TransactionEntities",
                column: "TransactionHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UpdateDidEntities_Did",
                table: "UpdateDidEntities",
                column: "Did");

            migrationBuilder.CreateIndex(
                name: "IX_UpdateDidEntities_TransactionHash_BlockHeight_BlockHashPref~",
                table: "UpdateDidEntities",
                columns: new[] { "TransactionHash", "BlockHeight", "BlockHashPrefix" });

            migrationBuilder.CreateIndex(
                name: "IX_UtxoEntities_StakeAddress",
                table: "UtxoEntities",
                column: "StakeAddress");

            migrationBuilder.CreateIndex(
                name: "IX_UtxoEntities_TransactionHash_BlockHeight_BlockHashPrefix_In~",
                table: "UtxoEntities",
                columns: new[] { "TransactionHash", "BlockHeight", "BlockHashPrefix", "Index", "IsOutgoing", "Value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UtxoEntities_WalletAddress",
                table: "UtxoEntities",
                column: "WalletAddress");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactionEntities_OperationStatusEntityId",
                table: "WalletTransactionEntities",
                column: "OperationStatusEntityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactionEntities_WalletEntityId",
                table: "WalletTransactionEntities",
                column: "WalletEntityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeactivateDidEntities");

            migrationBuilder.DropTable(
                name: "PatchedContextEntity");

            migrationBuilder.DropTable(
                name: "PrismPublicKeyEntities");

            migrationBuilder.DropTable(
                name: "PrismPublicKeyRemoveEntity");

            migrationBuilder.DropTable(
                name: "PrismServiceEntities");

            migrationBuilder.DropTable(
                name: "UtxoEntities");

            migrationBuilder.DropTable(
                name: "WalletTransactionEntities");

            migrationBuilder.DropTable(
                name: "UpdateDidEntities");

            migrationBuilder.DropTable(
                name: "StakeAddressEntities");

            migrationBuilder.DropTable(
                name: "WalletAddressEntities");

            migrationBuilder.DropTable(
                name: "OperationStatusEntities");

            migrationBuilder.DropTable(
                name: "WalletEntities");

            migrationBuilder.DropTable(
                name: "CreateDidEntities");

            migrationBuilder.DropTable(
                name: "TransactionEntities");

            migrationBuilder.DropTable(
                name: "BlockEntities");

            migrationBuilder.DropTable(
                name: "EpochEntities");

            migrationBuilder.DropTable(
                name: "LedgerEntities");
        }
    }
}
