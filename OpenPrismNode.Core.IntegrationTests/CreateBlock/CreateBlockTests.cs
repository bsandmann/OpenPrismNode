using OpenPrismNode.Core.Commands.CreateBlock;
using OpenPrismNode.Core.Commands.CreateEpoch;
using OpenPrismNode.Core.Commands.CreateLedger;
using OpenPrismNode.Core.Models;
using System;
using OpenPrismNode.Core.Entities;

public partial class IntegrationTests
{
    [Fact]
    public async Task CreateBlock_Succeeds_For_Default_Case()
    {
        // Arrange
        await _createLedgerHandler.Handle(new CreateLedgerRequest(LedgerType.CardanoPreprod), CancellationToken.None);
        await _createEpochHandler.Handle(new CreateEpochRequest(LedgerType.CardanoPreprod, 1), CancellationToken.None);
        // Creating the initial block
        // await _context.BlockEntities.AddAsync(new BlockEntity()
        // {
        //     BlockHeight = 1,
        //     BlockHashPrefix = 123,
        //     BlockHash = new byte[] { 1, 1, 1, 1 },
        //     TimeUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified),
        //     TxCount = 1,
        //     LastParsedOnUtc = null,
        //     PrismTransactionEntities = new List<TransactionEntity>(),
        //     EpochNumber = 1,
        // });
        // await _context.SaveChangesAsync();

        var request = new CreateBlockRequest(
            ledgerType: LedgerType.CardanoPreprod,
            blockHash: Hash.CreateFrom(new byte[] { 1, 2, 3, 4 }),
            previousBlockHash: Hash.CreateFrom(new byte[] { 1, 1, 1, 1 }),
            blockHeight: 1,
            previousBlockHeight: null,
            epochNumber: 1,
            timeUtc: DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified),
            txCount: 0
        );

        // Act
        var result = await _createBlockHandler.Handle(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(1, result.Value.BlockHeight);
    }

    [Fact]
    public async Task CreateBlock_Fails_For_NonExistent_Ledger()
    {
        await _createLedgerHandler.Handle(new CreateLedgerRequest(LedgerType.CardanoPreprod), CancellationToken.None);
        await _createEpochHandler.Handle(new CreateEpochRequest(LedgerType.CardanoPreprod, 1), CancellationToken.None);
        
        // Arrange
        var request = new CreateBlockRequest(
            ledgerType: LedgerType.CardanoMainnet, // Assuming this ledger hasn't been created in the test db
            blockHash: Hash.CreateFrom(new byte[] { 1, 2, 3, 4 }),
            previousBlockHash: null,
            blockHeight: 1,
            previousBlockHeight: null,
            epochNumber: 1,
            timeUtc: DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified),
            txCount: 0
        );

        // Act
        var result = await _createBlockHandler.Handle(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("Ledger not found", result.Errors.First().Message);
    }

    [Fact]
    public async Task CreateBlock_Fails_For_NonExistent_Epoch()
    {
        // Arrange
        await _createLedgerHandler.Handle(new CreateLedgerRequest(LedgerType.CardanoPreprod), CancellationToken.None);

        var request = new CreateBlockRequest(
            ledgerType: LedgerType.CardanoPreprod,
            blockHash: Hash.CreateFrom(new byte[] { 1, 2, 3, 4 }),
            previousBlockHash: null,
            blockHeight: 1,
            previousBlockHeight: null,
            epochNumber: 2, // Epoch 2 hasn't been created
            timeUtc: DateTime.UtcNow,
            txCount: 0
        );

        // Act
        var result = await _createBlockHandler.Handle(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("Epoch not found", result.Errors.First().Message);
    }

    [Fact]
    public async Task CreateBlock_Succeeds_For_Subsequent_Block()
    {
        // Arrange
        await _createLedgerHandler.Handle(new CreateLedgerRequest(LedgerType.CardanoPreprod), CancellationToken.None);
        await _createEpochHandler.Handle(new CreateEpochRequest(LedgerType.CardanoPreprod, 1), CancellationToken.None);

        var firstBlockRequest = new CreateBlockRequest(
            ledgerType: LedgerType.CardanoPreprod,
            blockHash: Hash.CreateFrom(new byte[] { 1, 2, 3, 4 }),
            previousBlockHash: null,
            blockHeight: 1,
            previousBlockHeight: null,
            epochNumber: 1,
            timeUtc: DateTime.UtcNow,
            txCount: 0
        );
        await _createBlockHandler.Handle(firstBlockRequest, CancellationToken.None);

        var secondBlockRequest = new CreateBlockRequest(
            ledgerType: LedgerType.CardanoPreprod,
            blockHash: Hash.CreateFrom(new byte[] { 5, 6, 7, 8 }),
            previousBlockHash: Hash.CreateFrom(new byte[] { 1, 2, 3, 4 }),
            blockHeight: 2,
            previousBlockHeight: 1,
            epochNumber: 1,
            timeUtc: DateTime.UtcNow.AddMinutes(1),
            txCount: 1
        );

        // Act
        var result = await _createBlockHandler.Handle(secondBlockRequest, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.BlockHeight);
    }

    [Fact]
    public async Task CreateBlock_Fails_For_Duplicate_BlockHeight()
    {
        // Arrange
        await _createLedgerHandler.Handle(new CreateLedgerRequest(LedgerType.CardanoPreprod), CancellationToken.None);
        await _createEpochHandler.Handle(new CreateEpochRequest(LedgerType.CardanoPreprod, 1), CancellationToken.None);

        var firstBlockRequest = new CreateBlockRequest(
            ledgerType: LedgerType.CardanoPreprod,
            blockHash: Hash.CreateFrom(new byte[] { 1, 2, 3, 4 }),
            previousBlockHash: null,
            blockHeight: 1,
            previousBlockHeight: null,
            epochNumber: 1,
            timeUtc: DateTime.UtcNow,
            txCount: 0
        );
        await _createBlockHandler.Handle(firstBlockRequest, CancellationToken.None);

        var duplicateBlockRequest = new CreateBlockRequest(
            ledgerType: LedgerType.CardanoPreprod,
            blockHash: Hash.CreateFrom(new byte[] { 5, 6, 7, 8 }),
            previousBlockHash: null,
            blockHeight: 1, // Same block height
            previousBlockHeight: null,
            epochNumber: 1,
            timeUtc: DateTime.UtcNow.AddMinutes(1),
            txCount: 1
        );

        // Act
        var result = await _createBlockHandler.Handle(duplicateBlockRequest, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("Block with this height already exists", result.Errors.First().Message);
    }

    [Fact]
    public async Task CreateBlock_Fails_For_Invalid_PreviousBlockHash()
    {
        // Arrange
        await _createLedgerHandler.Handle(new CreateLedgerRequest(LedgerType.CardanoPreprod), CancellationToken.None);
        await _createEpochHandler.Handle(new CreateEpochRequest(LedgerType.CardanoPreprod, 1), CancellationToken.None);

        var firstBlockRequest = new CreateBlockRequest(
            ledgerType: LedgerType.CardanoPreprod,
            blockHash: Hash.CreateFrom(new byte[] { 1, 2, 3, 4 }),
            previousBlockHash: null,
            blockHeight: 1,
            previousBlockHeight: null,
            epochNumber: 1,
            timeUtc: DateTime.UtcNow,
            txCount: 0
        );
        await _createBlockHandler.Handle(firstBlockRequest, CancellationToken.None);

        var invalidPreviousHashRequest = new CreateBlockRequest(
            ledgerType: LedgerType.CardanoPreprod,
            blockHash: Hash.CreateFrom(new byte[] { 5, 6, 7, 8 }),
            previousBlockHash: Hash.CreateFrom(new byte[] { 9, 10, 11, 12 }), // Invalid previous hash
            blockHeight: 2,
            previousBlockHeight: 1,
            epochNumber: 1,
            timeUtc: DateTime.UtcNow.AddMinutes(1),
            txCount: 1
        );

        // Act
        var result = await _createBlockHandler.Handle(invalidPreviousHashRequest, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("Invalid previous block hash", result.Errors.First().Message);
    }
}