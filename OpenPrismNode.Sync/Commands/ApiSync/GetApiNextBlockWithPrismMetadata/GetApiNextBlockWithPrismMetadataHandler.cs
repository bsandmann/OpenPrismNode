using System.Diagnostics;
using FluentResults;
using LazyCache;
using MediatR;
using Microsoft.Extensions.Logging;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Sync.Commands.ApiSync.GetApiBlockByNumber;
using OpenPrismNode.Sync.Commands.ApiSync.GetApiNextBlockWithPrismMetadata;
using OpenPrismNode.Sync.Commands.ApiSync.GetApiTransactionByHash;
using OpenPrismNode.Sync.Commands.ApiSync.GetApiTransactionMetadata;
using OpenPrismNode.Sync.Commands.DbSync.GetNextBlockWithPrismMetadata;
using OpenPrismNode.Sync.Services;

public class GetApiNextBlockWithPrismMetadataHandler
    : IRequestHandler<GetApiNextBlockWithPrismMetadataRequest, Result<GetNextBlockWithPrismMetadataResponse>>
{
    private readonly INpgsqlConnectionFactory _connectionFactory;
    private readonly ILogger<GetApiNextBlockWithPrismMetadataHandler> _logger;
    private readonly IAppCache _cache;
    private readonly IMetadataCacheService _metadataCacheService;
    private readonly IMediator _mediator;

    public GetApiNextBlockWithPrismMetadataHandler(
        INpgsqlConnectionFactory connectionFactory,
        ILogger<GetApiNextBlockWithPrismMetadataHandler> logger,
        IAppCache cache,
        IMetadataCacheService metadataCacheService,
        IMediator mediator)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
        _cache = cache;
        _metadataCacheService = metadataCacheService;
        _mediator = mediator;
    }

    public async Task<Result<GetNextBlockWithPrismMetadataResponse>> Handle(
        GetApiNextBlockWithPrismMetadataRequest request,
        CancellationToken cancellationToken)
    {
        // 1. Check if the cache is loaded; if not, rebuild it
        var hasExistingList = _cache.TryGetValue<List<TransactionBlockWrapper>>(
            CacheKeys.TransactionList_with_Metadata,
            out var transactionHashes
        );
        if (!hasExistingList || transactionHashes == null)
        {
            await _metadataCacheService.RebuildCacheAsync(request.CurrentApiBlockTip, cancellationToken);
            var rebuildHasExistingList = _cache.TryGetValue<List<TransactionBlockWrapper>>(
                CacheKeys.TransactionList_with_Metadata,
                out transactionHashes
            );
            if (!rebuildHasExistingList || transactionHashes == null)
            {
                return Result.Fail("Failed to rebuild cache with all prism transactions");
            }
        }

        // 2. If still no transactions, return
        if (!transactionHashes.Any())
        {
            return Result.Ok(new GetNextBlockWithPrismMetadataResponse());
        }

        // 3. Quick edge case: if the earliest (first) transaction is above StartBlockHeight 
        //    then check if it is within MaxBlockHeight and return immediately.
        //    (But we have to ensure we know the block height of the first item.)
        var firstItem = transactionHashes.First();
        if (firstItem.BlockHeight is null)
        {
            var blockHeightResult = await GetBlockNoForTransactionHash(firstItem.TxHash);
            if (blockHeightResult.IsFailed)
            {
                return blockHeightResult.ToResult();
            }

            firstItem.BlockHeight = blockHeightResult.Value;
        }

        if (firstItem.BlockHeight > request.StartBlockHeight)
        {
            // Check if it also lies below MaxBlockHeight
            if (firstItem.BlockHeight <= request.MaxBlockHeight)
            {
                var epochResult = await GeEpochNumberForBlockNo(firstItem.BlockHeight.Value);
                if (epochResult.IsFailed) return epochResult.ToResult();

                // Update the cache with any newly resolved block heights
                _cache.Add(CacheKeys.TransactionList_with_Metadata, transactionHashes);
                return Result.Ok(new GetNextBlockWithPrismMetadataResponse()
                {
                    BlockHeight = firstItem.BlockHeight,
                    EpochNumber = epochResult.Value
                });
            }

            // Otherwise nothing found
            return Result.Ok(new GetNextBlockWithPrismMetadataResponse());
        }

        // 4. Another quick edge case: if the latest (last) transaction is still below StartBlockHeight
        //    there's no block to find in the future range.
        var lastItem = transactionHashes.Last();
        if (lastItem.BlockHeight is null)
        {
            var blockHeightResult = await GetBlockNoForTransactionHash(lastItem.TxHash);
            if (blockHeightResult.IsFailed)
            {
                return blockHeightResult.ToResult();
            }

            lastItem.BlockHeight = blockHeightResult.Value;
        }

        if (lastItem.BlockHeight < request.StartBlockHeight)
        {
            // No transactions at or above StartBlockHeight
            return Result.Ok(new GetNextBlockWithPrismMetadataResponse());
        }

#if DEBUG
        // Optional: sanity-check that transactionHashes is sorted by actual block height
        // This can be expensive if the list is large, so you might want to keep it in DEBUG only.
        for (int i = 0; i < transactionHashes.Count - 1; i++)
        {
            var currBlockHeight = transactionHashes[i].BlockHeight;
            var nextBlockHeight = transactionHashes[i + 1].BlockHeight;
            // If either is null, we skip the check or do a quick partial check if possible
            if (currBlockHeight.HasValue && nextBlockHeight.HasValue)
            {
                Debug.Assert(currBlockHeight.Value <= nextBlockHeight.Value,
                    "Transactions should be sorted in ascending block order!");
            }
        }
#endif
        if (request.StartBlockHeight < request.CurrentApiBlockTip || request.MaxBlockHeight < request.CurrentApiBlockTip)
        {
            await _metadataCacheService.UpdateCacheAsync(request.CurrentApiBlockTip, cancellationToken);
        }

        // 5. Now the actual search algorithm (binary search).
        //    We want to find the first transaction whose block height > StartBlockHeight.
        var index = await BinarySearchNextBlockAsync(transactionHashes, request.StartBlockHeight);
        if (index.IsFailed)
        {
            return index.ToResult();
        }

        // If index is out of range => nothing found
        if (index.Value < 0 || index.Value >= transactionHashes.Count)
        {
            // No suitable block found
            return Result.Ok(new GetNextBlockWithPrismMetadataResponse());
        }

        var candidate = transactionHashes[index.Value];
        // We must ensure block height is fetched
        if (candidate.BlockHeight is null)
        {
            var blockHeightResult = await GetBlockNoForTransactionHash(candidate.TxHash);
            if (blockHeightResult.IsFailed)
            {
                return blockHeightResult.ToResult();
            }

            candidate.BlockHeight = blockHeightResult.Value;
        }

        // Check if it's within MaxBlockHeight
        if (candidate.BlockHeight > request.MaxBlockHeight)
        {
            // No suitable block in the range
            return Result.Ok(new GetNextBlockWithPrismMetadataResponse());
        }

        // If we get here, we have our next block
        var epochRes = await GeEpochNumberForBlockNo(candidate.BlockHeight.Value);
        if (epochRes.IsFailed) return epochRes.ToResult();

        // 6. Update the cache with any newly resolved block heights
        _cache.Add(CacheKeys.TransactionList_with_Metadata, transactionHashes);

        return Result.Ok(new GetNextBlockWithPrismMetadataResponse
        {
            BlockHeight = candidate.BlockHeight,
            EpochNumber = epochRes.Value
        });
    }

    /// <summary>
    /// Perform a binary search over the transaction list, which is sorted by ascending
    /// actual block order. We find the first transaction whose block height
    /// is strictly greater than the given <paramref name="startBlockHeight"/>.
    /// 
    /// We resolve (fetch) block heights as needed, but keep them in memory to avoid
    /// re-calling the API for the same index.
    /// </summary>
    private async Task<Result<int>> BinarySearchNextBlockAsync(
        List<TransactionBlockWrapper> transactions,
        int startBlockHeight)
    {
        int left = 0;
        int right = transactions.Count - 1;
        int resultIndex = -1;

        while (left <= right)
        {
            int mid = (left + right) / 2;

            // Resolve mid's block height if necessary
            var midHeight = await ResolveBlockHeightAsync(transactions, mid);
            if (midHeight.IsFailed)
            {
                return midHeight.ToResult();
            }

            if (midHeight.Value > startBlockHeight)
            {
                // We found a candidate, but there might be an earlier one
                resultIndex = mid;
                right = mid - 1;
            }
            else
            {
                // midHeight <= startBlockHeight, so the desired block is to the right
                left = mid + 1;
            }
        }

        return resultIndex;
    }

    /// <summary>
    /// Helper to ensure we have the block height in the given transaction item.
    /// If it is null, we call the API once; otherwise we just return it from memory.
    /// </summary>
    private async Task<Result<int>> ResolveBlockHeightAsync(List<TransactionBlockWrapper> transactions, int index)
    {
        var txItem = transactions[index];
        if (!txItem.BlockHeight.HasValue)
        {
            var blockHeightResult = await GetBlockNoForTransactionHash(txItem.TxHash);
            if (blockHeightResult.IsSuccess)
            {
                txItem.BlockHeight = blockHeightResult.Value;
            }
            else
            {
                return Result.Fail("Failed to resolve block height for transaction");
            }
        }

        return txItem.BlockHeight.Value;
    }

    private async Task<Result<int>> GetBlockNoForTransactionHash(string txHash)
    {
        var result = await _mediator.Send(new GetApiTransactionByHashRequest(txHash));
        if (result.IsFailed)
        {
            return result.ToResult();
        }

        return result.Value.BlockNo!;
    }

    private async Task<Result<int>> GeEpochNumberForBlockNo(int blockNo)
    {
        var result = await _mediator.Send(new GetApiBlockByNumberRequest(blockNo));
        if (result.IsFailed)
        {
            return result.ToResult();
        }

        return result.Value.epoch_no;
    }
}