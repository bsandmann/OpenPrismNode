namespace OpenPrismNode.Sync.Services;

using Commands.GetPostgresBlockByBlockId;
using Commands.GetPostgresBlockByBlockNo;
using Commands.GetPostgresBlocksByBlockNos;
using Commands.GetPostgresBlockTip;
using Commands.GetPostgresFirstBlockOfEpoch;
using Commands.ProcessBlock;
using Core;
using Core.Commands.CreateBlock;
using Core.Commands.CreateBlocksAsBatch;
using Core.Commands.CreateEpoch;
using Core.Commands.CreateLedger;
using Core.Commands.GetBlockByBlockHash;
using Core.Commands.GetBlockByBlockHeight;
using Core.Commands.GetEpoch;
using Core.Commands.GetMostRecentBlock;
using Core.Commands.SwitchBranch;
using Core.DbSyncModels;
using Core.Entities;
using Core.Models;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;

public static class SyncService
{
    public static async Task<Result> RunSync(IMediator mediator, ILogger logger, string ledger, CancellationToken cancellationToken, int startAtEpochNumber = 0, bool isInitialStartup = false)
    {
        LedgerType ledgerType;
        if (ledger.Equals("mainnet", StringComparison.InvariantCultureIgnoreCase))
        {
            ledgerType = LedgerType.CardanoMainnet;
        }
        else if (ledger.Equals("preprod", StringComparison.InvariantCultureIgnoreCase))
        {
            ledgerType = LedgerType.CardanoPreprod;
        }
        else
        {
            return Result.Fail("Unknown ledger");
        }

        var postgresBlockTipResult = await mediator.Send(new GetPostgresBlockTipRequest(), cancellationToken);
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
                var ledgerResult = await mediator.Send(new CreateLedgerRequest(ledgerType), cancellationToken);
                if (ledgerResult.IsFailed)
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
                    firstBlock = await mediator.Send(new GetPostgresFirstBlockOfEpochRequest(startAtEpochNumber), cancellationToken);
                }
                else
                {
                    firstBlock = await mediator.Send(new GetPostgresBlockByBlockNoRequest(1), cancellationToken);
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
                ), cancellationToken);
                if (firstBlockResult.IsFailed)
                {
                    return Result.Fail($"Cannot create first block for {ledgerType}. Database connection error?");
                }
            }
        }

        // Gets the tip of the internal database of blocks
        var mostRecentBlockResult = await mediator.Send(new GetMostRecentBlockRequest(ledgerType), cancellationToken);
        if (mostRecentBlockResult.IsFailed)
        {
            return Result.Fail(mostRecentBlockResult.Errors.First().Message);
        }

        // Checks if we have the tip of the postgrs-dbSync database also in our internal database
        // This does only check the longest chain. If a new fork is shorter than the longest chain, we'll still cling to the longest chain.
        // The check for forks is done in the ProcessBlockHandler
        var prefix = BlockEntity.CalculateBlockHashPrefix(postgresBlockTipResult.Value.hash) ?? 0;
        var blockInDb = await mediator.Send(new GetBlockByBlockHashRequest(postgresBlockTipResult.Value.block_no, prefix, ledgerType), cancellationToken);
        if (blockInDb.IsSuccess)
        {
            logger.LogInformation($"{ledgerType} is already up to date on tip {postgresBlockTipResult.Value.block_no}");
            return Result.Ok();
        }

        if (postgresBlockTipResult.Value.block_no < mostRecentBlockResult.Value.BlockHeight)
        {
            // Handle the fork without switching branches
            logger.LogWarning($"Fork detected in {ledgerType}. Postgres tip: {postgresBlockTipResult.Value.block_no}, prism tip: {mostRecentBlockResult.Value.BlockHeight}");
            return await HandleFork(mediator, cancellationToken, postgresBlockTipResult, ledgerType);
        }
        else if (postgresBlockTipResult.Value.block_no == mostRecentBlockResult.Value.BlockHeight)
        {
            // Handle the fork and switch to the new branch
            logger.LogWarning($"Fork detected in {ledgerType}. Postgres tip: {postgresBlockTipResult.Value.block_no}, prism tip: {mostRecentBlockResult.Value.BlockHeight}");
            return await HandleFork(mediator, cancellationToken, postgresBlockTipResult, ledgerType, true);
        }

        var previousBlockHash = mostRecentBlockResult.Value.BlockHash;
        var previousBlockHeight = mostRecentBlockResult.Value.BlockHeight;
        var lastEpochInDatabase = mostRecentBlockResult.Value.EpochNumber;

        var startingBlock = mostRecentBlockResult.Value.BlockHeight + 1;
        // Normal Sync-Path
        for (int i = startingBlock; i <= postgresBlockTipResult.Value.block_no; i++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Result.Fail("Sync operation was cancelled");
            }

            if (postgresBlockTipResult.Value.block_no - i - 1 > PrismParameters.FastSyncBlockDistanceRequirement)
            {
                // Fast Sync-Path
                // We are at least 150 blocks behind (THe requirement is set in the PrismParameters)
                // We find the next block with the PRISM metadata
                var getNextBlockWithPrismMetadataResult = await mediator.Send(new GetNextBlockWithPrismMetadataRequest(i, PrismParameters.MetadataKey, postgresBlockTipResult.Value.block_no, ledgerType), cancellationToken);
                if (getNextBlockWithPrismMetadataResult.IsFailed)
                {
                    return Result.Fail(getNextBlockWithPrismMetadataResult.Errors.First().Message);
                }

                if (getNextBlockWithPrismMetadataResult.Value.BlockHeight is null && getNextBlockWithPrismMetadataResult.Value.EpochNumber is null)
                {
                    // No new PRISM block in front of the current block.
                    // We can fast-sync to the tip
                    var fastSyncResult = await FastSyncTo(mediator, ledgerType, i, postgresBlockTipResult.Value.block_no - 1, lastEpochInDatabase);
                    if (fastSyncResult.IsFailed)
                    {
                        logger.LogError(fastSyncResult.Errors.First().Message);
                        return fastSyncResult.ToResult();
                    }

                    i = postgresBlockTipResult.Value.block_no;
                    previousBlockHash = fastSyncResult.Value.Value;
                    previousBlockHeight = postgresBlockTipResult.Value.block_no - 1;
                }
                else
                {
                    // There is a PRISM block somewhere in front of the current block.
                    var distanceToNextPrismBlock = getNextBlockWithPrismMetadataResult.Value.BlockHeight!.Value - i;

                    if (distanceToNextPrismBlock > 0)
                    {
                        var fastSyncResult = await FastSyncTo(mediator, ledgerType, i, getNextBlockWithPrismMetadataResult.Value.BlockHeight!.Value - 1, lastEpochInDatabase);
                        if (fastSyncResult.IsFailed)
                        {
                            logger.LogError(fastSyncResult.Errors.First().Message);
                            return fastSyncResult.ToResult();
                        }

                        i = getNextBlockWithPrismMetadataResult.Value.BlockHeight!.Value;
                        previousBlockHash = fastSyncResult.Value.Value;
                        previousBlockHeight = getNextBlockWithPrismMetadataResult.Value.BlockHeight.Value - 1;
                    }
                    else
                    {
                        var previousBlock = await mediator.Send(new GetBlockByBlockHeightRequest(ledgerType, i - 1), cancellationToken);
                        if (previousBlock.IsFailed)
                        {
                            return Result.Fail($"Cannot find previous block for block {i} in {ledgerType}");
                        }

                        previousBlockHash = previousBlock.Value.BlockHash;
                        previousBlockHeight = previousBlock.Value.BlockHeight;
                    }
                }
            }

            var getBlockByIdResult = await mediator.Send(new GetPostgresBlockByBlockNoRequest(i), cancellationToken);
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

            lastEpochInDatabase = epochNumber;

            if (getBlockByIdResult.Value.previousHash is not null && !getBlockByIdResult.Value.previousHash.SequenceEqual(previousBlockHash))
            {
                // The previous-hash of the new block we want to add to the database is not identical to the hash of the previous block in the database
                // This means, the new block is not a direct successor of the previous block in the database and therefor part of a fork
                var r = await HandleFork(mediator, cancellationToken, getBlockByIdResult, ledgerType, true);
            }

            var processBlockResult = await mediator.Send(new ProcessBlockRequest(getBlockByIdResult.Value, previousBlockHash, previousBlockHeight, ledgerType), cancellationToken);
            if (processBlockResult.IsFailed)
            {
                logger.LogError(processBlockResult.Errors.First().Message);
            }

            // TODO implement the notifications
            previousBlockHash = processBlockResult.Value.PreviousBlockHash;
            previousBlockHeight = processBlockResult.Value.PreviousBlockHeight;
        }

        return Result.Ok();
    }

    private static async Task<Result> HandleFork(IMediator mediator, CancellationToken cancellationToken, Result<Block> postgresBlockTipResult, LedgerType ledgerType, bool switchBranch = false)
    {
        Block currentBlock = postgresBlockTipResult.Value;
        List<Block> blocksToCreate = new List<Block>();
        var baseBlockHeight = 0;
        var baseBlockPrefix = 0;
        var newTipBlockHeight = 0;
        var newTipBlockPrefix = 0;


        while (true)
        {
            var prefixCurrentBlock = BlockEntity.CalculateBlockHashPrefix(currentBlock.hash) ?? 0;
            var blockInDatabase = await mediator.Send(new GetBlockByBlockHashRequest(currentBlock.block_no, prefixCurrentBlock, ledgerType), cancellationToken);

            if (blockInDatabase.IsSuccess)
            {
                // We found the point where the fork starts
                baseBlockHeight = blockInDatabase.Value.BlockHeight;
                baseBlockPrefix = blockInDatabase.Value.BlockHashPrefix;
                break;
            }

            blocksToCreate.Add(currentBlock);

            // Get the previous block
            var priorBlock = await mediator.Send(new GetPostgresBlockByBlockIdRequest(currentBlock.previous_id), cancellationToken);
            if (priorBlock.IsFailed)
            {
                return Result.Fail($"Cannot find prior block for forked block {currentBlock.block_no} in {ledgerType}");
            }

            currentBlock = priorBlock.Value;
        }

        // Now create all the forked blocks, starting from the earliest
        for (int i = blocksToCreate.Count - 1; i >= 0; i--)
        {
            var blockToCreate = blocksToCreate[i];
            var previousBlock = i == blocksToCreate.Count - 1
                ? await mediator.Send(new GetBlockByBlockHashRequest(currentBlock.block_no, BlockEntity.CalculateBlockHashPrefix(currentBlock.hash) ?? 0, ledgerType), cancellationToken)
                : await mediator.Send(new GetBlockByBlockHashRequest(blocksToCreate[i + 1].block_no, BlockEntity.CalculateBlockHashPrefix(blocksToCreate[i + 1].hash) ?? 0, ledgerType), cancellationToken);

            if (previousBlock.IsFailed)
            {
                return Result.Fail($"Cannot find previous block for forked block {blockToCreate.block_no} in {ledgerType}");
            }

            var createBlockResult = await mediator.Send(new CreateBlockRequest(
                ledgerType: ledgerType,
                blockHeight: blockToCreate.block_no,
                blockHash: Hash.CreateFrom(blockToCreate.hash),
                previousBlockHash: Hash.CreateFrom(previousBlock.Value.BlockHash),
                previousBlockHeight: previousBlock.Value.BlockHeight,
                epochNumber: blockToCreate.epoch_no,
                timeUtc: blockToCreate.time,
                txCount: blockToCreate.tx_count,
                isFork: true
            ), cancellationToken);

            if (createBlockResult.IsFailed)
            {
                return Result.Fail($"Unable to create forked block in database {ledgerType} for block # {blockToCreate.block_no}: {createBlockResult.Errors.First().Message}");
            }

            if (i == 0)
            {
                newTipBlockHeight = createBlockResult.Value.BlockHeight;
                newTipBlockPrefix = createBlockResult.Value.BlockHashPrefix;
            }
        }

        if (switchBranch)
        {
            var switchBranchResult = await mediator.Send(new SwitchBranchRequest(ledgerType, baseBlockHeight, baseBlockPrefix, newTipBlockHeight, newTipBlockPrefix), cancellationToken);
            if (switchBranchResult.IsFailed)
            {
                return switchBranchResult;
            }
        }

        return Result.Ok();
    }

    private static async Task<Result<Hash>> FastSyncTo(IMediator mediator, LedgerType ledgerType, int firstBlockToRetrieve, int lastBlockToRetrieve, int lastEpochInDatabase)
    {
        const int chunkSize = 1000;
        var currentBlockStart = firstBlockToRetrieve;
        Hash? lastProcessedBlockHash = null;

        while (currentBlockStart <= lastBlockToRetrieve)
        {
            var currentChunkEnd = Math.Min(currentBlockStart + chunkSize - 1, lastBlockToRetrieve);

            var blocksFromPostgresResult = await mediator.Send(new GetPostgresBlocksByBlockNosRequest(currentBlockStart, currentChunkEnd - currentBlockStart + 1));
            if (blocksFromPostgresResult.IsFailed)
            {
                return Result.Fail($"Unable to retrieve blocks {currentBlockStart} to {currentChunkEnd} from dbSync postgres database: {blocksFromPostgresResult.Errors.First().Message}");
            }

            var retrievedBlocks = blocksFromPostgresResult.Value;
            if (retrievedBlocks.Count != currentChunkEnd - currentBlockStart + 1)
            {
                return Result.Fail($"Not all blocks from {currentBlockStart} to {currentChunkEnd} could be retrieved from dbSync postgres database. Integrity error");
            }

            if (retrievedBlocks.First().block_no != currentBlockStart)
            {
                return Result.Fail($"First block retrieved (#{retrievedBlocks.First().block_no}) is not the expected block #{currentBlockStart}. Integrity error");
            }

            var highestEpochInChunk = retrievedBlocks.Max(b => b.epoch_no);
            if (highestEpochInChunk > lastEpochInDatabase)
            {
                for (int i = lastEpochInDatabase + 1; i <= highestEpochInChunk; i++)
                {
                    var createEpochResult = await mediator.Send(new CreateEpochRequest(ledgerType, i));
                    if (createEpochResult.IsFailed)
                    {
                        return createEpochResult.ToResult();
                    }
                }

                lastEpochInDatabase = highestEpochInChunk;
            }

            var createBlocksAsBatchResult = await mediator.Send(new CreateBlocksAsBatchRequest(ledgerType, retrievedBlocks));
            if (createBlocksAsBatchResult.IsFailed)
            {
                return createBlocksAsBatchResult;
            }

            lastProcessedBlockHash = createBlocksAsBatchResult.Value;
            currentBlockStart = currentChunkEnd + 1;
        }

        return Result.Ok(lastProcessedBlockHash!);
    }
}