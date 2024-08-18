using Dapper;
using FluentResults;
using LazyCache;
using MediatR;
using Microsoft.Extensions.Logging;
using OpenPrismNode.Sync.Services;

public class GetNextBlockWithPrismMetadataHandler : IRequestHandler<GetNextBlockWithPrismMetadataRequest, Result<GetNextBlockWithPrismMetadataResponse>>
{
    private readonly INpgsqlConnectionFactory _connectionFactory;
    private readonly ILogger<GetNextBlockWithPrismMetadataHandler> _logger;
    private readonly IAppCache _cache;

    public GetNextBlockWithPrismMetadataHandler(INpgsqlConnectionFactory connectionFactory, ILogger<GetNextBlockWithPrismMetadataHandler> logger, IAppCache cache)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
        _cache = cache;
    }

    public async Task<Result<GetNextBlockWithPrismMetadataResponse>> Handle(GetNextBlockWithPrismMetadataRequest request, CancellationToken cancellationToken)
    {
        var lowestBlockInCache = _cache.TryGetValue("LowestBlock", out BlockMetadataInfo lowestBlock);
        if (!lowestBlockInCache)
        {
            // Fill it with all entries using batching
            var cachingResult = await PopulateCache(request.MetadataKey);
            if (cachingResult.IsFailed)
            {
                return cachingResult.ToResult();
            }

            if (cachingResult.Value is null)
            {
                // No PRISM metadata found at all
                return Result.Ok(new GetNextBlockWithPrismMetadataResponse());
            }

            lowestBlock = cachingResult.Value;
        }

        if (request.StartBlockHeight <= lowestBlock.BlockNumber)
        {
            return Result.Ok(new GetNextBlockWithPrismMetadataResponse
            {
                BlockHeight = lowestBlock.BlockNumber,
                EpochNumber = lowestBlock.EpochNumber
            });
        }

        var highestBlockInCache = _cache.TryGetValue("HighestBlock", out BlockMetadataInfo highestBlock);
        if (!highestBlockInCache)
        {
            var cachingResult = await PopulateCache(request.MetadataKey);
            if (cachingResult.IsFailed)
            {
                return cachingResult.ToResult();
            }

            if (cachingResult.Value is null)
            {
                // No PRISM metadata found at all
                return Result.Ok(new GetNextBlockWithPrismMetadataResponse());
            }

            highestBlockInCache = _cache.TryGetValue("HighestBlock", out BlockMetadataInfo secondTryHighestBlock);
            if (!highestBlockInCache)
            {
                return Result.Fail("Highest block not found in cache. Caching error");
            }

            highestBlock = secondTryHighestBlock;
        }

        if (request.StartBlockHeight > highestBlock.BlockNumber)
        {
            // The sync up to the newest block until the non-fast-sync operation kicks in
            return await SearchForNextPrismTransactionInBatches(request.StartBlockHeight, request.MetadataKey, request.MaxBlockHeight);
        }
        else
        {
            // The default case for the initial sync process
            var cacheResult = _cache.TryGetValue("AllBlocks", out List<BlockMetadataInfo> listOfBlocks);
            if (!cacheResult)
            {
                return Result.Fail("Cache error");
            }

            var nextBlock = listOfBlocks.FirstOrDefault(p => p.BlockNumber >= request.StartBlockHeight);
            if (nextBlock != null)
            {
                return Result.Ok(new GetNextBlockWithPrismMetadataResponse
                {
                    BlockHeight = nextBlock.BlockNumber,
                    EpochNumber = nextBlock.EpochNumber
                });
            }
            else
            {
                return Result.Fail("Should not happen");
            }
        }


        return Result.Fail("");
    }

    private async Task<Result<BlockMetadataInfo?>> PopulateCache(int metadataKey)
    {
        await using var connection = _connectionFactory.CreateConnection();
        _logger.LogInformation($"Checking for block with PRISM-metadata starting from the genesis block");

        const int batchSize = 5000;
        long lastId = 0;
        var allResults = new List<BlockMetadataInfo>();

        while (true)
        {
            // Step 1: Fetch relevant transaction IDs in batches
            const string metadataQuery = @"
            SELECT id, tx_id
            FROM public.tx_metadata
            WHERE key = @MetadataKey AND id > @LastId
            ORDER BY id ASC
            LIMIT @BatchSize";

            var txBatch = await connection.QueryAsync<(long Id, long TxId)>(metadataQuery, 
                new { MetadataKey = metadataKey, LastId = lastId, BatchSize = batchSize });

            if (!txBatch.Any())
            {
                break; // No more data to process
            }

            var txIds = txBatch.Select(t => t.TxId).ToArray();
            lastId = txBatch.Last().Id;

            // Step 2: Get all the blocks matching these metadata-transactions
            const string blockQuery = @"
            SELECT b.block_no, b.epoch_no
            FROM public.tx t
            JOIN public.block b ON t.block_id = b.id
            WHERE t.id = ANY(@TxIds)
            ORDER BY b.block_no ASC";

            var results = await connection.QueryAsync<(int BlockNumber, int EpochNumber)>(
                blockQuery,
                new { TxIds = txIds }
            );

            allResults.AddRange(results.Select(p => new BlockMetadataInfo 
            { 
                BlockNumber = p.BlockNumber, 
                EpochNumber = p.EpochNumber 
            }));
        }

        if (!allResults.Any())
        {
            return Result.Ok();
        }

        var orderedResults = allResults.OrderBy(p => p.BlockNumber).ToList();

        _cache.Add("LowestBlock", orderedResults.First());
        _cache.Add("HighestBlock", orderedResults.Last());
        _cache.Add("AllBlocks", orderedResults);

        return Result.Ok(orderedResults.First()!);
    }

    public class BlockMetadataInfo
    {
        public int BlockNumber { get; set; }
        public int EpochNumber { get; set; }
    }

    private async Task<Result<GetNextBlockWithPrismMetadataResponse>> SearchForNextPrismTransactionInBatches(int startBlockHeight, int metadataKey, int maxBlockHeight)
    {
        await using var connection = _connectionFactory.CreateConnection();
        const int batchSize = 1_000;
        int currentBlockHeight = startBlockHeight;

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
                metadataKey
            };

            var result = await connection.QueryFirstOrDefaultAsync<(int? BlockNumber, int? EpochNumber, int? TransactionId)>(commandText, parameters);

            if (result.BlockNumber.HasValue)
            {
                _logger.LogInformation($"Found next PRISM block at {result.BlockNumber.Value}");

                var cacheResult = _cache.TryGetValue("AllBlock", out List<BlockMetadataInfo> listOfBlocks);
                if (!cacheResult)
                {
                    return Result.Fail("Cache error");
                }

                listOfBlocks.Add(new BlockMetadataInfo()
                {
                    BlockNumber = result.BlockNumber.Value,
                    EpochNumber = result.EpochNumber.Value,
                });

                _cache.Add("HighestBlock", listOfBlocks.Last());
                _cache.Add<List<BlockMetadataInfo>>("AllBlocks", listOfBlocks);
                return Result.Ok(
                    new GetNextBlockWithPrismMetadataResponse
                    {
                        BlockHeight = result.BlockNumber.Value,
                        EpochNumber = result.EpochNumber.Value
                    });
            }

            currentBlockHeight += batchSize;

            if (currentBlockHeight > maxBlockHeight)
            {
                return Result.Ok(new GetNextBlockWithPrismMetadataResponse());
            }
        }
    }
}