using Dapper;
using FluentResults;
using MediatR;
using OpenPrismNode.Core.DbSyncModels;
using OpenPrismNode.Sync.Services;

namespace OpenPrismNode.Sync.Commands.GetPostgresFirstBlockOfEpoch;

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