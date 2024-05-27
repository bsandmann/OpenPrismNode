namespace OpenPrismNode.Sync.Services;

using Commands.GetPostgresBlockTip;
using Core.Models;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;

public static class SyncService
{
    public static async Task<Result> RunSync(IMediator mediator, ILogger logger, string networkName, bool isInitialStartup = false)
    {
        LedgerType ledgerType;
        if (networkName.Equals("mainnet", StringComparison.InvariantCultureIgnoreCase))
        {
            ledgerType = LedgerType.CardanoMainnet;
        }
        else if (networkName.Equals("preprod", StringComparison.InvariantCultureIgnoreCase))
        {
            ledgerType = LedgerType.CardanoPreprod;
        }
        else
        {
            return Result.Fail("Unknown network");
        }

        var postgresBlockTipResult = await mediator.Send(new GetPostgresBlockTipRequest());
        if (postgresBlockTipResult.IsFailed)
        {
            return Result.Fail(postgresBlockTipResult.Errors.First().Message);
        }

        // if (isInitialStartup)
        // {
        //     var existingStartingEpoch = await mediator.Send(new GetEpochRequest(0), CancellationToken.None);
        //     if (existingStartingEpoch.IsFailed)
        //     {
        //         // This indicates that we have not yet created the starting epoch
        //         var networkResult = await mediator.Send(new CreateNetworkRequest(ledgerType));
        //         var createStartingEpochResult = await mediator.Send(new CreateEpochRequest(ledgerType, 0), CancellationToken.None);
        //         if (createStartingEpochResult.IsFailed)
        //         {
        //             return Result.Fail("Cannot create starting epoch. Database connection error?");
        //         }
        //
        //         var blocksOfEpoch = await mediator.Send(new GetPostgresBlocksOfEpochRequest(0));
        //         var firstBlock = blocksOfEpoch.Value.MinBy(p => p.block_no);
        //         if (firstBlock is null)
        //         {
        //             return Result.Fail("First block could not be found. Database connection error?");
        //         }
        //
        //         var firstBlockResult = await mediator.Send(new CreateBlockRequest(
        //             blockHash: Hash.CreateFrom(firstBlock.hash),
        //             blockHeight: firstBlock.block_no,
        //             epoch: (uint)firstBlock.epoch_no,
        //             epochSlot: firstBlock.epoch_slot_no,
        //             timeUtc: firstBlock.time,
        //             txCount: (uint)firstBlock.tx_count,
        //             previousBlockHash: null));
        //         if (firstBlockResult.IsFailed)
        //         {
        //             return Result.Fail("Cannot create first block. Database connection error?");
        //         }
        //     }
        // }

        // var sqlBlockTipResult = await mediator.Send(new GetMostRecentBlockRequest());
        // if (sqlBlockTipResult.IsFailed)
        // {
        //     return Result.Fail(sqlBlockTipResult.Errors.First().Message);
        // }
        //
        // var blockInDb = await mediator.Send(new GetBlockByHashRequest(Hash.CreateFrom(postgresBlockTipResult.Value.hash)));
        // if (blockInDb.IsSuccess)
        // {
        //     logger.LogInformation($"{networkName} is already up to date");
        //     return Result.Ok();
        // }

        // ATTENTION: This code does not really support rollbacks, as the old Version did!
        // var previousBlockHash = sqlBlockTipResult.Value.BlockHash;
        //
        // var lastAnalyticsUpdate = DateTime.MinValue;
        // for (int i = (int)(sqlBlockTipResult.Value.BlockHeight + 1); i <= postgresBlockTipResult.Value.block_no; i++)
        // {
        //     var getBlockByIdResult = await mediator.Send(new GetPostgresBlockByBlockNoRequest(i));
        //     if (getBlockByIdResult.IsFailed)
        //     {
        //         logger.LogError("Cannot read block from postgres: {Error}", getBlockByIdResult.Errors.First().Message);
        //         return Result.Fail(getBlockByIdResult.Errors.First().Message);
        //     }
        //
        //     var epoch = (uint)getBlockByIdResult.Value.epoch_no;
        //     var existigEpoch = await mediator.Send(new GetEpochRequest(epoch), CancellationToken.None);
        //     if (existigEpoch.IsFailed)
        //     {
        //         // we still need to create that epoch
        //         logger.LogInformation($"{networkName}: Creating new epoch {epoch}");
        //         await mediator.Send(new CreateEpochRequest(ledgerType, epoch), CancellationToken.None);
        //     }
        //
        //     var processBlockResult = await mediator.Send(new ProcessBlockRequest(getBlockByIdResult.Value, previousBlockHash));
        //     if (processBlockResult.IsFailed)
        //     {
        //         logger.LogError(processBlockResult.Errors.First().Message);
        //     }
        //
        //     var analyticsServerWebhookScoped = new AnalyticsServerWebhookSyncRequest(i);
        //     if (DateTime.UtcNow - lastAnalyticsUpdate > TimeSpan.FromSeconds(3))
        //     {
        //         var notificationResult = await mediator.Send(analyticsServerWebhookScoped);
        //         if (notificationResult.IsFailed)
        //         {
        //             logger.LogError(notificationResult.Errors.FirstOrDefault().Message);
        //         }
        //
        //         lastAnalyticsUpdate = DateTime.UtcNow;
        //     }
        //
        //     previousBlockHash = processBlockResult.Value;
        // }
        //
        return Result.Ok();
    }
}