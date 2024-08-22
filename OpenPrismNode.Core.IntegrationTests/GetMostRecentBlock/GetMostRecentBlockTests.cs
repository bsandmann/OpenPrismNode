using OpenPrismNode.Core.Commands.CreateBlock;
using OpenPrismNode.Core.Commands.CreateEpoch;
using OpenPrismNode.Core.Commands.CreateLedger;
using OpenPrismNode.Core.Commands.GetMostRecentBlock;
using OpenPrismNode.Core.Models;
using System;
using OpenPrismNode.Core.Entities;

public partial class IntegrationTests
{
    [Fact]
    public async Task GetMostRecentBlock_Succeeds_For_Existing_Blocks()
    {
        // Arrange
        var ledgerType = LedgerType.CardanoPreprod;
        var epochNumber = 10;
        var blockHeight1 = 300;
        var blockHeight2 = 301;

        await _createLedgerHandler.Handle(new CreateLedgerRequest(ledgerType), CancellationToken.None);
        await _createEpochHandler.Handle(new CreateEpochRequest(ledgerType, epochNumber), CancellationToken.None);

        var createBlockRequest1 = new CreateBlockRequest(
            ledgerType: ledgerType,
            blockHash: Hash.CreateFrom(new byte[] { 3, 6, 3, 4 }),
            previousBlockHash: null,
            blockHeight: blockHeight1,
            previousBlockHeight: null,
            epochNumber: epochNumber,
            timeUtc: DateTime.SpecifyKind(DateTime.UtcNow.AddMinutes(-1), DateTimeKind.Unspecified),
            txCount: 0
        );
        await _createBlockHandler.Handle(createBlockRequest1, CancellationToken.None);

        var createBlockRequest2 = new CreateBlockRequest(
            ledgerType: ledgerType,
            blockHash: Hash.CreateFrom(new byte[] { 4, 9, 4, 5 }),
            previousBlockHash: Hash.CreateFrom(new byte[] { 3, 6, 3, 4 }),
            blockHeight: blockHeight2,
            previousBlockHeight: blockHeight1,
            epochNumber: epochNumber,
            timeUtc: DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified),
            txCount: 1
        );
        await _createBlockHandler.Handle(createBlockRequest2, CancellationToken.None);

        var getMostRecentBlockRequest = new GetMostRecentBlockRequest(ledgerType);

        // Act
        var result = await _getMostRecentBlockHandler.Handle(getMostRecentBlockRequest, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(blockHeight2, result.Value.BlockHeight);
        Assert.Equal(epochNumber, result.Value.EpochNumber);
        Assert.Equal(createBlockRequest2.BlockHash.Value, result.Value.BlockHash);
    }

    [Fact]
    public async Task GetMostRecentBlock_Fails_For_Empty_Ledger()
    {
        // Arrange
        var ledgerType = LedgerType.UnknownLedger;

        await _createLedgerHandler.Handle(new CreateLedgerRequest(ledgerType), CancellationToken.None);

        var getMostRecentBlockRequest = new GetMostRecentBlockRequest(ledgerType);

        // Act
        var result = await _getMostRecentBlockHandler.Handle(getMostRecentBlockRequest, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("No blocks found in the database.", result.Errors.First().Message);
    }

    [Fact]
    public async Task GetMostRecentBlock_Fails_For_NonExistent_Ledger()
    {
        // Arrange
        var nonExistentLedgerType = LedgerType.UnknownLedger; // Assuming this ledger hasn't been created

        var getMostRecentBlockRequest = new GetMostRecentBlockRequest(nonExistentLedgerType);

        // Act
        var result = await _getMostRecentBlockHandler.Handle(getMostRecentBlockRequest, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("No blocks found in the database.", result.Errors.First().Message);
    }

    [Fact]
    public async Task GetMostRecentBlock_Returns_Most_Recent_Non_Fork_Block()
    {
        // Arrange
        var ledgerType = LedgerType.CardanoPreprod;
        var epochNumber = 10;
        var blockHeight1 = 200;
        var blockHeight2 = 201;
        var blockHeight3 = 202;

        await _createLedgerHandler.Handle(new CreateLedgerRequest(ledgerType), CancellationToken.None);
        await _createEpochHandler.Handle(new CreateEpochRequest(ledgerType, epochNumber), CancellationToken.None);

        var createBlockRequest1 = new CreateBlockRequest(
            ledgerType: ledgerType,
            blockHash: Hash.CreateFrom(new byte[] { 7, 3, 3, 4 }),
            previousBlockHash: null,
            blockHeight: blockHeight1,
            previousBlockHeight: null,
            epochNumber: epochNumber,
            timeUtc: DateTime.SpecifyKind(DateTime.UtcNow.AddMinutes(-2), DateTimeKind.Unspecified),
            txCount: 0
        );
        await _createBlockHandler.Handle(createBlockRequest1, CancellationToken.None);

        var createBlockRequest2 = new CreateBlockRequest(
            ledgerType: ledgerType,
            blockHash: Hash.CreateFrom(new byte[] { 8, 4, 4, 5 }),
            previousBlockHash: Hash.CreateFrom(new byte[] { 7, 3, 3, 4 }),
            blockHeight: blockHeight2,
            previousBlockHeight: blockHeight1,
            epochNumber: epochNumber,
            timeUtc: DateTime.SpecifyKind(DateTime.UtcNow.AddMinutes(-1), DateTimeKind.Unspecified),
            txCount: 1
        );
        await _createBlockHandler.Handle(createBlockRequest2, CancellationToken.None);

        var createBlockRequest3 = new CreateBlockRequest(
            ledgerType: ledgerType,
            blockHash: Hash.CreateFrom(new byte[] { 5, 5, 5, 6 }),
            previousBlockHash: Hash.CreateFrom(new byte[] { 8, 4, 4, 5 }),
            blockHeight: blockHeight3,
            previousBlockHeight: blockHeight2,
            epochNumber: epochNumber,
            timeUtc: DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified),
            txCount: 1,
            isFork: true // This is a fork block
        );
        await _createBlockHandler.Handle(createBlockRequest3, CancellationToken.None);

        var getMostRecentBlockRequest = new GetMostRecentBlockRequest(ledgerType);

        // Act
        var result = await _getMostRecentBlockHandler.Handle(getMostRecentBlockRequest, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(blockHeight2, result.Value.BlockHeight); // Should return block 2, not the fork block 3
        Assert.Equal(epochNumber, result.Value.EpochNumber);
        Assert.Equal(createBlockRequest2.BlockHash.Value, result.Value.BlockHash);
    }
}