using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenPrismNode.Core.Models;
using OpenPrismNode.Sync.Commands.ProcessTransaction;

namespace OpenPrismNode.Sync.Commands.ProcessBlock;

using System.Diagnostics;
using Core.Commands.CreateBlock;
using Core.Commands.GetBlockByBlockHash;
using Core.Common;
using Core.Entities;
using DbSync.GetTransactionsWithPrismMetadataForBlockId;

public class ProcessBlockHandler : IRequestHandler<ProcessBlockRequest, Result<ProcessBlockResponse>>
{
    private readonly IMediator _mediator;
    private readonly ILogger _logger;

    public ProcessBlockHandler(IMediator mediator, IOptions<AppSettings> appSettings, ILogger<ProcessBlockHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<Result<ProcessBlockResponse>> Handle(ProcessBlockRequest request, CancellationToken cancellationToken)
    {
        byte[]? previousBlockHash = request.PreviousBlockHash;
        int? previousBlockHeight = request.PreviousBlockHeight;
        ProcessBlockResponse? response = null;

        var block = await _mediator.Send(new GetBlockByBlockHashRequest(request.Block.block_no, BlockEntity.CalculateBlockHashPrefix(request.Block.hash), request.LedgerType), cancellationToken);
        if (!request.IgnoreCheckForExistingBlock)
        {
            if (block.IsSuccess)
            {
                // already in the db. Nothing to do here anymore
                return Result.Ok(new ProcessBlockResponse(block.Value.BlockHash, block.Value.BlockHeight));
            }

            var prismBlockResult = await _mediator.Send(new CreateBlockRequest(
                ledgerType: request.LedgerType,
                blockHeight: request.Block.block_no,
                blockHash: Hash.CreateFrom(request.Block.hash),
                previousBlockHash: Hash.CreateFrom(previousBlockHash),
                previousBlockHeight: previousBlockHeight,
                epochNumber: request.Block.epoch_no,
                timeUtc: request.Block.time,
                txCount: request.Block.tx_count
            ), cancellationToken);
            if (prismBlockResult.IsFailed)
            {
                return Result.Fail($"Error while creating block #{block.Value.BlockHeight} for {request.LedgerType}: {prismBlockResult.Errors.First().Message}");
            }

            response = new ProcessBlockResponse(prismBlockResult.Value.BlockHash, prismBlockResult.Value.BlockHeight);
        }
        else
        {
            // The block does exist, but we still need to process the transactions
            response = new ProcessBlockResponse(block.Value.BlockHash, block.Value.BlockHeight);
        }

        var blockTransactions = await _mediator.Send(new GetTransactionsWithPrismMetadataForBlockIdRequest(request.Block.id), cancellationToken);
        if (blockTransactions.IsFailed)
        {
            _logger.LogError($"Failed while reading transactions of block # {request.Block.block_no}: {blockTransactions.Errors.First().Message}");
        }

        foreach (var blockTransaction in blockTransactions.Value)
        {
            var result = await _mediator.Send(new ProcessTransactionRequest(request.LedgerType, request.Block, blockTransaction), cancellationToken);

            Debug.Assert(result.IsSuccess);
        }

        return Result.Ok(response);
    }
}