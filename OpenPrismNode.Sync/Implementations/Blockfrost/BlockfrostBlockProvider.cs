namespace OpenPrismNode.Sync.Implementations.Blockfrost;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using OpenPrismNode.Core.DbSyncModels;
using OpenPrismNode.Sync.Abstractions;
using OpenPrismNode.Sync.Commands.ApiSync.GetApiBlockTip;

/// <summary>
/// Implementation of the IBlockProvider interface that retrieves data from the Blockfrost API.
/// This implementation uses dedicated API handlers for each operation type.
/// </summary>
public class BlockfrostBlockProvider : IBlockProvider
{
    private readonly IMediator _mediator;
    private readonly ILogger<BlockfrostBlockProvider> _logger;
    private readonly ITransactionProvider _transactionProvider;

    public BlockfrostBlockProvider(
        IMediator mediator,
        ILogger<BlockfrostBlockProvider> logger,
        ITransactionProvider transactionProvider)
    {
        _mediator = mediator;
        _logger = logger;
        _transactionProvider = transactionProvider;
    }

    /// <inheritdoc />
    public async Task<Result<Block>> GetBlockTip(CancellationToken cancellationToken = default)
    {
        // Use the new API handler to get the block tip
        return await _mediator.Send(new GetApiBlockTipRequest(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<Block>> GetBlockByNumber(int blockNo, CancellationToken cancellationToken = default)
    {
        // TODO: Implement GetApiBlockByNumberRequest/Handler
        _logger.LogWarning("GetBlockByNumber is not yet implemented for BlockfrostBlockProviderV2");
        return Result.Fail<Block>("Not implemented yet");
    }

    /// <inheritdoc />
    public async Task<Result<Block>> GetBlockById(int blockId, CancellationToken cancellationToken = default)
    {
        // TODO: Implement GetApiBlockByIdRequest/Handler
        _logger.LogWarning("GetBlockById is not yet implemented for BlockfrostBlockProviderV2");
        return Result.Fail<Block>("Not implemented yet");
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<Block>>> GetBlocksByNumbers(IEnumerable<int> blockNos, CancellationToken cancellationToken = default)
    {
        // TODO: Implement GetApiBlocksByNumbersRequest/Handler
        _logger.LogWarning("GetBlocksByNumbers is not yet implemented for BlockfrostBlockProviderV2");
        return Result.Fail<IEnumerable<Block>>("Not implemented yet");
    }

    /// <inheritdoc />
    public async Task<Result<Block>> GetFirstBlockOfEpoch(int epochNo, CancellationToken cancellationToken = default)
    {
        // TODO: Implement GetApiFirstBlockOfEpochRequest/Handler
        _logger.LogWarning("GetFirstBlockOfEpoch is not yet implemented for BlockfrostBlockProviderV2");
        return Result.Fail<Block>("Not implemented yet");
    }

    /// <inheritdoc />
    public async Task<Result<Block>> GetNextBlockWithPrismMetadata(int afterBlockNo, CancellationToken cancellationToken = default)
    {
        // TODO: Implement GetApiNextBlockWithPrismMetadataRequest/Handler
        _logger.LogWarning("GetNextBlockWithPrismMetadata is not yet implemented for BlockfrostBlockProviderV2");
        return Result.Fail<Block>("Not implemented yet");
    }
}