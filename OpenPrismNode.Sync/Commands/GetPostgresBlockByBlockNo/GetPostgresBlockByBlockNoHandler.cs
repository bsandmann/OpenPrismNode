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

            block.time = DateTime.SpecifyKind(block.time, DateTimeKind.Utc);
            return Result.Ok(block);
        }
    }
}