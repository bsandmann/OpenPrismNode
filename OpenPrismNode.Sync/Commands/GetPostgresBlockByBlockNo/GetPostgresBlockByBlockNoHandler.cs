namespace OpenPrismNode.Sync.Commands.GetPostgresBlockByBlockNo;

using Dapper;
using FluentResults;
using MediatR;
using OpenPrismNode.Sync.PostgresModels;
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
            // TODO optimize query to get only what is needed.
            string commandText = $"SELECT * FROM public.block WHERE block_no = {request.BlockNo};";
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