using FluentAssertions;
using FluentResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using OpenPrismNode.Core.Commands.CreateBlock;
using OpenPrismNode.Core.Commands.GetBlockByBlockHash;
using OpenPrismNode.Core.Commands.GetEpoch;
using OpenPrismNode.Core.Commands.GetMostRecentBlock;
using OpenPrismNode.Core.Commands.SwitchBranch;
using OpenPrismNode.Core.DbSyncModels;
using OpenPrismNode.Core.Entities;
using OpenPrismNode.Core.Models;
using OpenPrismNode.Sync.Commands.GetPostgresBlockByBlockId;
using OpenPrismNode.Sync.Commands.GetPostgresBlockByBlockNo;
using OpenPrismNode.Sync.Commands.GetPostgresBlockTip;
using OpenPrismNode.Sync.Commands.GetTransactionsWithPrismMetadataForBlockId;
using OpenPrismNode.Sync.Commands.ProcessBlock;
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
        var syncResult = await SyncService.RunSync(_mediatorMock.Object, logger.Object, "preprod", new CancellationToken(), 10, false);

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
        var syncResult = await SyncService.RunSync(_mediatorMock.Object, logger.Object, "preprod", new CancellationToken(), 10, false);

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

        var syncResult2 = await SyncService.RunSync(_mediatorMock.Object, logger.Object, "preprod", new CancellationToken(), 10, false);

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
        var syncResult = await SyncService.RunSync(_mediatorMock.Object, logger.Object, "preprod", new CancellationToken(), 10, false);

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

        var syncResult2 = await SyncService.RunSync(_mediatorMock.Object, logger.Object, "preprod", new CancellationToken(), 10, false);

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
        var syncResult = await SyncService.RunSync(_mediatorMock.Object, logger.Object, "preprod", new CancellationToken(), 10, false);

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

        var syncResult2 = await SyncService.RunSync(_mediatorMock.Object, logger.Object, "preprod", new CancellationToken(), 10, false);

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

        // Setup the dbsync Block to be at block 12 to start syncing
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
        var syncResult = await SyncService.RunSync(_mediatorMock.Object, logger.Object, "preprod", new CancellationToken(), 10, false);

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

        var syncResult2 = await SyncService.RunSync(_mediatorMock.Object, logger.Object, "preprod", new CancellationToken(), 10, false);

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
        var syncResult = await SyncService.RunSync(_mediatorMock.Object, logger.Object, "preprod", new CancellationToken(), 10, false);

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

        var syncResult2 = await SyncService.RunSync(_mediatorMock.Object, logger.Object, "preprod", new CancellationToken(), 10, false);

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
        var syncResult = await SyncService.RunSync(_mediatorMock.Object, logger.Object, "preprod", new CancellationToken(), 10, false);

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

        var syncResult2 = await SyncService.RunSync(_mediatorMock.Object, logger.Object, "preprod", new CancellationToken(), 10, false);

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
            previous_id = 121
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
                previous_id = 121
            }));

        var syncResult3 = await SyncService.RunSync(_mediatorMock.Object, logger.Object, "preprod", new CancellationToken(), 10, false);
        var blocksNotforked = await _context.BlockEntities.Where(p => !p.IsFork).ToListAsync();
        blocksNotforked.Should().Contain(p => p.BlockHeight == 13);
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
        var syncResult = await SyncService.RunSync(_mediatorMock.Object, logger.Object, "preprod", new CancellationToken(), 10, false);

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

        var syncResult2 = await SyncService.RunSync(_mediatorMock.Object, logger.Object, "preprod", new CancellationToken(), 10, false);

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
            }));

        var syncResult3 = await SyncService.RunSync(_mediatorMock.Object, logger.Object, "preprod", new CancellationToken(), 10, false);
        var blocksNotforked = await _context.BlockEntities.Where(p => !p.IsFork).ToListAsync();
        blocksNotforked.Should().Contain(p => p.BlockHeight == 13);
    }

    //  [Fact]
    // public async Task Unknown_tip_with_higher_blockHeight_switches_fork()
    // {
    //     // Arrange block 10 as base
    //     var logger = new Mock<ILogger>();
    //     var ledgerType = LedgerType.CardanoPreprod;
    //     var epochNumber = 1;
    //     var blockHeight = 9;
    //     blockHeight++;
    //     var baseBlockHash = new byte[] { 10, 1, 1, 1 };
    //     await SetupLedgerEpochAndBlock(ledgerType, epochNumber, blockHeight, baseBlockHash, null, 9);
    //
    //     // Setup the dbsync Block to be at block 12 to start syncing
    //     _mediatorMock.Setup(p => p.Send(It.IsAny<GetPostgresBlockTipRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok(new Block()
    //     {
    //         block_no = 12,
    //         epoch_no = 1,
    //         hash = new byte[] { 12, 1, 1, 1 },
    //         time = DateTime.UtcNow,
    //         tx_count = 0,
    //         previous_id = 110,
    //         id = 120
    //     }));
    //
    //     // No Prism transactions in the blocks
    //     _mediatorMock.Setup(p => p.Send(It.IsAny<GetTransactionsWithPrismMetadataForBlockIdRequest>(), It.IsAny<CancellationToken>()))
    //         .ReturnsAsync(Result.Ok(new List<Transaction>()));
    //
    //     _mediatorMock.Setup(p => p.Send(It.IsAny<GetMostRecentBlockRequest>(), It.IsAny<CancellationToken>()))
    //         .Returns(async (GetMostRecentBlockRequest request, CancellationToken token) => await this._getMostRecentBlockHandler.Handle(request, token));
    //     _mediatorMock.Setup(p => p.Send(It.IsAny<GetBlockByBlockHashRequest>(), It.IsAny<CancellationToken>()))
    //         .Returns(async (GetBlockByBlockHashRequest request, CancellationToken token) => await this._getBlockByBlockHashHandler.Handle(request, token));
    //     _mediatorMock.Setup(p => p.Send(It.IsAny<GetEpochRequest>(), It.IsAny<CancellationToken>()))
    //         .Returns(async (GetEpochRequest request, CancellationToken token) => await this._getEpochHandler.Handle(request, token));
    //     _mediatorMock.Setup(p => p.Send(It.IsAny<ProcessBlockRequest>(), It.IsAny<CancellationToken>()))
    //         .Returns(async (ProcessBlockRequest request, CancellationToken token) => await this._processBlockHandler.Handle(request, token));
    //     _mediatorMock.Setup(p => p.Send(It.IsAny<CreateBlockRequest>(), It.IsAny<CancellationToken>()))
    //         .Returns(async (CreateBlockRequest request, CancellationToken token) => await this._createBlockHandler.Handle(request, token));
    //     _mediatorMock.Setup(p => p.Send(It.IsAny<SwitchBranchRequest>(), It.IsAny<CancellationToken>()))
    //         .Returns(async (SwitchBranchRequest request, CancellationToken token) => await this._switchBranchHandler.Handle(request, token));
    //
    //     // Return block 11 from dbsync, and then block 12 
    //     _mediatorMock.SetupSequence(p => p.Send(It.IsAny<GetPostgresBlockByBlockNoRequest>(), It.IsAny<CancellationToken>()))
    //         .ReturnsAsync(Result.Ok(new Block()
    //         {
    //             block_no = 11,
    //             epoch_no = 1,
    //             hash = new byte[] { 11, 1, 1, 1 },
    //             time = DateTime.UtcNow,
    //             tx_count = 0,
    //             id = 110,
    //             previous_id = 100
    //         })).ReturnsAsync(Result.Ok(new Block()
    //         {
    //             block_no = 12,
    //             epoch_no = 1,
    //             hash = new byte[] { 12, 1, 1, 1 },
    //             time = DateTime.UtcNow,
    //             tx_count = 0,
    //             id = 120,
    //             previous_id = 110
    //         }));
    //     
    //     // Act 1
    //     var syncResult = await SyncService.RunSync(_mediatorMock.Object, logger.Object, "preprod", new CancellationToken(), 10, false);
    //
    //     // Assert
    //     syncResult.IsSuccess.Should().BeTrue();
    //
    //     // Blocks 10, 11, and 12 should be in the database
    //     var blocks = await _context.BlockEntities.ToListAsync();
    //     blocks.Should().Contain(p => p.BlockHeight == 10);
    //     blocks.Should().Contain(p => p.BlockHeight == 11);
    //     blocks.Should().Contain(p => p.BlockHeight == 12);
    //
    //     // Now the fork comes into play. When rerunning the sync-process, the tip is at block 13, but with different hash 
    //     // It is also not referencing the same previousId (120) as the existing block 12 in the database, instead it is referencing another forked block (121)
    //     // Return block 13 from dbsync as the tip
    //     _mediatorMock.Setup(p => p.Send(It.IsAny<GetPostgresBlockTipRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok(new Block()
    //     {
    //         block_no = 13,
    //         epoch_no = 1,
    //         hash = new byte[] { 13, 13, 13, 13 },
    //         time = DateTime.UtcNow,
    //         tx_count = 0,
    //         id = 131,
    //         previous_id = 121
    //     }));
    //
    //     // Get data for block 13
    //     _mediatorMock.SetupSequence(p => p.Send(It.IsAny<GetPostgresBlockByBlockNoRequest>(), It.IsAny<CancellationToken>()))
    //         .ReturnsAsync(Result.Ok(new Block()
    //         {
    //             block_no = 13,
    //             epoch_no = 1,
    //             hash = new byte[] { 13, 13, 13, 13 },
    //             time = DateTime.UtcNow,
    //             tx_count = 0,
    //             id = 131,
    //             previous_id = 121
    //         })).ReturnsAsync(Result.Ok(new Block()
    //         {
    //             block_no = 12,
    //             epoch_no = 1,
    //             hash = new byte[] { 12, 12, 12, 12 },
    //             time = DateTime.UtcNow,
    //             tx_count = 0,
    //             id = 121,
    //             previous_id = 111
    //         })).ReturnsAsync(Result.Ok(new Block()
    //         {
    //             block_no = 11,
    //             epoch_no = 1,
    //             hash = new byte[] { 11, 11, 11, 11 },
    //             time = DateTime.UtcNow,
    //             tx_count = 0,
    //             id = 111,
    //             previous_id = 100
    //         }));
    //
    //     // Get block 11 from dbsync by its id
    //     _mediatorMock.SetupSequence(p => p.Send(It.IsAny<GetPostgresBlockByBlockIdRequest>(), It.IsAny<CancellationToken>()))
    //         .ReturnsAsync(Result.Ok(new Block()
    //         {
    //             block_no = 12,
    //             epoch_no = 1,
    //             hash = new byte[] { 12, 12, 12, 12 },
    //             time = DateTime.UtcNow,
    //             tx_count = 0,
    //             id = 121,
    //             previous_id = 111
    //         })).ReturnsAsync(Result.Ok(new Block()
    //         {
    //             block_no = 11,
    //             epoch_no = 1,
    //             hash = new byte[] { 11, 11, 11, 11 },
    //             time = DateTime.UtcNow,
    //             tx_count = 0,
    //             id = 111,
    //             previous_id = 100
    //         })).ReturnsAsync(Result.Ok(new Block()
    //         {
    //             block_no = 10,
    //             epoch_no = 1,
    //             hash = new byte[] { 10, 1, 1, 1 },
    //             time = DateTime.UtcNow,
    //             tx_count = 0,
    //             id = 100,
    //             previous_id = 90
    //         }));
    //
    //     var syncResult2 = await SyncService.RunSync(_mediatorMock.Object, logger.Object, "preprod", new CancellationToken(), 10, false);
    //
    //     // Assert
    //     syncResult2.IsSuccess.Should().BeTrue();
    //
    //     // We should also now have a forked block 11 and 12 in the database. But these are the blocks initially created
    //     var blocksforked = await _context.BlockEntities.Where(p => p.IsFork).ToListAsync();
    //     blocksforked.Should().Contain(p => p.BlockHeight == 11);
    //     blocksforked.Should().Contain(p => p.BlockHeight == 12);
    //     blocksforked.Should().Contain(p => p.BlockHashPrefix == BlockEntity.CalculateBlockHashPrefix(new byte[] { 11, 1, 1, 1 }));
    //     blocksforked.Should().Contain(p => p.BlockHashPrefix == BlockEntity.CalculateBlockHashPrefix(new byte[] { 12, 1, 1, 1 }));
    //     var blocksNotForked = await _context.BlockEntities.Where(p => !p.IsFork).ToListAsync();
    //     blocksNotForked.Should().Contain(p => p.BlockHeight == 10);
    //     blocksNotForked.Should().Contain(p => p.BlockHeight == 11);
    //     blocksNotForked.Should().Contain(p => p.BlockHeight == 12);
    //     blocksNotForked.Should().Contain(p => p.BlockHeight == 13);
    // }

    // todo switiching back to original branch
}