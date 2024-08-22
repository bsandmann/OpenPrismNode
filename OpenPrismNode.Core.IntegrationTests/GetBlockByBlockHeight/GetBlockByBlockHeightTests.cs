using OpenPrismNode.Core.Commands.CreateBlock;
using OpenPrismNode.Core.Commands.CreateEpoch;
using OpenPrismNode.Core.Commands.CreateLedger;
using OpenPrismNode.Core.Commands.GetBlockByBlockHeight;
using OpenPrismNode.Core.Models;
using System;
using OpenPrismNode.Core.Entities;

public partial class IntegrationTests
{
    [Fact]
    public async Task GetBlockByBlockHeight_Succeeds_For_Existing_Block()
    {
        // Arrange
        var ledgerType = LedgerType.CardanoPreprod;
        var epochNumber = 10;
        var blockHeight = 100;

        await _createLedgerHandler.Handle(new CreateLedgerRequest(ledgerType), CancellationToken.None);
        await _createEpochHandler.Handle(new CreateEpochRequest(ledgerType, epochNumber), CancellationToken.None);

        var createBlockRequest = new CreateBlockRequest(
            ledgerType: ledgerType,
            blockHash: Hash.CreateFrom(new byte[] { 3, 3, 3, 4 }),
            previousBlockHash: null,
            blockHeight: blockHeight,
            previousBlockHeight: null,
            epochNumber: epochNumber,
            timeUtc: DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified),
            txCount: 0
        );
        await _createBlockHandler.Handle(createBlockRequest, CancellationToken.None);

        var getBlockRequest = new GetBlockByBlockHeightRequest(ledgerType, blockHeight);

        // Act
        var result = await _getBlockByBlockHeightHandler.Handle(getBlockRequest, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(blockHeight, result.Value.BlockHeight);
        Assert.Equal(epochNumber, result.Value.EpochNumber);
        Assert.Equal(createBlockRequest.BlockHash.Value, result.Value.BlockHash);
    }

    [Fact]
    public async Task GetBlockByBlockHeight_Fails_For_NonExistent_Block()
    {
        // Arrange
        var ledgerType = LedgerType.CardanoPreprod;
        var nonExistentBlockHeight = 999;

        await _createLedgerHandler.Handle(new CreateLedgerRequest(ledgerType), CancellationToken.None);

        var getBlockRequest = new GetBlockByBlockHeightRequest(ledgerType, nonExistentBlockHeight);

        // Act
        var result = await _getBlockByBlockHeightHandler.Handle(getBlockRequest, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains($"No blocks found in the database with the given block height: {nonExistentBlockHeight}", result.Errors.First().Message);
    }

    [Fact]
    public async Task GetBlockByBlockHeight_Fails_For_NonExistent_Ledger()
    {
        // Arrange
        var nonExistentLedgerType = LedgerType.UnknownLedger; // Assuming this ledger hasn't been created
        var blockHeight = 1;

        var getBlockRequest = new GetBlockByBlockHeightRequest(nonExistentLedgerType, blockHeight);

        // Act
        var result = await _getBlockByBlockHeightHandler.Handle(getBlockRequest, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains($"No blocks found in the database with the given block height: {blockHeight}", result.Errors.First().Message);
    }
}