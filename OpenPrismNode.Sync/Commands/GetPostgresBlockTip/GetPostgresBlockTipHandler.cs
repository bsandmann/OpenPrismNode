namespace OpenPrismNode.Sync.Commands.GetPostgresBlockTip;

using System.Threading;
using System.Threading.Tasks;
using Core.DbSyncModels;
using FluentResults;
using MediatR;
using Dapper;
using Services;

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
            string commandText = $"SELECT block_no, epoch_no FROM public.block WHERE block_no IS NOT NULL ORDER BY block_no DESC LIMIT 1";
            var block = await connection.QueryFirstOrDefaultAsync<Block>(commandText);
            if (block is null)
            {
                return Result.Fail("Block could not be found");
            }
            
            return Result.Ok(block);
        }
    }
}