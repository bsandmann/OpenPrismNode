namespace OpenPrismNode.Sync.Commands.GetPostgresBlockTip;

using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using MediatR;
using PostgresModels;
using Dapper;
using Services;

public class GetPostgresBlockTipHandler : IRequestHandler<GetPostgresBlockTipRequest, Result<Block>>
{
    private readonly INpgsqlConnectionFactory _connectionFactory;

    public GetPostgresBlockTipHandler(INpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }


    /// <inheritdoc />
    public async Task<Result<Block>> Handle(GetPostgresBlockTipRequest request, CancellationToken cancellationToken)
    {
        await using (var connection = _connectionFactory.CreateConnection())
        {
            string commandText = $"SELECT * FROM public.block WHERE block_no IS NOT NULL ORDER BY block_no DESC LIMIT 1";
            var block = await connection.QueryFirstOrDefaultAsync<Block>(commandText);
            if (block is null)
            {
                return Result.Fail("Block could not be found");
            }

            return Result.Ok(block);
        }
    }
}