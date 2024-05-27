namespace OpenPrismNode.Sync.Commands.GetMetadataFromPrismTransactions;

using Core.Models;
using Dapper;
using FluentResults;
// using global::Grpc.Core;
using MediatR;
using PostgresModels;
using Services;

public class GetMetadataOfPrismTransactionHandler : IRequestHandler<GetMetadataOfPrismTransactionRequest, Result<Metadata>>
{
    private readonly INpgsqlConnectionFactory _connectionFactory;

    public GetMetadataOfPrismTransactionHandler(INpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <inheritdoc />
    public async Task<Result<Metadata>> Handle(GetMetadataOfPrismTransactionRequest request, CancellationToken cancellationToken)
    {
        await using (var connection = _connectionFactory.CreateConnection())
        {
            string commandText = $"SELECT * FROM tx_metadata where tx_id = {request.TxId} and key = {request.Key}";
            var metadata = await connection.QueryAsync<Metadata>(commandText);

            if (metadata.Count() == 0 || metadata.Count() > 1)
            {
                return Result.Fail<Metadata>("Metadata not found or multiple entries found");
            }

            return Result.Ok(metadata.Single());
        }
    }
}