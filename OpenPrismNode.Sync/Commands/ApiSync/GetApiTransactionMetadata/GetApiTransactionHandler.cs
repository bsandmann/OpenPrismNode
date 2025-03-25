namespace OpenPrismNode.Sync.Commands.ApiSync.GetApiTransactionMetadata;

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using GetApiTransactionByHash;
using LazyCache;
using MediatR;
using Microsoft.Extensions.Logging;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Core.DbSyncModels;
using OpenPrismNode.Sync.Services;

/// <summary>
/// Retrieves transaction metadata from the Blockfrost API using a transaction hash.
/// </summary>
public class GetApiTransactionHandler : IRequestHandler<GetApiTransactionRequest, Result<Transaction?>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<GetApiTransactionHandler> _logger;
    private readonly IAppCache _cache;
    private readonly IMetadataCacheService _metadataCacheService;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetApiTransactionHandler"/> class.
    /// </summary>
    public GetApiTransactionHandler(
        IMediator mediator,
        ILogger<GetApiTransactionHandler> logger,
        IAppCache cache,
        IMetadataCacheService metadataCacheService)
    {
        _mediator = mediator;
        _logger = logger;
        _cache = cache;
        _metadataCacheService = metadataCacheService;
    }

    /// <summary>
    /// Handles the request to retrieve transaction metadata from the Blockfrost API.
    /// </summary>
    /// <param name="request">The request object containing the transaction hash</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A result containing the transaction data or an error</returns>
    public async Task<Result<Transaction?>> Handle(GetApiTransactionRequest request, CancellationToken cancellationToken)
    {
        // if (string.IsNullOrWhiteSpace(request.TxHash))
        // {
        //     _logger.LogError("Transaction hash is required");
        //     return Result.Fail<Transaction>("Transaction hash is required");
        // }

        var cacheResult = _cache.TryGetValue(string.Concat(CacheKeys.MetadataFromPrismTransaction, request.TxHash), out TransactionMetadataWrapper transactionMetadataWrapper);
        if (cacheResult)
        {
            var transactionResult = await _mediator.Send(new GetApiTransactionByHashRequest(request.TxHash), cancellationToken);
            if (transactionResult.IsFailed)
            {
                return transactionResult.ToResult();
            }

            return Result.Ok<Transaction?>(transactionResult.Value);
        }

        var lastBlockWithUpdate = _cache.TryGetValue(CacheKeys.BlockNoOfMetadataCacheUpdate, out int updatedOnBlockNo);

        if (!lastBlockWithUpdate)
        {
            // We don't find the last block with update, so we need to rebuild the cache
            var cachingResult = await _metadataCacheService.RebuildCacheAsync(request.CurrentApiBlockTip, cancellationToken);
            if (cachingResult.IsFailed)
            {
                return cachingResult.ToResult<Transaction?>();
            }

            _metadataCacheService.UpdateBlockNoOfMetadataCacheUpdate(request.CurrentBlockNo);

            // Look again in the cache
            var cacheResult2 = _cache.TryGetValue(string.Concat(CacheKeys.MetadataFromPrismTransaction, request.TxHash), out TransactionMetadataWrapper transactionMetadataWrapper2);
            if (cacheResult2)
            {
                var transactionResult = await _mediator.Send(new GetApiTransactionByHashRequest(request.TxHash), cancellationToken);
                if (transactionResult.IsFailed)
                {
                    return transactionResult.ToResult();
                }

                return Result.Ok<Transaction?>(transactionResult.Value);
            }

            return Result.Ok();
        }
        else if (lastBlockWithUpdate && updatedOnBlockNo < request.CurrentBlockNo)
        {
            var currentApiBlockTipResult = _cache.TryGetValue(CacheKeys.TipOfMetadataCacheUpdate, out int currentApiBlockTip);
            if (currentApiBlockTipResult && currentApiBlockTip >= request.CurrentBlockNo)
            {
                // We don't need to update the cache. And since we have found it initially we don't have to look any further
                return Result.Ok<Transaction?>(null);
            }

            // The cache is outdated, so we need to rebuild it
            var cachingResult = await _metadataCacheService.UpdateCacheAsync(request.CurrentApiBlockTip, cancellationToken);
            if (cachingResult.IsFailed)
            {
                return cachingResult.ToResult<Transaction?>();
            }

            _metadataCacheService.UpdateBlockNoOfMetadataCacheUpdate(request.CurrentBlockNo);

            // Look again in the cache
            var cacheResult2 = _cache.TryGetValue(string.Concat(CacheKeys.MetadataFromPrismTransaction, request.TxHash), out TransactionMetadataWrapper transactionMetadataWrapper2);
            if (cacheResult2)
            {
                var transactionResult = await _mediator.Send(new GetApiTransactionByHashRequest(request.TxHash), cancellationToken);
                if (transactionResult.IsFailed)
                {
                    return transactionResult.ToResult();
                }

                return Result.Ok<Transaction?>(transactionResult.Value);
            }

            return Result.Ok<Transaction?>(null);
        }
        else if (lastBlockWithUpdate && updatedOnBlockNo == request.CurrentBlockNo)
        {
            // The cache is up to date, but the transaction is not found
            return Result.Ok<Transaction?>(null);
        }
        else if (lastBlockWithUpdate && updatedOnBlockNo > request.CurrentBlockNo)
        {
            // The cache is ahead of the current block
            // This might happen due to a rollback, so we need to rebuild the cache,
            // but with a calculated formular to avoid overcaching
            // We assume that the maximum number of items in a block is 100
            // so the maximum number of pages we have to update is cache is the number of blocks
            int blocksRolledBack = updatedOnBlockNo - request.CurrentBlockNo;

            var rollbackResult = await _metadataCacheService.RollbackCacheAsync(blocksRolledBack, request.CurrentApiBlockTip, cancellationToken);
            if (rollbackResult.IsFailed)
            {
                return rollbackResult.ToResult<Transaction?>();
            }

            // After rolling back, store the new "current" block in the cache
            _metadataCacheService.UpdateBlockNoOfMetadataCacheUpdate(request.CurrentBlockNo);

            // Try to get the transaction again from the cache
            var cacheResult2 = _cache.TryGetValue(
                string.Concat(CacheKeys.MetadataFromPrismTransaction, request.TxHash),
                out TransactionMetadataWrapper transactionMetadataWrapper2
            );

            if (cacheResult2)
            {
                // If found, fetch from DB
                var transactionResult = await _mediator.Send(
                    new GetApiTransactionByHashRequest(request.TxHash),
                    cancellationToken
                );

                if (transactionResult.IsFailed)
                {
                    return transactionResult.ToResult();
                }

                return Result.Ok<Transaction?>(transactionResult.Value);
            }

            return Result.Ok<Transaction?>(null);
        }

        return Result.Ok();
    }
}