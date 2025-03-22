namespace OpenPrismNode.Sync.Commands.DbSync.GetMetadataFromTransaction;

using Dapper;
using FluentResults;
using MediatR;
using OpenPrismNode.Core.DbSyncModels;
using OpenPrismNode.Sync.Services;

/// <summary>
/// Retrieves metadata associated with a specific transaction from the Cardano DB Sync PostgreSQL database.
/// This handler is used to extract PRISM-specific metadata stored in Cardano transactions.
/// </summary>
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
        
        // SQL Query: Retrieves transaction metadata with a specific key
        // - Selects the JSON content from transaction metadata
        // - Filters by both transaction ID and metadata key (typically the PRISM metadata key)
        // - Each transaction can have multiple metadata entries with different keys
        const string commandText = "SELECT json FROM tx_metadata WHERE tx_id = @TxId AND key = @Key";
        var parameters = new { request.TxId, request.Key };

        var metadata = await connection.QuerySingleOrDefaultAsync<Metadata>(commandText, parameters);

        return metadata != null
            ? Result.Ok(metadata)
            : Result.Fail<Metadata>("Metadata not found");
    }
}