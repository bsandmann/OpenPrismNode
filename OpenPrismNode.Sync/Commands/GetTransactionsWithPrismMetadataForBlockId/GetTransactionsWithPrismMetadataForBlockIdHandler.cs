namespace OpenPrismNode.Sync.Commands.GetTransactionsWithPrismMetadataForBlockId;

using Core.Common;
using Core.DbSyncModels;
using Dapper;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Options;
using OpenPrismNode.Core;
using OpenPrismNode.Sync.Services;

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