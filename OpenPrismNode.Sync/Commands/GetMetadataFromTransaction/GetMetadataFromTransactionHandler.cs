using Dapper;
using FluentResults;
using MediatR;
using OpenPrismNode.Sync.Services;

namespace OpenPrismNode.Sync.Commands.GetMetadataFromTransaction;

using Core.DbSyncModels;

public class GetMetadataFromTransactionHandler : IRequestHandler<GetMetadataFromTransactionRequest, Result<Metadata>>
{
    private readonly INpgsqlConnectionFactory _connectionFactory;

    public GetMetadataFromTransactionHandler(INpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<Metadata>> Handle(GetMetadataFromTransactionRequest request, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        const string commandText = "SELECT json FROM tx_metadata WHERE tx_id = @TxId AND key = @Key";
        var parameters = new { request.TxId, request.Key };

        var metadata = await connection.QuerySingleOrDefaultAsync<Metadata>(commandText, parameters);

        return metadata != null
            ? Result.Ok(metadata)
            : Result.Fail<Metadata>("Metadata not found");
    }
}