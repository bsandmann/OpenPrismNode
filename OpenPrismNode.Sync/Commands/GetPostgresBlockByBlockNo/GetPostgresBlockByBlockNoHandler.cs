namespace OpenPrismNode.Sync.Commands.GetPostgresBlockByBlockNo;

using Core.DbSyncModels;
using Dapper;
using FluentResults;
using MediatR;
using Services;

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
            string commandText = $"  SELECT id, hash, epoch_no, block_no, time, tx_count FROM public.block WHERE block_no = {request.BlockNo};";
            var block = await connection.QueryFirstOrDefaultAsync<Block>(commandText);
            if (block is null)
            {
                return Result.Fail("Block could not be found");
            }

            block.time = DateTime.SpecifyKind(block.time, DateTimeKind.Utc);
            return Result.Ok(block);
        }
    }
}