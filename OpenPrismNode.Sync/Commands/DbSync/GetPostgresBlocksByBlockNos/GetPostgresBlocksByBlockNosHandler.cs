namespace OpenPrismNode.Sync.Commands.DbSync.GetPostgresBlocksByBlockNos
{
    using Dapper;
    using FluentResults;
    using MediatR;
    using OpenPrismNode.Core.DbSyncModels;
    using OpenPrismNode.Sync.Services;

    /// <summary>
    /// Retrieves a range of blocks from the Cardano DB Sync PostgreSQL database by a range of block numbers.
    /// This handler is primarily used for batch operations during fast-sync to efficiently retrieve multiple blocks at once.
    /// </summary>
    public class GetPostgresBlocksByBlockNosHandler : IRequestHandler<GetPostgresBlocksByBlockNosRequest, Result<List<Block>>>
    {
        private readonly INpgsqlConnectionFactory _connectionFactory;

        public GetPostgresBlocksByBlockNosHandler(INpgsqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<Result<List<Block>>> Handle(GetPostgresBlocksByBlockNosRequest request, CancellationToken cancellationToken)
        {
            await using var connection = _connectionFactory.CreateConnection();

            // SQL Query: Retrieves a range of blocks within a specified block number range
            // - Selects core block information (id, hash, epoch, block number, time, tx count, etc.)
            // - Filters blocks by a range from StartBlockNo to EndBlockNo (exclusive upper bound)
            // - Orders results by block_no to ensure sequential processing
            // - Used for batch processing during fast-sync operations
            string commandText = @"
                SELECT id, hash, epoch_no, block_no, time, tx_count, previous_id
                FROM public.block
                WHERE block_no >= @StartBlockNo AND block_no < @EndBlockNo
                ORDER BY block_no;";

            var parameters = new
            {
                StartBlockNo = request.StartBlockNo,
                EndBlockNo = request.StartBlockNo + request.Count
            };

            var blocks = await connection.QueryAsync<Block>(commandText, parameters);

            if (!blocks.Any())
            {
                return Result.Fail("No blocks found in the specified range");
            }

            foreach (var block in blocks)
            {
                block.time = DateTime.SpecifyKind(block.time, DateTimeKind.Utc);
            }

            return Result.Ok(blocks.ToList());
        }
    }
}