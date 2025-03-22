namespace OpenPrismNode.Sync.Commands.DbSync.GetPostgresFirstBlockOfEpoch;

using Dapper;
using FluentResults;
using MediatR;
using OpenPrismNode.Core.DbSyncModels;
using OpenPrismNode.Sync.Services;

/// <summary>
/// Retrieves the first block of a specific epoch from the Cardano DB Sync PostgreSQL database.
/// This handler is used during the initial sync process to establish epoch boundaries.
/// </summary>
public class GetPostgresFirstBlockOfEpochHandler : IRequestHandler<GetPostgresFirstBlockOfEpochRequest, Result<Block>>
{
    private readonly INpgsqlConnectionFactory _connectionFactory;

    public GetPostgresFirstBlockOfEpochHandler(INpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<Block>> Handle(GetPostgresFirstBlockOfEpochRequest request, CancellationToken cancellationToken)
    {
        await using (var connection = _connectionFactory.CreateConnection())
        {
            // SQL Query: Retrieves the first block of a specific epoch
            // - Selects all block information
            // - Filters blocks by epoch number
            // - Orders by block_no ascending to get the first block in the epoch
            // - Limits to 1 row to return only the first block
            string commandText = @"
                SELECT b.* 
                FROM public.block b
                WHERE b.epoch_no = @EpochNo
                ORDER BY b.block_no ASC
                LIMIT 1;";

            var block = await connection.QueryFirstOrDefaultAsync<Block>(commandText, new { EpochNo = request.EpochNumber });

            if (block is null)
            {
                return Result.Fail($"No block found for epoch {request.EpochNumber}");
            }

            block.time = DateTime.SpecifyKind(block.time, DateTimeKind.Utc);
            return Result.Ok(block);
        }
    }
}