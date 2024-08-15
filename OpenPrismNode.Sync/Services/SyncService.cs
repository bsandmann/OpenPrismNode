namespace OpenPrismNode.Sync.Services;

using Commands.GetPostgresBlockByBlockNo;
using Commands.GetPostgresBlockTip;
using Commands.GetPostgresFirstBlockOfEpoch;
using Commands.ProcessBlock;
using Core.Commands.CreateBlock;
using Core.Commands.CreateEpoch;
using Core.Commands.CreateNetwork;
using Core.Commands.GetBlockByBlockHeight;
using Core.Commands.GetEpoch;
using Core.Commands.GetMostRecentBlock;
using Core.Models;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using PostgresModels;

public static class SyncService
{
    public static async Task<Result> RunSync(IMediator mediator, ILogger logger, string networkName, int startAtEpochNumber = 0, bool isInitialStartup = false)
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

        if (isInitialStartup)
        {
            var existingStartingEpoch = await mediator.Send(new GetEpochRequest(ledgerType, startAtEpochNumber), CancellationToken.None);
            if (existingStartingEpoch.IsFailed)
            {
                // This indicates that we have not yet created the starting epoch
                var networkResult = await mediator.Send(new CreateNetworkRequest(ledgerType));
                if (networkResult.IsFailed)
                {
                    return Result.Fail($"Cannot create network {ledgerType}. Database connection error?");
                }

                var createStartingEpochResult = await mediator.Send(new CreateEpochRequest(ledgerType, startAtEpochNumber), CancellationToken.None);
                if (createStartingEpochResult.IsFailed)
                {
                    return Result.Fail($"Cannot create starting epoch 0 for {ledgerType}. Database connection error?");
                }

                Result<Block> firstBlock;
                if (startAtEpochNumber != 0)
                {
                    firstBlock = await mediator.Send(new GetPostgresFirstBlockOfEpochRequest(startAtEpochNumber));
                }
                else
                {
                    firstBlock = await mediator.Send(new GetPostgresBlockByBlockNoRequest(1));
                }

                if (firstBlock.IsFailed)
                {
                    return Result.Fail($"First block could not be found on dbSyncs postgresdatabase for {ledgerType}. Database connection error?: {firstBlock.Errors.First().Message}");
                }

                var firstBlockResult = await mediator.Send(new CreateBlockRequest(
                    ledgerType: ledgerType,
                    blockHeight: firstBlock.Value.block_no,
                    blockHash: Hash.CreateFrom(firstBlock.Value.hash),
                    previousBlockHash: null,
                    previousBlockHeight: null,
                    epochNumber: firstBlock.Value.epoch_no,
                    timeUtc: firstBlock.Value.time,
                    txCount: firstBlock.Value.tx_count
                ));
                if (firstBlockResult.IsFailed)
                {
                    return Result.Fail($"Cannot create first block for {ledgerType}. Database connection error?");
                }
            }
        }

        // Gets the tip of the internal database of blocks
        var mostRecentBlockResult = await mediator.Send(new GetMostRecentBlockRequest(ledgerType));
        if (mostRecentBlockResult.IsFailed)
        {
            return Result.Fail(mostRecentBlockResult.Errors.First().Message);
        }

        // Checks if we have the tip of the postgrs-dbSync database also in our internal database
        // This does only check the longest chain. If a new fork is shorter than the longest chain, we'll still cling to the longest chain.
        // The check for forks is done in the ProcessBlockHandler
        var blockInDb = await mediator.Send(new GetBlockByBlockHeightRequest(ledgerType, postgresBlockTipResult.Value.block_no));
        if (blockInDb.IsSuccess)
        {
            logger.LogInformation($"{ledgerType} is already up to date on tip {postgresBlockTipResult.Value.block_no}");
            return Result.Ok();
        }

        var previousBlockHash = mostRecentBlockResult.Value.BlockHash;
        var previousBlockHeight = mostRecentBlockResult.Value.BlockHeight;
        // TODO ?var lastAnalyticsUpdate = DateTime.MinValue;
        for (int i = mostRecentBlockResult.Value.BlockHeight + 1; i <= postgresBlockTipResult.Value.block_no; i++)
        {
            if (i == 197355)
            {
                // This block is broken in the dbSync database
            }
            
#if !DEBUG
            if (i == 179966)
            {
                break;
            }
#endif

            var getBlockByIdResult = await mediator.Send(new GetPostgresBlockByBlockNoRequest(i));
            if (getBlockByIdResult.IsFailed)
            {
                logger.LogError($"Cannot read block from postgres (dbSync) for {ledgerType}: {getBlockByIdResult.Errors.First().Message}");
                return Result.Fail(getBlockByIdResult.Errors.First().Message);
            }

            var epochNumber = getBlockByIdResult.Value.epoch_no;
            var existigEpoch = await mediator.Send(new GetEpochRequest(ledgerType, epochNumber), CancellationToken.None);
            if (existigEpoch.IsFailed)
            {
                // we still need to create that epoch
                logger.LogInformation($"{ledgerType}: Creating new epoch #{epochNumber}");
                await mediator.Send(new CreateEpochRequest(ledgerType, epochNumber), CancellationToken.None);
            }

            //TODO fix the uglieness with the Hash-class?
            var processBlockResult = await mediator.Send(new ProcessBlockRequest(getBlockByIdResult.Value, previousBlockHash, previousBlockHeight, ledgerType));
            if (processBlockResult.IsFailed)
            {
                logger.LogError(processBlockResult.Errors.First().Message);
            }

            // TODO implement the notifications

            previousBlockHash = processBlockResult.Value.PreviousBlockHash;
            previousBlockHeight = processBlockResult.Value.PreviousBlockHeight;
        }

        //
        return Result.Ok();
    }
}