namespace OpenPrismNode.Sync.Implementations.Blockfrost;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Commands.ApiSync.GetApiBlocksByNumbers;
using Commands.ApiSync.GetApiNextBlockWithPrismMetadata;
using Commands.ApiSync.GetFirstBlockOfEpoch;
using Commands.DbSync.GetNextBlockWithPrismMetadata;
using Core.Models;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using OpenPrismNode.Core.DbSyncModels;
using OpenPrismNode.Sync.Abstractions;
using OpenPrismNode.Sync.Commands.ApiSync.GetApiBlockTip;
using OpenPrismNode.Sync.Commands.ApiSync.GetApiBlockByNumber;

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
    public async Task<Result<Block>> GetBlockTip(CancellationToken cancellationToken)
    {
        // Use the new API handler to get the block tip
        return await _mediator.Send(new GetApiBlockTipRequest(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<Block>> GetBlockByNumber(int blockNo, CancellationToken cancellationToken)
    {
        // Use the new API handler to get a block by its number
        return await _mediator.Send(new GetApiBlockByNumberRequest(blockNo), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<Block>> GetBlockById(int blockId, CancellationToken cancellationToken, int? blockNo = null)
    {
        if (blockNo is null)
        {
            _logger.LogWarning("GetBlockById is not yet implemented for BlockfrostBlockProviderV2");
            return Result.Fail<Block>("Not implemented yet");
        }
        return await _mediator.Send(new GetApiBlockByNumberRequest(blockNo.Value), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<List<Block>>> GetBlocksByNumbers(int firstBlockNo, int count, CancellationToken cancellationToken)
    {
        return await _mediator.Send(new GetApiBlocksByNumbersRequest(firstBlockNo, count), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<Block>> GetFirstBlockOfEpoch(int epochNo, CancellationToken cancellationToken)
    {
        return await _mediator.Send(new GetFirstBlockOfEpochRequest(epochNo), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<GetNextBlockWithPrismMetadataResponse>> GetNextBlockWithPrismMetadata(int afterBlockNo, int maxBlockNo, LedgerType ledgerType, int metadataKey, int currentBlockTip, CancellationToken cancellationToken)
    {
        return await _mediator.Send(new GetApiNextBlockWithPrismMetadataRequest(afterBlockNo, metadataKey, maxBlockNo, ledgerType, currentBlockTip), cancellationToken);
    }
}