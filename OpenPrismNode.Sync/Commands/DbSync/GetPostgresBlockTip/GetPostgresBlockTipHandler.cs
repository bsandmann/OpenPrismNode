namespace OpenPrismNode.Sync.Commands.DbSync.GetPostgresBlockTip;

using System.Threading;
using System.Threading.Tasks;
using Dapper;
using FluentResults;
using MediatR;
using OpenPrismNode.Core.DbSyncModels;
using OpenPrismNode.Sync.Services;

/// <summary>
/// Retrieves the tip (most recent block) from the Cardano DB Sync PostgreSQL database.
/// This handler is responsible for finding the latest block in the blockchain to determine
/// how far the syncing process needs to go.
/// </summary>
public class GetPostgresBlockTipHandler : IRequestHandler<GetPostgresBlockTipRequest, Result<Block>>
{
    private readonly INpgsqlConnectionFactory _connectionFactory;

    public GetPostgresBlockTipHandler(INpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }


    /// <summary>
    /// Get the most recent block from the database
    /// Note that in the dbSync-database a block_no is indeed unique (in case it is not null).
    /// But there are some blocks with block_no = null. These might be the result of forks a
    /// specific to the behaivor of the local node. Still unclear.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<Result<Block>> Handle(GetPostgresBlockTipRequest request, CancellationToken cancellationToken)
    {
        await using (var connection = _connectionFactory.CreateConnection())
        {
            // SQL Query: Retrieves the most recent block from the Cardano DB Sync database
            // - Selects basic block information (block number, epoch, previous ID, hash)
            // - Filters for blocks with non-null block_no (avoids orphaned blocks)
            // - Orders by block_no descending and limits to 1 row to get the tip
            // - This gives us the latest block in the longest chain
            string commandText = $"SELECT block_no, epoch_no, previous_id, hash FROM public.block WHERE block_no IS NOT NULL ORDER BY block_no DESC LIMIT 1";
            var block = await connection.QueryFirstOrDefaultAsync<Block>(commandText);
            if (block is null)
            {
                return Result.Fail("Block could not be found");
            }
            
            return Result.Ok(block);
        }
    }
}