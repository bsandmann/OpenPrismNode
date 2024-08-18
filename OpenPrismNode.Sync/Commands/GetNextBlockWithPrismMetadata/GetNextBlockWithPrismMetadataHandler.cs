using Dapper;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using OpenPrismNode.Sync.Services;

public class GetNextBlockWithPrismMetadataHandlerX : IRequestHandler<GetNextBlockWithPrismMetadataRequest, Result<GetNextBlockWithPrismMetadataResponse>>
{
    private readonly INpgsqlConnectionFactory _connectionFactory;
    private readonly ILogger<GetNextBlockWithPrismMetadataHandlerX> _logger;

    public GetNextBlockWithPrismMetadataHandlerX(INpgsqlConnectionFactory connectionFactory, ILogger<GetNextBlockWithPrismMetadataHandlerX> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<Result<GetNextBlockWithPrismMetadataResponse>> Handle(GetNextBlockWithPrismMetadataRequest request, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();

        // Finding the next block with PRISM metadata is split into two alternative approaches:
        // Finding the inital block can be quite slow, if the first PRISM transaction is years away from the genesis block.
        // So in this case we see if we can find any PRISM transactions first, and then find the block for this transaction (assuming that the PRISM transactions are in order)
        if (request.StartBlockHeight == 1)
        {
            _logger.LogInformation($"Checking for block with PRISM-metadata starting from the genesis block");
            // Step 1: Fetch relevant transaction IDs
            const string metadataQuery = @"
            SELECT tx_id
            FROM public.tx_metadata
            WHERE key = @MetadataKey
            ORDER BY id ASC
            LIMIT 1"; // Adjust this limit based on your typical data

            var txIds = await connection.QueryAsync<long>(metadataQuery, new { request.MetadataKey });

            if (!txIds.Any())
            {
                return Result.Ok(new GetNextBlockWithPrismMetadataResponse());
            }

            // Step 2: Get the block information for these transactions
            const string blockQuery = @"
            SELECT b.block_no, b.epoch_no
            FROM public.tx t
            JOIN public.block b ON t.block_id = b.id
            WHERE t.id = ANY(@TxIds) AND b.block_no > @StartBlockHeight
            ORDER BY b.block_no ASC
            LIMIT 1";

            var result = await connection.QueryFirstOrDefaultAsync<(int? BlockNumber, int? EpochNumber)>(
                blockQuery,
                new { TxIds = txIds.ToArray(), request.StartBlockHeight }
            );

            if (result.BlockNumber.HasValue)
            {
                _logger.LogInformation($"Found frist PRISM block at {result.BlockNumber.Value}");
                return Result.Ok(new GetNextBlockWithPrismMetadataResponse
                {
                    BlockHeight = result.BlockNumber.Value,
                    EpochNumber = result.EpochNumber.Value
                });
            }

            return Result.Ok(new GetNextBlockWithPrismMetadataResponse());
        }
        else
        {
            // In case we already have a starting block, we can use different approach, we just looing ahead for the next blocks.
            // This isn't very fast, but more reliable than the first approach for a chain which structure ins unknown.
            // The first approach might theorectically fail or an continious approach, if we suddently have millions of PRISM transactions on chain.
            const int batchSize = 1_000;
            int currentBlockHeight = request.StartBlockHeight;

            while (true)
            {
                _logger.LogInformation($"Checking for block with PRISM-metadata between {currentBlockHeight} and {currentBlockHeight + batchSize}");
                const string commandText = @"
                 SELECT b.block_no, b.epoch_no
                 FROM public.block b
                 WHERE b.block_no > @StartBlockHeight
                   AND b.block_no <= @EndBlockHeight
                   AND EXISTS (
                     SELECT 1
                     FROM public.tx t
                     JOIN public.tx_metadata m ON t.id = m.tx_id
                     WHERE t.block_id = b.id AND m.key = @MetadataKey
                   )
                 ORDER BY b.block_no ASC
                 LIMIT 1";

                var parameters = new
                {
                    StartBlockHeight = currentBlockHeight,
                    EndBlockHeight = currentBlockHeight + batchSize,
                    request.MetadataKey
                };

                var result = await connection.QueryFirstOrDefaultAsync<(int? BlockNumber, int? EpochNumber)>(commandText, parameters);

                if (result.BlockNumber.HasValue)
                {
                    _logger.LogInformation($"Found next PRISM block at {result.BlockNumber.Value}");
                    return Result.Ok(new GetNextBlockWithPrismMetadataResponse
                    {
                        BlockHeight = result.BlockNumber.Value,
                        EpochNumber = result.EpochNumber.Value
                    });
                }

                currentBlockHeight += batchSize;

                if (currentBlockHeight > request.MaxBlockHeight)
                {
                    return Result.Ok(new GetNextBlockWithPrismMetadataResponse());
                }
            }
        }
    }
}