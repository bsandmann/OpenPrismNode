namespace OpenPrismNode.Sync.Services;

using Abstractions;
using Commands.ProcessBlock;
using Commands.SwitchBranch;
using Core;
using Core.Commands.CreateBlock;
using Core.Commands.CreateBlocksAsBatch;
using Core.Commands.CreateEpoch;
using Core.Commands.CreateLedger;
using Core.Commands.GetBlockByBlockHash;
using Core.Commands.GetBlockByBlockHeight;
using Core.Commands.GetEpoch;
using Core.Commands.GetMostRecentBlock;
using Core.Common;
using Core.DbSyncModels;
using Core.Entities;
using Core.Models;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;

public static class SyncService
{
    // Keep backward compatibility with existing tests
    public static async Task<Result> RunSync(IMediator mediator, AppSettings appsettings, ILogger logger, string ledger, CancellationToken cancellationToken, int startAtEpochNumber = 0, bool isInitialStartup = false)
    {
        // This implementation is only for backward compatibility with tests
        // In production code, always use the version with BlockProvider and TransactionProvider parameters

        logger.LogWarning("Using deprecated RunSync method without BlockProvider and TransactionProvider. This should only be used in tests.");

        // Use direct requests in the backward compatibility version
        // This ensures tests continue to work
        return Result.Fail("This legacy method should only be used in tests. Please update your code to use the version with BlockProvider and TransactionProvider parameters.");
    }

    // This is the actual implementation that should be used in production code
    public static async Task<Result> RunSync(
        IMediator mediator,
        AppSettings appsettings,
        ILogger logger,
        string ledger,
        CancellationToken cancellationToken,
        IBlockProvider blockProvider,
        ITransactionProvider transactionProvider,
        int startAtEpochNumber = 0,
        bool isInitialStartup = false)
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

        var blockTipResult = await blockProvider.GetBlockTip(cancellationToken);
        if (blockTipResult.IsFailed)
        {
            return Result.Fail(blockTipResult.Errors.First().Message);
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
                    firstBlock = await blockProvider.GetFirstBlockOfEpoch(startAtEpochNumber, cancellationToken);
                }
                else
                {
                    firstBlock = await blockProvider.GetBlockByNumber(1, cancellationToken);
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

        // Checks if we have the tip of the blockchain data also in our internal database
        // This does only check the longest chain. If a new fork is shorter than the longest chain, we'll still cling to the longest chain.
        // The check for forks is done in the ProcessBlockHandler
        var prefix = BlockEntity.CalculateBlockHashPrefix(blockTipResult.Value.hash) ?? 0;
        var blockInDb = await mediator.Send(new GetBlockByBlockHashRequest(blockTipResult.Value.block_no, prefix, ledgerType), cancellationToken);
        if (blockInDb.IsSuccess)
        {
            logger.LogInformation($"{ledgerType} is already up to date on tip {blockTipResult.Value.block_no}");
            return Result.Ok();
        }

        if (blockTipResult.Value.block_no < mostRecentBlockResult.Value.BlockHeight)
        {
            // Handle the fork without switching branches
            logger.LogWarning($"Fork detected (1) in {ledgerType}. Blockchain tip: {blockTipResult.Value.block_no}, prism tip: {mostRecentBlockResult.Value.BlockHeight}");
            return await HandleFork(mediator, cancellationToken, blockTipResult, ledgerType, blockProvider, transactionProvider);
        }
        else if (blockTipResult.Value.block_no == mostRecentBlockResult.Value.BlockHeight)
        {
            // Handle the fork and switch to the new branch
            logger.LogWarning($"Fork detected (2) in {ledgerType}. Blockchain tip: {blockTipResult.Value.block_no}, prism tip: {mostRecentBlockResult.Value.BlockHeight}");
            return await HandleFork(mediator, cancellationToken, blockTipResult, ledgerType, blockProvider, transactionProvider, true);
        }

        var previousBlockHash = mostRecentBlockResult.Value.BlockHash;
        var previousBlockHeight = mostRecentBlockResult.Value.BlockHeight;
        var lastEpochInDatabase = mostRecentBlockResult.Value.EpochNumber;

        var startingBlock = mostRecentBlockResult.Value.BlockHeight + 1;
        // Normal Sync-Path
        for (int i = startingBlock; i <= blockTipResult.Value.block_no; i++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Result.Fail("Sync operation was cancelled");
            }

            if (blockTipResult.Value.block_no - i - 1 > appsettings.FastSyncBlockDistanceRequirement)
            {
                // Fast Sync-Path
                // We are at least 150 blocks behind (The requirement is set in the PrismParameters)
                // We find the next block with the PRISM metadata
                // var getNextBlockWithPrismMetadataResult = await mediator.Send(new GetNextBlockWithPrismMetadataRequest(i, appsettings.MetadataKey, postgresBlockTipResult.Value.block_no, ledgerType), cancellationToken);

                var getNextBlockWithPrismMetadataResult = await blockProvider.GetNextBlockWithPrismMetadata(i, blockTipResult.Value.block_no, ledgerType, appsettings.MetadataKey, cancellationToken);
                if (getNextBlockWithPrismMetadataResult.IsFailed)
                {
                    return Result.Fail(getNextBlockWithPrismMetadataResult.Errors.First().Message);
                }

                // We need to modify this since the response structure of GetNextBlockWithPrismMetadata
                // may be different when coming from the API vs DB
                var nextBlockNo = getNextBlockWithPrismMetadataResult.Value?.block_no;
                var nextEpochNo = getNextBlockWithPrismMetadataResult.Value?.epoch_no;

                if (nextBlockNo == null && nextEpochNo == null)
                {
                    // No new PRISM block in front of the current block.
                    // We can fast-sync to the tip
                    var fastSyncResult = await FastSyncTo(
                        mediator,
                        appsettings,
                        ledgerType,
                        i,
                        blockTipResult.Value.block_no - 1,
                        lastEpochInDatabase,
                        blockProvider,
                        transactionProvider,
                        cancellationToken);

                    if (fastSyncResult.IsFailed)
                    {
                        logger.LogError(fastSyncResult.Errors.First().Message);
                        return fastSyncResult.ToResult();
                    }

                    i = blockTipResult.Value.block_no;
                    previousBlockHash = fastSyncResult.Value.Value;
                    previousBlockHeight = blockTipResult.Value.block_no - 1;
                }
                else
                {
                    // There is a PRISM block somewhere in front of the current block.
                    var distanceToNextPrismBlock = nextBlockNo - i;

                    if (distanceToNextPrismBlock > 0)
                    {
                        var fastSyncResult = await FastSyncTo(
                            mediator,
                            appsettings,
                            ledgerType,
                            i,
                            nextBlockNo.Value - 1,
                            lastEpochInDatabase,
                            blockProvider,
                            transactionProvider,
                            cancellationToken);

                        if (fastSyncResult.IsFailed)
                        {
                            logger.LogError(fastSyncResult.Errors.First().Message);
                            return fastSyncResult.ToResult();
                        }

                        i = nextBlockNo.Value;
                        previousBlockHash = fastSyncResult.Value.Value;
                        previousBlockHeight = nextBlockNo.Value - 1;
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

            var getBlockByIdResult = await blockProvider.GetBlockByNumber(i, cancellationToken);
            if (getBlockByIdResult.IsFailed)
            {
                logger.LogError($"Cannot read block for {ledgerType}: {getBlockByIdResult.Errors.First().Message}");
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
                logger.LogWarning($"Fork detected (3) in {ledgerType}. Blockchain block: {blockTipResult.Value.block_no}, prism block: {getBlockByIdResult.Value.block_no}");
                var handleForkResult = await HandleFork(mediator, cancellationToken, getBlockByIdResult, ledgerType, blockProvider, transactionProvider, true);
                if (handleForkResult.IsFailed)
                {
                    return handleForkResult;
                }
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

    private static async Task<Result> HandleFork(
        IMediator mediator,
        CancellationToken cancellationToken,
        Result<Block> blockTipResult,
        LedgerType ledgerType,
        IBlockProvider blockProvider,
        ITransactionProvider transactionProvider,
        bool switchBranch = false)
    {
        Block currentBlock = blockTipResult.Value;
        List<Block> blocksToCreate = new List<Block>();
        var baseBlockHeight = 0;
        var baseBlockPrefix = 0;
        var newTipBlockHeight = 0;
        var newTipBlockPrefix = 0;


        while (true)
        {
            // Check for cancellation before each iteration of potentially infinite loop
            if (cancellationToken.IsCancellationRequested)
            {
                return Result.Fail("Handle fork operation was cancelled");
            }

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
            var priorBlock = await blockProvider.GetBlockById(currentBlock.previous_id, cancellationToken);
            if (priorBlock.IsFailed)
            {
                return Result.Fail($"Cannot find prior block for forked block {currentBlock.block_no} in {ledgerType}");
            }

            currentBlock = priorBlock.Value;
        }

        // Now create all the forked blocks, starting from the earliest
        for (int i = blocksToCreate.Count - 1; i >= 0; i--)
        {
            // Check for cancellation before processing each block
            if (cancellationToken.IsCancellationRequested)
            {
                return Result.Fail("Handle fork operation was cancelled during block creation");
            }

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

    private static async Task<Result<Hash>> FastSyncTo(
        IMediator mediator,
        AppSettings appSettings,
        LedgerType ledgerType,
        int firstBlockToRetrieve,
        int lastBlockToRetrieve,
        int lastEpochInDatabase,
        IBlockProvider blockProvider,
        ITransactionProvider transactionProvider,
        CancellationToken cancellationToken = default)
    {
        var currentBlockStart = firstBlockToRetrieve;
        Hash? lastProcessedBlockHash = null;

        while (currentBlockStart <= lastBlockToRetrieve)
        {
            // Check for cancellation before each iteration
            if (cancellationToken.IsCancellationRequested)
            {
                return Result.Fail("Fast sync operation was cancelled");
            }

            var currentChunkEnd = Math.Min(currentBlockStart + appSettings.FastSyncBatchSize - 1, lastBlockToRetrieve);

            // Get blocks by their numbers using the block provider
            var blocksResult = await blockProvider.GetBlocksByNumbers(currentBlockStart, currentChunkEnd - currentBlockStart + 1, cancellationToken);
            if (blocksResult.IsFailed)
            {
                return Result.Fail($"Unable to retrieve blocks {currentBlockStart} to {currentChunkEnd}: {blocksResult.Errors.First().Message}");
            }

            var retrievedBlocks = blocksResult.Value.ToList();
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
                    // Check for cancellation before creating epochs
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return Result.Fail("Fast sync operation was cancelled during epoch creation");
                    }

                    var createEpochResult = await mediator.Send(new CreateEpochRequest(ledgerType, i), cancellationToken);
                    if (createEpochResult.IsFailed)
                    {
                        return createEpochResult.ToResult();
                    }
                }

                lastEpochInDatabase = highestEpochInChunk;
            }

            var createBlocksAsBatchResult = await mediator.Send(new CreateBlocksAsBatchRequest(ledgerType, retrievedBlocks), cancellationToken);
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