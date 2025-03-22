namespace OpenPrismNode.Sync.Commands.DbSync.GetPostgresBlockByBlockNo;

using Dapper;
using FluentResults;
using MediatR;
using OpenPrismNode.Core.DbSyncModels;
using OpenPrismNode.Sync.Services;

/// <summary>
/// Retrieves a specific block from the Cardano DB Sync PostgreSQL database by its block number.
/// This handler is used when we need to look up detailed information about a block at a specific height.
/// </summary>
public class GetPostgresBlockByBlockNoHandler : IRequestHandler<GetPostgresBlockByBlockNoRequest, Result<Block>>
{
    private readonly INpgsqlConnectionFactory _connectionFactory;

    public GetPostgresBlockByBlockNoHandler(INpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <inheritdoc />
    public async Task<Result<Block>> Handle(GetPostgresBlockByBlockNoRequest request, CancellationToken cancellationToken)
    {
        await using (var connection = _connectionFactory.CreateConnection())
        {
            // SQL Query: Retrieves a specific block by its block number
            // - Selects all block information (id, hash, epoch, block number, time, tx count, etc.)
            // - Joins with the previous block to get its hash (as previousHash)
            // - Filters by the requested block number
            // - Note: Uses parameterized query to prevent SQL injection
            string commandText = @"
                SELECT b.id, b.hash, b.epoch_no, b.block_no, b.time, b.tx_count, b.previous_id, 
                       pb.hash as previousHash
                FROM public.block b
                LEFT JOIN public.block pb ON b.previous_id = pb.id
                WHERE b.block_no = @BlockNo;";
            var block = await connection.QueryFirstOrDefaultAsync<Block>(commandText, new { BlockNo = request.BlockNo });
            if (block is null)
            {
                return Result.Fail("Block could not be found");
            }

            // Ensure time is in UTC format for consistent timestamp handling
            block.time = DateTime.SpecifyKind(block.time, DateTimeKind.Utc);
            return Result.Ok(block);
        }
    }
}