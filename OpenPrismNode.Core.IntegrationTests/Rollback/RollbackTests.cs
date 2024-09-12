using FluentAssertions;
using FluentResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using OpenPrismNode.Core.Commands.CreateAddresses;
using OpenPrismNode.Core.Commands.CreateBlock;
using OpenPrismNode.Core.Commands.CreateStakeAddress;
using OpenPrismNode.Core.Commands.CreateTransaction;
using OpenPrismNode.Core.Commands.CreateTransactionCreateDid;
using OpenPrismNode.Core.Commands.CreateWalletAddress;
using OpenPrismNode.Core.Commands.GetBlockByBlockHash;
using OpenPrismNode.Core.Commands.GetEpoch;
using OpenPrismNode.Core.Commands.GetMostRecentBlock;
using OpenPrismNode.Core.Commands.SwitchBranch;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Core.DbSyncModels;
using OpenPrismNode.Core.Entities;
using OpenPrismNode.Core.Models;
using OpenPrismNode.Sync.Commands.DecodeTransaction;
using OpenPrismNode.Sync.Commands.GetMetadataFromTransaction;
using OpenPrismNode.Sync.Commands.GetPaymentDataFromTransaction;
using OpenPrismNode.Sync.Commands.GetPostgresBlockByBlockId;
using OpenPrismNode.Sync.Commands.GetPostgresBlockByBlockNo;
using OpenPrismNode.Sync.Commands.GetPostgresBlockTip;
using OpenPrismNode.Sync.Commands.GetTransactionsWithPrismMetadataForBlockId;
using OpenPrismNode.Sync.Commands.ParseTransaction;
using OpenPrismNode.Sync.Commands.ProcessBlock;
using OpenPrismNode.Sync.Commands.ProcessTransaction;
using OpenPrismNode.Sync.Services;

public partial class IntegrationTests
{
    [Fact]
    public async Task Normal_sync_proccesses_as_expected()
    {
        // Arrange block 10 as base
        var logger = new Mock<ILogger>();
        var ledgerType = LedgerType.CardanoPreprod;
        var epochNumber = 1;
        var blockHeight = 9;
        blockHeight++;
        var baseBlockHash = new byte[] { 10, 1, 1, 1 };
        await SetupLedgerEpochAndBlock(ledgerType, epochNumber, blockHeight, baseBlockHash, null, 9);

        // Setup the dbsync Block to be at block 12 to start syncing
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetPostgresBlockTipRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok(new Block()
        {
            block_no = 12,
            epoch_no = 1,
            hash = new byte[] { 12, 1, 1, 1 },
            time = DateTime.UtcNow,
            tx_count = 0,
            id = 120,
            previous_id = 110,
            previousHash = new byte[] { 11, 1, 1, 1 }
        }));

        // No Prism transactions in the blocks
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetTransactionsWithPrismMetadataForBlockIdRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new List<Transaction>()));

        _mediatorMock.Setup(p => p.Send(It.IsAny<GetMostRecentBlockRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (GetMostRecentBlockRequest request, CancellationToken token) => await this._getMostRecentBlockHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetBlockByBlockHashRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (GetBlockByBlockHashRequest request, CancellationToken token) => await this._getBlockByBlockHashHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetEpochRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (GetEpochRequest request, CancellationToken token) => await this._getEpochHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<ProcessBlockRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (ProcessBlockRequest request, CancellationToken token) => await this._processBlockHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<CreateBlockRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (CreateBlockRequest request, CancellationToken token) => await this._createBlockHandler.Handle(request, token));

        // Return block 11 from dbsync, and then block 12
        _mediatorMock.SetupSequence(p => p.Send(It.IsAny<GetPostgresBlockByBlockNoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 11,
                epoch_no = 1,
                hash = new byte[] { 11, 1, 1, 1 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 110,
                previous_id = 100,
                previousHash = new byte[] { 10, 1, 1, 1 }
            })).ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 12,
                epoch_no = 1,
                hash = new byte[] { 12, 1, 1, 1 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 120,
                previous_id = 110,
                previousHash = new byte[] { 11, 1, 1, 1 }
            }));

        // Act
        var syncResult = await SyncService.RunSync(_mediatorMock.Object, new AppSettings() { FastSyncBlockDistanceRequirement = 150 }, logger.Object, "preprod", new CancellationToken(), 10, false);

        // Assert
        syncResult.IsSuccess.Should().BeTrue();

        // Blocks 10, 11 and 12 should be in the database
        var blocks = await _context.BlockEntities.ToListAsync();
        blocks.Should().Contain(p => p.BlockHeight == 10);
        blocks.Should().Contain(p => p.BlockHeight == 11);
        blocks.Should().Contain(p => p.BlockHeight == 12);
    }

    [Fact]
    public async Task Normal_sync_proccesses_as_expected_with_already_existing_blocks_get_skipped()
    {
        // Arrange block 10 as base
        var logger = new Mock<ILogger>();
        var ledgerType = LedgerType.CardanoPreprod;
        var epochNumber = 1;
        var blockHeight = 9;
        blockHeight++;
        var baseBlockHash = new byte[] { 10, 1, 1, 1 };
        await SetupLedgerEpochAndBlock(ledgerType, epochNumber, blockHeight, baseBlockHash, null, 9);

        // Setup the dbsync Block to be at block 12 to start syncing
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetPostgresBlockTipRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok(new Block()
        {
            block_no = 12,
            epoch_no = 1,
            hash = new byte[] { 12, 1, 1, 1 },
            time = DateTime.UtcNow,
            tx_count = 0,
            id = 120,
            previous_id = 110
        }));

        // No Prism transactions in the blocks
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetTransactionsWithPrismMetadataForBlockIdRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new List<Transaction>()));

        _mediatorMock.Setup(p => p.Send(It.IsAny<GetMostRecentBlockRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (GetMostRecentBlockRequest request, CancellationToken token) => await this._getMostRecentBlockHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetBlockByBlockHashRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (GetBlockByBlockHashRequest request, CancellationToken token) => await this._getBlockByBlockHashHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetEpochRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (GetEpochRequest request, CancellationToken token) => await this._getEpochHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<ProcessBlockRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (ProcessBlockRequest request, CancellationToken token) => await this._processBlockHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<CreateBlockRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (CreateBlockRequest request, CancellationToken token) => await this._createBlockHandler.Handle(request, token));

        // Return block 11 from dbsync, and then block 10 and then 12
        _mediatorMock.SetupSequence(p => p.Send(It.IsAny<GetPostgresBlockByBlockNoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 11,
                epoch_no = 1,
                hash = new byte[] { 11, 1, 1, 1 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 110,
                previous_id = 100
            })).ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 12,
                epoch_no = 1,
                hash = new byte[] { 12, 1, 1, 1 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 120,
                previous_id = 110
            }));

        // Act
        var syncResult = await SyncService.RunSync(_mediatorMock.Object,new AppSettings() { FastSyncBlockDistanceRequirement = 150 }, logger.Object, "preprod", new CancellationToken(), 10, false);

        // Assert
        syncResult.IsSuccess.Should().BeTrue();

        // Blocks 10, 11 and 12 should be in the database
        var blocks = await _context.BlockEntities.ToListAsync();
        blocks.Should().Contain(p => p.BlockHeight == 10);
        blocks.Should().Contain(p => p.BlockHeight == 11);
        blocks.Should().Contain(p => p.BlockHeight == 12);


        // The goal of the test is to show if the syncing is run again, with an older block, it should skip the block
        // Return block 11 from dbsync as the tip
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetPostgresBlockTipRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok(new Block()
        {
            block_no = 11,
            epoch_no = 1,
            hash = new byte[] { 11, 1, 1, 1 },
            time = DateTime.UtcNow,
            tx_count = 0,
            id = 110,
            previous_id = 100
        }));

        _mediatorMock.SetupSequence(p => p.Send(It.IsAny<GetPostgresBlockByBlockNoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 11,
                epoch_no = 1,
                hash = new byte[] { 11, 1, 1, 1 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 110,
                previous_id = 100
            }));

        var syncResult2 = await SyncService.RunSync(_mediatorMock.Object,new AppSettings() { FastSyncBlockDistanceRequirement = 150 }, logger.Object, "preprod", new CancellationToken(), 10, false);

        // Assert
        syncResult2.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Unknown_tip_with_lower_blockHeight_gets_marked_as_fork()
    {
        // Arrange block 10 as base
        var logger = new Mock<ILogger>();
        var ledgerType = LedgerType.CardanoPreprod;
        var epochNumber = 1;
        var blockHeight = 9;
        blockHeight++;
        var baseBlockHash = new byte[] { 10, 1, 1, 1 };
        await SetupLedgerEpochAndBlock(ledgerType, epochNumber, blockHeight, baseBlockHash, null, 9);

        // Setup the dbsync Block to be at block 12 to start syncing
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetPostgresBlockTipRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok(new Block()
        {
            block_no = 12,
            epoch_no = 1,
            hash = new byte[] { 12, 1, 1, 1 },
            time = DateTime.UtcNow,
            tx_count = 0,
            previous_id = 110,
            id = 120
        }));

        // No Prism transactions in the blocks
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetTransactionsWithPrismMetadataForBlockIdRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new List<Transaction>()));

        _mediatorMock.Setup(p => p.Send(It.IsAny<GetMostRecentBlockRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (GetMostRecentBlockRequest request, CancellationToken token) => await this._getMostRecentBlockHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetBlockByBlockHashRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (GetBlockByBlockHashRequest request, CancellationToken token) => await this._getBlockByBlockHashHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetEpochRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (GetEpochRequest request, CancellationToken token) => await this._getEpochHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<ProcessBlockRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (ProcessBlockRequest request, CancellationToken token) => await this._processBlockHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<CreateBlockRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (CreateBlockRequest request, CancellationToken token) => await this._createBlockHandler.Handle(request, token));

        // Return block 11 from dbsync, and then 12
        _mediatorMock.SetupSequence(p => p.Send(It.IsAny<GetPostgresBlockByBlockNoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 11,
                epoch_no = 1,
                hash = new byte[] { 11, 1, 1, 1 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 110,
                previous_id = 100
            })).ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 12,
                epoch_no = 1,
                hash = new byte[] { 12, 1, 1, 1 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 120,
                previous_id = 110
            }));

        // Act 1
        var syncResult = await SyncService.RunSync(_mediatorMock.Object,new AppSettings() { FastSyncBlockDistanceRequirement = 150 }, logger.Object, "preprod", new CancellationToken(), 10, false);

        // Assert
        syncResult.IsSuccess.Should().BeTrue();

        // Blocks 10, 11 and 12 should be in the database
        var blocks = await _context.BlockEntities.ToListAsync();
        blocks.Should().Contain(p => p.BlockHeight == 10);
        blocks.Should().Contain(p => p.BlockHeight == 11);
        blocks.Should().Contain(p => p.BlockHeight == 12);


        // Now the fork comes into play. When rerunning the sync-process, the tip is at block 11, but with different hash 
        // It is referencing the same previousId (100) as the existing block 11 in the database
        // Return block 11 from dbsync as the tip
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetPostgresBlockTipRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok(new Block()
        {
            block_no = 11,
            epoch_no = 1,
            hash = new byte[] { 11, 11, 11, 11 },
            time = DateTime.UtcNow,
            tx_count = 0,
            id = 111,
            previous_id = 100
        }));

        // Get data for block 11
        _mediatorMock.SetupSequence(p => p.Send(It.IsAny<GetPostgresBlockByBlockNoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 11,
                epoch_no = 1,
                hash = new byte[] { 11, 11, 11, 11 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 111,
                previous_id = 100
            }));

        // Get block 10 from dbsync by its id
        _mediatorMock.SetupSequence(p => p.Send(It.IsAny<GetPostgresBlockByBlockIdRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 10,
                epoch_no = 1,
                hash = new byte[] { 10, 1, 1, 1 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 100,
                previous_id = 90
            }));

        var syncResult2 = await SyncService.RunSync(_mediatorMock.Object,new AppSettings() { FastSyncBlockDistanceRequirement = 150 }, logger.Object, "preprod", new CancellationToken(), 10, false);

        // Assert
        syncResult2.IsSuccess.Should().BeTrue();

        // We should also now have a forked block 11 in the database
        var blocksforked = await _context.BlockEntities.Where(p => p.IsFork).ToListAsync();
        blocksforked.Should().Contain(p => p.BlockHeight == 11);
    }

    [Fact]
    public async Task Unknown_tips_with_lower_blockHeight_gets_marked_as_forks()
    {
        // Arrange block 10 as base
        var logger = new Mock<ILogger>();
        var ledgerType = LedgerType.CardanoPreprod;
        var epochNumber = 1;
        var blockHeight = 9;
        blockHeight++;
        var baseBlockHash = new byte[] { 10, 1, 1, 1 };
        await SetupLedgerEpochAndBlock(ledgerType, epochNumber, blockHeight, baseBlockHash, null, 9);

        // Setup the dbsync Block to be at block 12 to start syncing
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetPostgresBlockTipRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok(new Block()
        {
            block_no = 13,
            epoch_no = 1,
            hash = new byte[] { 13, 1, 1, 1 },
            time = DateTime.UtcNow,
            tx_count = 0,
            previous_id = 120,
            id = 130
        }));

        // No Prism transactions in the blocks
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetTransactionsWithPrismMetadataForBlockIdRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new List<Transaction>()));

        _mediatorMock.Setup(p => p.Send(It.IsAny<GetMostRecentBlockRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (GetMostRecentBlockRequest request, CancellationToken token) => await this._getMostRecentBlockHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetBlockByBlockHashRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (GetBlockByBlockHashRequest request, CancellationToken token) => await this._getBlockByBlockHashHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetEpochRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (GetEpochRequest request, CancellationToken token) => await this._getEpochHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<ProcessBlockRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (ProcessBlockRequest request, CancellationToken token) => await this._processBlockHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<CreateBlockRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (CreateBlockRequest request, CancellationToken token) => await this._createBlockHandler.Handle(request, token));

        // Return block 11 from dbsync, and then block 12 and then 13
        _mediatorMock.SetupSequence(p => p.Send(It.IsAny<GetPostgresBlockByBlockNoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 11,
                epoch_no = 1,
                hash = new byte[] { 11, 1, 1, 1 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 110,
                previous_id = 100
            })).ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 12,
                epoch_no = 1,
                hash = new byte[] { 12, 1, 1, 1 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 120,
                previous_id = 110
            })).ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 13,
                epoch_no = 1,
                hash = new byte[] { 13, 1, 1, 1 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 130,
                previous_id = 120
            }));

        // Act 1
        var syncResult = await SyncService.RunSync(_mediatorMock.Object,new AppSettings() { FastSyncBlockDistanceRequirement = 150 }, logger.Object, "preprod", new CancellationToken(), 10, false);

        // Assert
        syncResult.IsSuccess.Should().BeTrue();

        // Blocks 10, 11, 12 and 13 should be in the database
        var blocks = await _context.BlockEntities.ToListAsync();
        blocks.Should().Contain(p => p.BlockHeight == 10);
        blocks.Should().Contain(p => p.BlockHeight == 11);
        blocks.Should().Contain(p => p.BlockHeight == 12);
        blocks.Should().Contain(p => p.BlockHeight == 13);

        // Now the fork comes into play. When rerunning the sync-process, the tip is at block 12, but with different hash 
        // It is also not referencing the same previousId (110) as the existing block 12 in the database, instead it is referencing another forked block (111)
        // Return block 12 from dbsync as the tip
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetPostgresBlockTipRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok(new Block()
        {
            block_no = 12,
            epoch_no = 1,
            hash = new byte[] { 12, 12, 12, 12 },
            time = DateTime.UtcNow,
            tx_count = 0,
            id = 121,
            previous_id = 111
        }));

        // Get data for block 12
        _mediatorMock.SetupSequence(p => p.Send(It.IsAny<GetPostgresBlockByBlockNoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 12,
                epoch_no = 1,
                hash = new byte[] { 12, 12, 12, 12 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 121,
                previous_id = 111
            })).ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 11,
                epoch_no = 1,
                hash = new byte[] { 11, 11, 11, 11 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 111,
                previous_id = 100
            }));

        // Get block 11 from dbsync by its id
        _mediatorMock.SetupSequence(p => p.Send(It.IsAny<GetPostgresBlockByBlockIdRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 11,
                epoch_no = 1,
                hash = new byte[] { 11, 11, 11, 11 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 111,
                previous_id = 100
            })).ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 10,
                epoch_no = 1,
                hash = new byte[] { 10, 1, 1, 1 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 100,
                previous_id = 90
            }));

        var syncResult2 = await SyncService.RunSync(_mediatorMock.Object,new AppSettings() { FastSyncBlockDistanceRequirement = 150 }, logger.Object, "preprod", new CancellationToken(), 10, false);

        // Assert
        syncResult2.IsSuccess.Should().BeTrue();

        // We should also now have a forked block 11 and 12 in the database
        var blocksforked = await _context.BlockEntities.Where(p => p.IsFork).ToListAsync();
        blocksforked.Should().Contain(p => p.BlockHeight == 11);
        blocksforked.Should().Contain(p => p.BlockHeight == 12);
    }

    [Fact]
    public async Task Unknown_tip_with_identical_blockHeight_switches_fork()
    {
        // Arrange block 10 as base
        var logger = new Mock<ILogger>();
        var ledgerType = LedgerType.CardanoPreprod;
        var epochNumber = 1;
        var blockHeight = 9;
        blockHeight++;
        var baseBlockHash = new byte[] { 10, 1, 1, 1 };
        await SetupLedgerEpochAndBlock(ledgerType, epochNumber, blockHeight, baseBlockHash, null, 9);

        // Setup the dbsync Block to be at block 11 to start syncing
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetPostgresBlockTipRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok(new Block()
        {
            block_no = 11,
            epoch_no = 1,
            hash = new byte[] { 11, 1, 1, 1 },
            time = DateTime.UtcNow,
            tx_count = 0,
            previous_id = 100,
            id = 110
        }));

        // No Prism transactions in the blocks
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetTransactionsWithPrismMetadataForBlockIdRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new List<Transaction>()));

        _mediatorMock.Setup(p => p.Send(It.IsAny<GetMostRecentBlockRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (GetMostRecentBlockRequest request, CancellationToken token) => await this._getMostRecentBlockHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetBlockByBlockHashRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (GetBlockByBlockHashRequest request, CancellationToken token) => await this._getBlockByBlockHashHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetEpochRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (GetEpochRequest request, CancellationToken token) => await this._getEpochHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<ProcessBlockRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (ProcessBlockRequest request, CancellationToken token) => await this._processBlockHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<CreateBlockRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (CreateBlockRequest request, CancellationToken token) => await this._createBlockHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<SwitchBranchRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (SwitchBranchRequest request, CancellationToken token) => await this._switchBranchHandler.Handle(request, token));

        // Return block 11 from dbsync 
        _mediatorMock.SetupSequence(p => p.Send(It.IsAny<GetPostgresBlockByBlockNoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 11,
                epoch_no = 1,
                hash = new byte[] { 11, 1, 1, 1 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 110,
                previous_id = 100
            }));

        // Act 1
        var syncResult = await SyncService.RunSync(_mediatorMock.Object,new AppSettings() { FastSyncBlockDistanceRequirement = 150 }, logger.Object, "preprod", new CancellationToken(), 10, false);

        // Assert
        syncResult.IsSuccess.Should().BeTrue();

        // Blocks 10, 11 should be in the database
        var blocks = await _context.BlockEntities.ToListAsync();
        blocks.Should().Contain(p => p.BlockHeight == 10);
        blocks.Should().Contain(p => p.BlockHeight == 11);


        // Now the fork comes into play. When rerunning the sync-process, the tip is at block 11, but with different hash 
        // It is referencing the same previousId (100) as the existing block 11 in the database
        // Return block 11 from dbsync as the tip
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetPostgresBlockTipRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok(new Block()
        {
            block_no = 11,
            epoch_no = 1,
            hash = new byte[] { 11, 11, 11, 11 },
            time = DateTime.UtcNow,
            tx_count = 0,
            id = 111,
            previous_id = 100
        }));

        // Get data for block 11
        _mediatorMock.SetupSequence(p => p.Send(It.IsAny<GetPostgresBlockByBlockNoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 11,
                epoch_no = 1,
                hash = new byte[] { 11, 11, 11, 11 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 111,
                previous_id = 100
            }));

        // Get block 10 from dbsync by its id
        _mediatorMock.SetupSequence(p => p.Send(It.IsAny<GetPostgresBlockByBlockIdRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 10,
                epoch_no = 1,
                hash = new byte[] { 10, 1, 1, 1 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 100,
                previous_id = 90
            }));

        var syncResult2 = await SyncService.RunSync(_mediatorMock.Object,new AppSettings() { FastSyncBlockDistanceRequirement = 150 }, logger.Object, "preprod", new CancellationToken(), 10, false);

        // Assert
        syncResult2.IsSuccess.Should().BeTrue();

        // We should also now have a forked block 11 in the database
        // But the block with the fork-flag is now the inital block 11 
        // The branch as basically switched
        var blocksforked = await _context.BlockEntities.Where(p => p.IsFork).ToListAsync();
        blocksforked.Count.Should().Be(1);
        blocksforked.Single().BlockHeight.Should().Be(11);
        blocksforked.Single().BlockHashPrefix.Should().Be(BlockEntity.CalculateBlockHashPrefix(new byte[] { 11, 1, 1, 1 }));
    }

    [Fact]
    public async Task Unknown_tip_with_identical_blockHeight_switches_fork_with_PRISM_transactions()
    {
        // Arrange block 10 as base
        var logger = new Mock<ILogger>();
        var ledgerType = LedgerType.CardanoPreprod;
        var epochNumber = 1;
        var blockHeight = 9;
        blockHeight++;
        var baseBlockHash = new byte[] { 10, 1, 1, 1 };
        await SetupLedgerEpochAndBlock(ledgerType, epochNumber, blockHeight, baseBlockHash, null, 9);

        // Setup the dbsync Block to be at block 11 to start syncing
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetPostgresBlockTipRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok(new Block()
        {
            block_no = 11,
            epoch_no = 1,
            hash = new byte[] { 11, 1, 1, 1 },
            time = DateTime.UtcNow,
            tx_count = 0,
            previous_id = 100,
            id = 110
        }));

        // Single PRISM transaction
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetTransactionsWithPrismMetadataForBlockIdRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new List<Transaction>()
            {
                new Transaction()
                {
                    block_index = 7,
                    fee = 34.34m,
                    hash = new byte[] { 1, 1, 2, 2 },
                    id = 8,
                    size = 123
                }
            }));
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetMetadataFromTransactionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new Metadata()
            {
                id = 9,
                bytes = new byte[] { 2, 2, 3, 3 },
                json = """
                       {
                       "c":[
                       "0x22c60212c3020a076d61737465723012463044022007a66810de54b8362297afeffd6274f13576dba2385ac912dbda53bab13bb98102201179b6682c10d0e5e7",
                       "0xe1c752e321b5d401b0cd23c68240128be7f2faed7228491aef010aec010ae90112390a056b65792d3110044a2e0a09736563703235366b31122102447a1746cc",
                       "0xc1032b8471f3906b9b1ab507aa6ec6d2f25903a82ccce3d98a275112390a056b65792d3210024a2e0a09736563703235366b31122103b07d9cade268680a502e",
                       "0x3d20aacb79bd283ab2cf8b5703d3cc5e7be9626bcb7b123b0a076d61737465723010014a2e0a09736563703235366b31122102888a4ccec8d6309d3c2e4adcb0",
                       "0x46b7808608660c69340968a927013aa7b2002c1a340a09736572766963652d31120d4c696e6b6564446f6d61696e731a185b2268747470733a2f2f6d342e6373",
                       "0x69676e2e696f2f225d"
                       ],
                       "v":1
                       }
                       """,
                key = 21235,
                tx_id = 8
            }));
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetPaymentDataFromTransactionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new Payment()
            {
                Incoming = new List<Utxo>()
                {
                    new Utxo()
                    {
                        Index = 0,
                        WalletAddress = new WalletAddress()
                        {
                            StakeAddressString = null,
                            WalletAddressString = "someWalletAddress1"
                        },
                        Value = 123
                    }
                },
                Outgoing = new List<Utxo>()
                {
                    new Utxo()
                    {
                        Index = 1,
                        WalletAddress = new WalletAddress()
                        {
                            StakeAddressString = "someStakeAddress",
                            WalletAddressString = "someWalletAddress2"
                        },
                        Value = 456
                    }
                }
            }));

        _mediatorMock.Setup(p => p.Send(It.IsAny<DecodeTransactionRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (DecodeTransactionRequest request, CancellationToken token) => await this._decodeTransactionHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<ProcessTransactionRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (ProcessTransactionRequest request, CancellationToken token) => await this._processTransactionHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetMostRecentBlockRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (GetMostRecentBlockRequest request, CancellationToken token) => await this._getMostRecentBlockHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetBlockByBlockHashRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (GetBlockByBlockHashRequest request, CancellationToken token) => await this._getBlockByBlockHashHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetEpochRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (GetEpochRequest request, CancellationToken token) => await this._getEpochHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<ProcessBlockRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (ProcessBlockRequest request, CancellationToken token) => await this._processBlockHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<CreateBlockRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (CreateBlockRequest request, CancellationToken token) => await this._createBlockHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<SwitchBranchRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (SwitchBranchRequest request, CancellationToken token) => await this._switchBranchHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<CreateAddressesRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (CreateAddressesRequest request, CancellationToken token) => await this._createAddressesHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<CreateWalletAddressRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (CreateWalletAddressRequest request, CancellationToken token) => await this._createWalletAddressHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<CreateStakeAddressRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (CreateStakeAddressRequest request, CancellationToken token) => await this._createStakeAddressHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<ParseTransactionRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (ParseTransactionRequest request, CancellationToken token) => await this._parseTransactionHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<CreateTransactionRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (CreateTransactionRequest request, CancellationToken token) => await this._createTransactionHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<CreateTransactionCreateDidRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (CreateTransactionCreateDidRequest request, CancellationToken token) => await this._createTransactionCreateDidHandler.Handle(request, token));

        // Return block 11 from dbsync 
        _mediatorMock.SetupSequence(p => p.Send(It.IsAny<GetPostgresBlockByBlockNoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 11,
                epoch_no = 1,
                hash = new byte[] { 11, 1, 1, 1 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 110,
                previous_id = 100
            }));

        // Act 1
        var syncResult = await SyncService.RunSync(_mediatorMock.Object,new AppSettings() { FastSyncBlockDistanceRequirement = 150 }, logger.Object, "preprod", new CancellationToken(), 10, false);

        // Assert
        syncResult.IsSuccess.Should().BeTrue();

        // Blocks 10, 11 should be in the database
        var blocks = await _context.BlockEntities.ToListAsync();
        blocks.Should().Contain(p => p.BlockHeight == 10);
        blocks.Should().Contain(p => p.BlockHeight == 11);


        // Now the fork comes into play. When rerunning the sync-process, the tip is at block 11, but with different hash 
        // It is referencing the same previousId (100) as the existing block 11 in the database
        // Return block 11 from dbsync as the tip
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetPostgresBlockTipRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok(new Block()
        {
            block_no = 11,
            epoch_no = 1,
            hash = new byte[] { 11, 11, 11, 11 },
            time = DateTime.UtcNow,
            tx_count = 0,
            id = 111,
            previous_id = 100
        }));

        // Get data for block 11
        _mediatorMock.SetupSequence(p => p.Send(It.IsAny<GetPostgresBlockByBlockNoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 11,
                epoch_no = 1,
                hash = new byte[] { 11, 11, 11, 11 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 111,
                previous_id = 100
            }));

        // Get block 10 from dbsync by its id
        _mediatorMock.SetupSequence(p => p.Send(It.IsAny<GetPostgresBlockByBlockIdRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 10,
                epoch_no = 1,
                hash = new byte[] { 10, 1, 1, 1 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 100,
                previous_id = 90
            }));

        var syncResult2 = await SyncService.RunSync(_mediatorMock.Object,new AppSettings() { FastSyncBlockDistanceRequirement = 150 }, logger.Object, "preprod", new CancellationToken(), 10, false);

        // Assert
        syncResult2.IsSuccess.Should().BeTrue();

        // We should also now have a forked block 11 in the database
        // But the block with the fork-flag is now the inital block 11 
        // The branch as basically switched
        var blocksforked = await _context.BlockEntities.Where(p => p.IsFork).ToListAsync();
        blocksforked.Count.Should().Be(1);
        blocksforked.Single().BlockHeight.Should().Be(11);
        blocksforked.Single().BlockHashPrefix.Should().Be(BlockEntity.CalculateBlockHashPrefix(new byte[] { 11, 1, 1, 1 }));

        // Initiall a CreateDID operations gets added to the database.
        // Thorugh the switching of the branch, the CreateDID operation is then removed.
        // but since the mock is setup in a way that it again returns the same block, the CreateDID operation is added again
        // It therefor should be in the database again
        var createDIDOperations = await _context.CreateDidEntities.FirstOrDefaultAsync();
        createDIDOperations.Should().NotBeNull();
    }

    [Fact]
    public async Task Unknown_tip_with_identical_blockHeight_switches_fork_even_for_longer_chains()
    {
        // Arrange block 10 as base
        var logger = new Mock<ILogger>();
        var ledgerType = LedgerType.CardanoPreprod;
        var epochNumber = 1;
        var blockHeight = 9;
        blockHeight++;
        var baseBlockHash = new byte[] { 10, 1, 1, 1 };
        await SetupLedgerEpochAndBlock(ledgerType, epochNumber, blockHeight, baseBlockHash, null, 9);

        // Setup the dbsync Block to be at block 12 to start syncing
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetPostgresBlockTipRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok(new Block()
        {
            block_no = 12,
            epoch_no = 1,
            hash = new byte[] { 12, 1, 1, 1 },
            time = DateTime.UtcNow,
            tx_count = 0,
            previous_id = 110,
            id = 120,
            previousHash = new byte[] { 11, 1, 1, 1 }
        }));

        // No Prism transactions in the blocks
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetTransactionsWithPrismMetadataForBlockIdRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new List<Transaction>()));

        _mediatorMock.Setup(p => p.Send(It.IsAny<GetMostRecentBlockRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (GetMostRecentBlockRequest request, CancellationToken token) => await this._getMostRecentBlockHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetBlockByBlockHashRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (GetBlockByBlockHashRequest request, CancellationToken token) => await this._getBlockByBlockHashHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetEpochRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (GetEpochRequest request, CancellationToken token) => await this._getEpochHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<ProcessBlockRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (ProcessBlockRequest request, CancellationToken token) => await this._processBlockHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<CreateBlockRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (CreateBlockRequest request, CancellationToken token) => await this._createBlockHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<SwitchBranchRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (SwitchBranchRequest request, CancellationToken token) => await this._switchBranchHandler.Handle(request, token));

        // Return block 11 from dbsync, and then block 12
        _mediatorMock.SetupSequence(p => p.Send(It.IsAny<GetPostgresBlockByBlockNoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 11,
                epoch_no = 1,
                hash = new byte[] { 11, 1, 1, 1 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 110,
                previous_id = 100,
                previousHash = new byte[] { 10, 1, 1, 1 }
            })).ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 12,
                epoch_no = 1,
                hash = new byte[] { 12, 1, 1, 1 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 120,
                previous_id = 110,
                previousHash = new byte[] { 11, 1, 1, 1 }
            }));

        // Act 1
        var syncResult = await SyncService.RunSync(_mediatorMock.Object,new AppSettings() { FastSyncBlockDistanceRequirement = 150 }, logger.Object, "preprod", new CancellationToken(), 10, false);

        // Assert
        syncResult.IsSuccess.Should().BeTrue();

        // Blocks 10, 11, and 12 should be in the database
        var blocks = await _context.BlockEntities.ToListAsync();
        blocks.Should().Contain(p => p.BlockHeight == 10);
        blocks.Should().Contain(p => p.BlockHeight == 11);
        blocks.Should().Contain(p => p.BlockHeight == 12);

        // Now the fork comes into play. When rerunning the sync-process, the tip is at block 12, but with different hash 
        // It is also not referencing the same previousId (110) as the existing block 12 in the database, instead it is referencing another forked block (111)
        // Return block 12 from dbsync as the tip
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetPostgresBlockTipRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok(new Block()
        {
            block_no = 12,
            epoch_no = 1,
            hash = new byte[] { 12, 12, 12, 12 },
            time = DateTime.UtcNow,
            tx_count = 0,
            id = 121,
            previous_id = 111,
            previousHash = new byte[] { 11, 11, 11, 11 }
        }));

        // Get data for block 12
        _mediatorMock.SetupSequence(p => p.Send(It.IsAny<GetPostgresBlockByBlockNoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 11,
                epoch_no = 1,
                hash = new byte[] { 11, 11, 11, 11 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 111,
                previous_id = 100,
                previousHash = new byte[] { 10, 1, 1, 1 }
            }))
            .ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 12,
                epoch_no = 1,
                hash = new byte[] { 12, 12, 12, 12 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 121,
                previous_id = 111,
                previousHash = new byte[] { 11, 11, 11, 11 }
            }));

        // Get block 11 from dbsync by its id
        _mediatorMock.SetupSequence(p => p.Send(It.IsAny<GetPostgresBlockByBlockIdRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 11,
                epoch_no = 1,
                hash = new byte[] { 11, 11, 11, 11 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 111,
                previous_id = 100,
                previousHash = new byte[] { 10, 1, 1, 1 }
            })).ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 10,
                epoch_no = 1,
                hash = new byte[] { 10, 1, 1, 1 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 100,
                previous_id = 90,
                previousHash = new byte[] { 9, 1, 1, 1 }
            }));

        var syncResult2 = await SyncService.RunSync(_mediatorMock.Object,new AppSettings() { FastSyncBlockDistanceRequirement = 150 }, logger.Object, "preprod", new CancellationToken(), 10, false);

        // Assert
        syncResult2.IsSuccess.Should().BeTrue();

        // We should also now have a forked block 11 and 12 in the database. But these are the blocks initially created
        var blocksforked = await _context.BlockEntities.Where(p => p.IsFork).ToListAsync();
        blocksforked.Should().Contain(p => p.BlockHeight == 11);
        blocksforked.Should().Contain(p => p.BlockHeight == 12);
        blocksforked.Should().Contain(p => p.BlockHashPrefix == BlockEntity.CalculateBlockHashPrefix(new byte[] { 11, 1, 1, 1 }));
        blocksforked.Should().Contain(p => p.BlockHashPrefix == BlockEntity.CalculateBlockHashPrefix(new byte[] { 12, 1, 1, 1 }));
    }

    [Fact]
    public async Task Unknown_tip_with_identical_blockHeight_switches_fork_and_continues_new_fork()
    {
        // Arrange block 10 as base
        var logger = new Mock<ILogger>();
        var ledgerType = LedgerType.CardanoPreprod;
        var epochNumber = 1;
        var blockHeight = 9;
        blockHeight++;
        var baseBlockHash = new byte[] { 10, 1, 1, 1 };
        await SetupLedgerEpochAndBlock(ledgerType, epochNumber, blockHeight, baseBlockHash, null, 9);

        // Setup the dbsync Block to be at block 12 to start syncing
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetPostgresBlockTipRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok(new Block()
        {
            block_no = 12,
            epoch_no = 1,
            hash = new byte[] { 12, 1, 1, 1 },
            time = DateTime.UtcNow,
            tx_count = 0,
            id = 120,
            previous_id = 110,
            previousHash = new byte[] { 11, 1, 1, 1 }
        }));

        // No Prism transactions in the blocks
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetTransactionsWithPrismMetadataForBlockIdRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new List<Transaction>()));

        _mediatorMock.Setup(p => p.Send(It.IsAny<GetMostRecentBlockRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (GetMostRecentBlockRequest request, CancellationToken token) => await this._getMostRecentBlockHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetBlockByBlockHashRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (GetBlockByBlockHashRequest request, CancellationToken token) => await this._getBlockByBlockHashHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetEpochRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (GetEpochRequest request, CancellationToken token) => await this._getEpochHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<ProcessBlockRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (ProcessBlockRequest request, CancellationToken token) => await this._processBlockHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<CreateBlockRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (CreateBlockRequest request, CancellationToken token) => await this._createBlockHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<SwitchBranchRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (SwitchBranchRequest request, CancellationToken token) => await this._switchBranchHandler.Handle(request, token));

        // Return block 11 from dbsync, and then block 12
        _mediatorMock.SetupSequence(p => p.Send(It.IsAny<GetPostgresBlockByBlockNoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 11,
                epoch_no = 1,
                hash = new byte[] { 11, 1, 1, 1 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 110,
                previous_id = 100,
                previousHash = new byte[] { 10, 1, 1, 1 }
            })).ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 12,
                epoch_no = 1,
                hash = new byte[] { 12, 1, 1, 1 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 120,
                previous_id = 110,
                previousHash = new byte[] { 11, 1, 1, 1 }
            }));

        // Act 1
        var syncResult = await SyncService.RunSync(_mediatorMock.Object,new AppSettings() { FastSyncBlockDistanceRequirement = 150 }, logger.Object, "preprod", new CancellationToken(), 10, false);

        // Assert
        syncResult.IsSuccess.Should().BeTrue();

        // Blocks 10, 11, and 12 should be in the database
        var blocks = await _context.BlockEntities.ToListAsync();
        blocks.Should().Contain(p => p.BlockHeight == 10);
        blocks.Should().Contain(p => p.BlockHeight == 11);
        blocks.Should().Contain(p => p.BlockHeight == 12);

        // Now the fork comes into play. When rerunning the sync-process, the tip is at block 12, but with different hash 
        // It is also not referencing the same previousId (110) as the existing block 12 in the database, instead it is referencing another forked block (111)
        // Return block 12 from dbsync as the tip
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetPostgresBlockTipRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok(new Block()
        {
            block_no = 12,
            epoch_no = 1,
            hash = new byte[] { 12, 12, 12, 12 },
            time = DateTime.UtcNow,
            tx_count = 0,
            id = 121,
            previous_id = 111,
            previousHash = new byte[] { 11, 11, 11, 11 }
        }));

        // Get data for block 12
        _mediatorMock.SetupSequence(p => p.Send(It.IsAny<GetPostgresBlockByBlockNoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 11,
                epoch_no = 1,
                hash = new byte[] { 11, 11, 11, 11 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 111,
                previous_id = 100,
                previousHash = new byte[] { 10, 1, 1, 1 }
            }))
            .ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 12,
                epoch_no = 1,
                hash = new byte[] { 12, 12, 12, 12 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 121,
                previous_id = 111,
                previousHash = new byte[] { 11, 11, 11, 11 }
            }));

        // Get block 11 from dbsync by its id
        _mediatorMock.SetupSequence(p => p.Send(It.IsAny<GetPostgresBlockByBlockIdRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 11,
                epoch_no = 1,
                hash = new byte[] { 11, 11, 11, 11 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 111,
                previous_id = 100,
                previousHash = new byte[] { 10, 1, 1, 1 }
            })).ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 10,
                epoch_no = 1,
                hash = new byte[] { 10, 1, 1, 1 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 100,
                previous_id = 90,
                previousHash = new byte[] { 9, 1, 1, 1 }
            }));

        var syncResult2 = await SyncService.RunSync(_mediatorMock.Object,new AppSettings() { FastSyncBlockDistanceRequirement = 150 }, logger.Object, "preprod", new CancellationToken(), 10, false);

        // Assert
        syncResult2.IsSuccess.Should().BeTrue();

        // We should also now have a forked block 11 and 12 in the database. But these are the blocks initially created
        var blocksforked = await _context.BlockEntities.Where(p => p.IsFork).ToListAsync();
        blocksforked.Should().Contain(p => p.BlockHeight == 11);
        blocksforked.Should().Contain(p => p.BlockHeight == 12);
        blocksforked.Should().Contain(p => p.BlockHashPrefix == BlockEntity.CalculateBlockHashPrefix(new byte[] { 11, 1, 1, 1 }));
        blocksforked.Should().Contain(p => p.BlockHashPrefix == BlockEntity.CalculateBlockHashPrefix(new byte[] { 12, 1, 1, 1 }));

        // Next we continue the fork with block 13
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetPostgresBlockTipRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok(new Block()
        {
            block_no = 13,
            epoch_no = 1,
            hash = new byte[] { 3, 3, 3, 3 },
            time = DateTime.UtcNow,
            tx_count = 0,
            id = 131,
            previous_id = 121,
            previousHash = new byte[] { 12, 12, 12, 12 }
        }));

        // Get data for block 13
        _mediatorMock.SetupSequence(p => p.Send(It.IsAny<GetPostgresBlockByBlockNoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 13,
                epoch_no = 1,
                hash = new byte[] { 3, 3, 3, 3 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 131,
                previous_id = 121,
                previousHash = new byte[] { 12, 12, 12, 12 }
            }));

        var syncResult3 = await SyncService.RunSync(_mediatorMock.Object,new AppSettings() { FastSyncBlockDistanceRequirement = 150 }, logger.Object, "preprod", new CancellationToken(), 10, false);
        syncResult3.IsSuccess.Should().BeTrue();
        var blocksNotforked = await _context.BlockEntities.Where(p => !p.IsFork).ToListAsync();
        blocksNotforked.Should().Contain(p => p.BlockHeight == 13);
        blocksNotforked.FirstOrDefault(p => p.BlockHeight == 13).PreviousBlockHashPrefix.Should().Be(202116108);
    }

    [Fact]
    public async Task Unknown_tip_with_identical_blockHeight_switches_fork_and_continues_old_branch()
    {
        // Arrange block 10 as base
        var logger = new Mock<ILogger>();
        var ledgerType = LedgerType.CardanoPreprod;
        var epochNumber = 1;
        var blockHeight = 9;
        blockHeight++;
        var baseBlockHash = new byte[] { 10, 1, 1, 1 };
        await SetupLedgerEpochAndBlock(ledgerType, epochNumber, blockHeight, baseBlockHash, null, 9);

        // Setup the dbsync Block to be at block 12 to start syncing
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetPostgresBlockTipRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok(new Block()
        {
            block_no = 12,
            epoch_no = 1,
            hash = new byte[] { 12, 1, 1, 1 },
            time = DateTime.UtcNow,
            tx_count = 0,
            id = 120,
            previous_id = 110,
            previousHash = new byte[] { 11, 1, 1, 1 }
        }));

        // No Prism transactions in the blocks
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetTransactionsWithPrismMetadataForBlockIdRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new List<Transaction>()));

        _mediatorMock.Setup(p => p.Send(It.IsAny<GetMostRecentBlockRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (GetMostRecentBlockRequest request, CancellationToken token) => await this._getMostRecentBlockHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetBlockByBlockHashRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (GetBlockByBlockHashRequest request, CancellationToken token) => await this._getBlockByBlockHashHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetEpochRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (GetEpochRequest request, CancellationToken token) => await this._getEpochHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<ProcessBlockRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (ProcessBlockRequest request, CancellationToken token) => await this._processBlockHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<CreateBlockRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (CreateBlockRequest request, CancellationToken token) => await this._createBlockHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<SwitchBranchRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (SwitchBranchRequest request, CancellationToken token) => await this._switchBranchHandler.Handle(request, token));

        // Return block 11 from dbsync, and then block 12
        _mediatorMock.SetupSequence(p => p.Send(It.IsAny<GetPostgresBlockByBlockNoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 11,
                epoch_no = 1,
                hash = new byte[] { 11, 1, 1, 1 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 110,
                previous_id = 100,
                previousHash = new byte[] { 10, 1, 1, 1 }
            })).ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 12,
                epoch_no = 1,
                hash = new byte[] { 12, 1, 1, 1 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 120,
                previous_id = 110,
                previousHash = new byte[] { 11, 1, 1, 1 }
            }));

        // Act 1
        var syncResult = await SyncService.RunSync(_mediatorMock.Object,new AppSettings() { FastSyncBlockDistanceRequirement = 150 }, logger.Object, "preprod", new CancellationToken(), 10, false);

        // Assert
        syncResult.IsSuccess.Should().BeTrue();

        // Blocks 10, 11, and 12 should be in the database
        var blocks = await _context.BlockEntities.ToListAsync();
        blocks.Should().Contain(p => p.BlockHeight == 10);
        blocks.Should().Contain(p => p.BlockHeight == 11);
        blocks.Should().Contain(p => p.BlockHeight == 12);

        // Now the fork comes into play. When rerunning the sync-process, the tip is at block 12, but with different hash 
        // It is also not referencing the same previousId (110) as the existing block 12 in the database, instead it is referencing another forked block (111)
        // Return block 12 from dbsync as the tip
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetPostgresBlockTipRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok(new Block()
        {
            block_no = 12,
            epoch_no = 1,
            hash = new byte[] { 12, 12, 12, 12 },
            time = DateTime.UtcNow,
            tx_count = 0,
            id = 121,
            previous_id = 111,
            previousHash = new byte[] { 11, 11, 11, 11 }
        }));

        // Get data for block 12
        _mediatorMock.SetupSequence(p => p.Send(It.IsAny<GetPostgresBlockByBlockNoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 11,
                epoch_no = 1,
                hash = new byte[] { 11, 11, 11, 11 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 111,
                previous_id = 100,
                previousHash = new byte[] { 10, 1, 1, 1 }
            }))
            .ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 12,
                epoch_no = 1,
                hash = new byte[] { 12, 12, 12, 12 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 121,
                previous_id = 111,
                previousHash = new byte[] { 11, 11, 11, 11 }
            }));

        // Get block 11 from dbsync by its id
        _mediatorMock.SetupSequence(p => p.Send(It.IsAny<GetPostgresBlockByBlockIdRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 11,
                epoch_no = 1,
                hash = new byte[] { 11, 11, 11, 11 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 111,
                previous_id = 100
            })).ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 10,
                epoch_no = 1,
                hash = new byte[] { 10, 1, 1, 1 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 100,
                previous_id = 90
            }));

        var syncResult2 = await SyncService.RunSync(_mediatorMock.Object,new AppSettings() { FastSyncBlockDistanceRequirement = 150 }, logger.Object, "preprod", new CancellationToken(), 10, false);

        // Assert
        syncResult2.IsSuccess.Should().BeTrue();

        // We should also now have a forked block 11 and 12 in the database. But these are the blocks initially created
        var blocksforked = await _context.BlockEntities.Where(p => p.IsFork).ToListAsync();
        blocksforked.Should().Contain(p => p.BlockHeight == 11);
        blocksforked.Should().Contain(p => p.BlockHeight == 12);
        blocksforked.Should().Contain(p => p.BlockHashPrefix == BlockEntity.CalculateBlockHashPrefix(new byte[] { 11, 1, 1, 1 }));
        blocksforked.Should().Contain(p => p.BlockHashPrefix == BlockEntity.CalculateBlockHashPrefix(new byte[] { 12, 1, 1, 1 }));

        // Next we continue the fork with block 13
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetPostgresBlockTipRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok(new Block()
        {
            block_no = 13,
            epoch_no = 1,
            hash = new byte[] { 3, 3, 3, 3 },
            time = DateTime.UtcNow,
            tx_count = 0,
            id = 130,
            previous_id = 120,
            previousHash = new byte[] { 12, 1, 1, 1 }
        }));

        // Get data for block 13
        _mediatorMock.SetupSequence(p => p.Send(It.IsAny<GetPostgresBlockByBlockNoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 13,
                epoch_no = 1,
                hash = new byte[] { 3, 3, 3, 3 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 130,
                previous_id = 120,
                previousHash = new byte[] { 12, 1, 1, 1 }
            }))
            .ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 11,
                epoch_no = 1,
                hash = new byte[] { 11, 1, 1, 1 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 110,
                previous_id = 100,
                previousHash = new byte[] { 10, 1, 1, 1 }
            }));

        // Get block 12 from dbsync by its id
        _mediatorMock.SetupSequence(p => p.Send(It.IsAny<GetPostgresBlockByBlockIdRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 12,
                epoch_no = 1,
                hash = new byte[] { 12, 1, 1, 1 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 120,
                previous_id = 110,
                previousHash = new byte[] { 11, 1, 1, 1 }
            }));

        var syncResult3 = await SyncService.RunSync(_mediatorMock.Object,new AppSettings() { FastSyncBlockDistanceRequirement = 150 }, logger.Object, "preprod", new CancellationToken(), 10, false);
        var blocksNotforked = await _context.BlockEntities.Where(p => !p.IsFork).ToListAsync();
        blocksNotforked.Should().Contain(p => p.BlockHeight == 13);
        blocksNotforked.FirstOrDefault(p => p.BlockHeight == 13).PreviousBlockHashPrefix.Should().Be(16843020);
    }

    [Fact]
    public async Task Unknown_tip_with_higher_blockHeight_switches_fork()
    {
        // Arrange block 10 as base
        var logger = new Mock<ILogger>();
        var ledgerType = LedgerType.CardanoPreprod;
        var epochNumber = 1;
        var blockHeight = 9;
        blockHeight++;
        var baseBlockHash = new byte[] { 10, 1, 1, 1 };
        await SetupLedgerEpochAndBlock(ledgerType, epochNumber, blockHeight, baseBlockHash, null, 9);

        // Setup the dbsync Block to be at block 12 to start syncing
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetPostgresBlockTipRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok(new Block()
        {
            block_no = 12,
            epoch_no = 1,
            hash = new byte[] { 12, 1, 1, 1 },
            time = DateTime.UtcNow,
            tx_count = 0,
            previous_id = 110,
            id = 120,
            previousHash = new byte[] { 11, 1, 1, 1 }
        }));

        // No Prism transactions in the blocks
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetTransactionsWithPrismMetadataForBlockIdRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new List<Transaction>()));

        _mediatorMock.Setup(p => p.Send(It.IsAny<GetMostRecentBlockRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (GetMostRecentBlockRequest request, CancellationToken token) => await this._getMostRecentBlockHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetBlockByBlockHashRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (GetBlockByBlockHashRequest request, CancellationToken token) => await this._getBlockByBlockHashHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetEpochRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (GetEpochRequest request, CancellationToken token) => await this._getEpochHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<ProcessBlockRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (ProcessBlockRequest request, CancellationToken token) => await this._processBlockHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<CreateBlockRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (CreateBlockRequest request, CancellationToken token) => await this._createBlockHandler.Handle(request, token));
        _mediatorMock.Setup(p => p.Send(It.IsAny<SwitchBranchRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (SwitchBranchRequest request, CancellationToken token) => await this._switchBranchHandler.Handle(request, token));

        // Return block 11 from dbsync, and then block 12 
        _mediatorMock.SetupSequence(p => p.Send(It.IsAny<GetPostgresBlockByBlockNoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 11,
                epoch_no = 1,
                hash = new byte[] { 11, 1, 1, 1 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 110,
                previous_id = 100,
                previousHash = new byte[] { 10, 1, 1, 1 }
            })).ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 12,
                epoch_no = 1,
                hash = new byte[] { 12, 1, 1, 1 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 120,
                previous_id = 110,
                previousHash = new byte[] { 11, 1, 1, 1 }
            }));

        // Act 1
        var syncResult = await SyncService.RunSync(_mediatorMock.Object,new AppSettings() { FastSyncBlockDistanceRequirement = 150 }, logger.Object, "preprod", new CancellationToken(), 10, false);

        // Assert
        syncResult.IsSuccess.Should().BeTrue();

        // Blocks 10, 11, and 12 should be in the database
        var blocks = await _context.BlockEntities.ToListAsync();
        blocks.Should().Contain(p => p.BlockHeight == 10);
        blocks.Should().Contain(p => p.BlockHeight == 11);
        blocks.Should().Contain(p => p.BlockHeight == 12);

        // Now the fork comes into play. When rerunning the sync-process, the tip is at block 13, but with different hash 
        // It is also not referencing the same previousId (120) as the existing block 12 in the database, instead it is referencing another forked block (121)
        // Return block 13 from dbsync as the tip
        _mediatorMock.Setup(p => p.Send(It.IsAny<GetPostgresBlockTipRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok(new Block()
        {
            block_no = 13,
            epoch_no = 1,
            hash = new byte[] { 3, 3, 3, 3 },
            time = DateTime.UtcNow,
            tx_count = 0,
            id = 131,
            previous_id = 121,
            previousHash = new byte[] { 12, 12, 12, 12 }
        }));

        // Get data for block 13
        _mediatorMock.SetupSequence(p => p.Send(It.IsAny<GetPostgresBlockByBlockNoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 13,
                epoch_no = 1,
                hash = new byte[] { 3, 3, 3, 3 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 131,
                previous_id = 121,
                previousHash = new byte[] { 12, 12, 12, 12 }
            })).ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 11,
                epoch_no = 1,
                hash = new byte[] { 11, 11, 11, 11 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 111,
                previous_id = 100,
                previousHash = new byte[] { 10, 1, 1, 1 }
            })).ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 12,
                epoch_no = 1,
                hash = new byte[] { 12, 12, 12, 12 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 121,
                previous_id = 111,
                previousHash = new byte[] { 11, 11, 11, 11 }
            })).ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 13,
                epoch_no = 1,
                hash = new byte[] { 3, 3, 3, 3 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 131,
                previous_id = 121,
                previousHash = new byte[] { 12, 12, 12, 12 }
            }));

        // Get block 11 from dbsync by its id
        _mediatorMock.SetupSequence(p => p.Send(It.IsAny<GetPostgresBlockByBlockIdRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 12,
                epoch_no = 1,
                hash = new byte[] { 12, 12, 12, 12 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 121,
                previous_id = 111,
                previousHash = new byte[] { 11, 11, 11, 11 }
            })).ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 11,
                epoch_no = 1,
                hash = new byte[] { 11, 11, 11, 11 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 111,
                previous_id = 100,
                previousHash = new byte[] { 10, 1, 1, 1 }
            })).ReturnsAsync(Result.Ok(new Block()
            {
                block_no = 10,
                epoch_no = 1,
                hash = new byte[] { 10, 1, 1, 1 },
                time = DateTime.UtcNow,
                tx_count = 0,
                id = 100,
                previous_id = 90,
                previousHash = new byte[] { 9, 1, 1, 1 }
            }));

        var syncResult2 = await SyncService.RunSync(_mediatorMock.Object,new AppSettings() { FastSyncBlockDistanceRequirement = 150 }, logger.Object, "preprod", new CancellationToken(), 10, false);

        // Assert
        syncResult2.IsSuccess.Should().BeTrue();

        // We should also now have a forked block 11 and 12 in the database. But these are the blocks initially created
        var blocksforked = await _context.BlockEntities.Where(p => p.IsFork).ToListAsync();
        blocksforked.Should().Contain(p => p.BlockHeight == 11);
        blocksforked.Should().Contain(p => p.BlockHeight == 12);
        blocksforked.Should().Contain(p => p.BlockHashPrefix == BlockEntity.CalculateBlockHashPrefix(new byte[] { 11, 1, 1, 1 }));
        blocksforked.Should().Contain(p => p.BlockHashPrefix == BlockEntity.CalculateBlockHashPrefix(new byte[] { 12, 1, 1, 1 }));
        var blocksNotForked = await _context.BlockEntities.Where(p => !p.IsFork).ToListAsync();
        blocksNotForked.Should().Contain(p => p.BlockHeight == 10);
        blocksNotForked.Should().Contain(p => p.BlockHeight == 11);
        blocksNotForked.Should().Contain(p => p.BlockHeight == 12);
        blocksNotForked.Should().Contain(p => p.BlockHeight == 13);
    }
}