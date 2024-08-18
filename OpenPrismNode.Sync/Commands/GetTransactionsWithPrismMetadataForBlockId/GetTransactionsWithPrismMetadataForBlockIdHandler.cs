namespace OpenPrismNode.Sync.Commands.GetTransactionsWithPrismMetadataForBlockId;

using Core.DbSyncModels;
using Dapper;
using FluentResults;
using MediatR;
using OpenPrismNode.Core;
using OpenPrismNode.Sync.Services;

public class GetTransactionsWithPrismMetadataForBlockIdHandler : IRequestHandler<GetTransactionsWithPrismMetadataForBlockIdRequest, Result<List<Transaction>>>
{
    private readonly INpgsqlConnectionFactory _connectionFactory;

    public GetTransactionsWithPrismMetadataForBlockIdHandler(INpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <inheritdoc />
    public async Task<Result<List<Transaction>>> Handle(GetTransactionsWithPrismMetadataForBlockIdRequest idRequest, CancellationToken cancellationToken)
    {
        await using (var connection = _connectionFactory.CreateConnection())
        {
            string commandText = @"
            SELECT t.id,t.hash, t.block_index, t.fee, t.size
            FROM tx t
            JOIN tx_metadata m ON t.id = m.tx_id
            WHERE t.block_id = @BlockId AND m.key = @PrismMetadataKey
            ORDER BY t.block_index";
            var parameters = new { BlockId = idRequest.BlockId, PrismMetadataKey = PrismParameters.MetadataKey };

            var transactions = await connection.QueryAsync<Transaction>(commandText, parameters);
            return transactions.ToList();
        }
    }
}