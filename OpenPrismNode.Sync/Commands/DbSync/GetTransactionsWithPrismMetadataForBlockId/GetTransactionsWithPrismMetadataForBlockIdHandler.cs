namespace OpenPrismNode.Sync.Commands.DbSync.GetTransactionsWithPrismMetadataForBlockId;

using Dapper;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Options;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Core.DbSyncModels;
using OpenPrismNode.Sync.Services;

/// <summary>
/// Retrieves all transactions that contain PRISM metadata for a specific block from the Cardano DB Sync PostgreSQL database.
/// This handler is critical for the block processing pipeline as it identifies which transactions in a block contain PRISM operations.
/// </summary>
public class GetTransactionsWithPrismMetadataForBlockIdHandler : IRequestHandler<GetTransactionsWithPrismMetadataForBlockIdRequest, Result<List<Transaction>>>
{
    private readonly INpgsqlConnectionFactory _connectionFactory;
    private readonly AppSettings _appSettings;

    public GetTransactionsWithPrismMetadataForBlockIdHandler(INpgsqlConnectionFactory connectionFactory, IOptions<AppSettings> appSettings)
    {
        _connectionFactory = connectionFactory;
        _appSettings = appSettings.Value;
    }

    /// <inheritdoc />
    public async Task<Result<List<Transaction>>> Handle(GetTransactionsWithPrismMetadataForBlockIdRequest idRequest, CancellationToken cancellationToken)
    {
        await using (var connection = _connectionFactory.CreateConnection())
        {
            // SQL Query: Retrieves transactions in a specific block that contain PRISM metadata
            // - Selects transaction data (id, hash, block_index, fee, size)
            // - Joins with tx_metadata table to find transactions with PRISM metadata
            // - Filters by block ID and the configured PRISM metadata key
            // - Orders by block_index to process transactions in the correct sequence
            string commandText = @"
            SELECT t.id,t.hash, t.block_index, t.fee, t.size
            FROM tx t
            JOIN tx_metadata m ON t.id = m.tx_id
            WHERE t.block_id = @BlockId AND m.key = @PrismMetadataKey
            ORDER BY t.block_index";
            var parameters = new { BlockId = idRequest.BlockId, PrismMetadataKey = _appSettings.MetadataKey };

            var transactions = await connection.QueryAsync<Transaction>(commandText, parameters);
            return transactions.ToList();
        }
    }
}