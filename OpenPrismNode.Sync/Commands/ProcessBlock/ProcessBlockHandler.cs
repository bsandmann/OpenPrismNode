using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenPrismNode.Core.Models;
using OpenPrismNode.Sync.Commands.ProcessTransaction;

namespace OpenPrismNode.Sync.Commands.ProcessBlock;

using System.Diagnostics;
using Core.Common;
using GetTransactionsWithPrismMetadataForBlockId;

public class ProcessBlockHandler : IRequestHandler<ProcessBlockRequest, Result<Hash?>>
{
    private readonly IMediator _mediator;
    private readonly AppSettings _appSettings;
    private readonly ILogger _logger;
    // private Hash? _previousBlockHash = null;

    public ProcessBlockHandler(IMediator mediator, IOptions<AppSettings> appSettings, ILogger<ProcessBlockHandler> logger)
    {
        _mediator = mediator;
        _appSettings = appSettings.Value;
        _logger = logger;
    }


    public async Task<Result<Hash?>> Handle(ProcessBlockRequest request, CancellationToken cancellationToken)
    {
        // Hash? previousBlockHash = request.PreviousBlockHash;
        // var block = await _mediator.Send(new GetBlockByHashRequest(Hash.CreateFrom(request.Block.hash)));
        // if (block.IsSuccess)
        // {
        //     // already in the db. Nothing to do here anymore
        // }
        // else
        // {
        //     if (previousBlockHash is null)
        //     {
        //         // special case, when starting this method. The previous blockHash should be null when we start the db
        //         // but when we have done a rollback before we have to connect the oldchain to new continuation
        //         var previousBlock = await _mediator.Send(new GetPostgresBlockByBlockIdRequest(request.Block.previous_id));
        //         if (previousBlock.IsSuccess)
        //         {
        //             //lets try to find that block in our db
        //             var previousBlockHashDb = Hash.CreateFrom(previousBlock.Value.hash);
        //             var previousBlockInDb = await _mediator.Send(new GetBlockByHashRequest(previousBlockHashDb));
        //             if (previousBlockInDb.IsSuccess)
        //             {
        //                 previousBlockHash = previousBlockInDb.Value.BlockHash;
        //             }
        //         }
        //     }
        //
        //     var prismBlockModelResult = await _mediator.Send(new CreateBlockRequest(
        //         blockHash: Hash.CreateFrom(request.Block.hash),
        //         blockHeight: request.Block.block_no,
        //         epoch: (uint)request.Block.epoch_no,
        //         epochSlot: request.Block.epoch_slot_no,
        //         timeUtc: request.Block.time,
        //         txCount: (uint)request.Block.tx_count,
        //         previousBlockHash: previousBlockHash
        //     ));
        //     previousBlockHash = prismBlockModelResult.Value.BlockHash;


        var blockTransactions = await _mediator.Send(new GetTransactionsWithPrismMetadataForBlockIdRequest(request.Block.id), cancellationToken);
        if (blockTransactions.IsFailed)
        {
            _logger.LogError($"Failed while reading transactions of block # {request.Block.block_no}: {blockTransactions.Errors.First().Message}");
        }

        foreach (var blockTransaction in blockTransactions.Value)
        {
            var result = await _mediator.Send(new ProcessTransactionRequest(request.Block, blockTransaction), cancellationToken);

            Debug.Assert(result.IsSuccess);
        }

        return Result.Ok();
        // return Result.Ok(previousBlockHash);
    }
}