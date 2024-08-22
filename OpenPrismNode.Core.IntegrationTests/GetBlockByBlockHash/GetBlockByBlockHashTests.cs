using OpenPrismNode.Core.Commands.CreateBlock;
using OpenPrismNode.Core.Commands.CreateEpoch;
using OpenPrismNode.Core.Commands.CreateLedger;
using OpenPrismNode.Core.Commands.GetBlockByBlockHash;
using OpenPrismNode.Core.Models;
using System;
using OpenPrismNode.Core.Entities;

public partial class IntegrationTests
{
    [Fact]
    public async Task GetBlockByBlockHash_Succeeds_For_Existing_Block()
    {
        // Arrange
        var ledgerType = LedgerType.CardanoPreprod;
        var epochNumber = 11;
        var blockHeight = 101;
        var blockHash = Hash.CreateFrom(new byte[] { 3, 4, 3, 4 });

        await _createLedgerHandler.Handle(new CreateLedgerRequest(ledgerType), CancellationToken.None);
        await _createEpochHandler.Handle(new CreateEpochRequest(ledgerType, epochNumber), CancellationToken.None);

        var createBlockRequest = new CreateBlockRequest(
            ledgerType: ledgerType,
            blockHash: blockHash,
            previousBlockHash: null,
            blockHeight: blockHeight,
            previousBlockHeight: null,
            epochNumber: epochNumber,
            timeUtc: DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified),
            txCount: 0
        );
        await _createBlockHandler.Handle(createBlockRequest, CancellationToken.None);

        var getBlockRequest = new GetBlockByBlockHashRequest(blockHeight, BlockEntity.CalculateBlockHashPrefix(blockHash.Value), ledgerType);

        // Act
        var result = await _getBlockByBlockHashHandler.Handle(getBlockRequest, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(blockHeight, result.Value.BlockHeight);
        Assert.Equal(epochNumber, result.Value.EpochNumber);
        Assert.Equal(blockHash.Value, result.Value.BlockHash);
    }

    [Fact]
    public async Task GetBlockByBlockHash_Fails_For_NonExistent_Block()
    {
        // Arrange
        var ledgerType = LedgerType.CardanoPreprod;
        var nonExistentBlockHeight = 999;
        var nonExistentBlockHash = Hash.CreateFrom(new byte[] { 9, 4, 9, 9 });

        await _createLedgerHandler.Handle(new CreateLedgerRequest(ledgerType), CancellationToken.None);

        var getBlockRequest = new GetBlockByBlockHashRequest(nonExistentBlockHeight, BlockEntity.CalculateBlockHashPrefix(nonExistentBlockHash.Value), ledgerType);

        // Act
        var result = await _getBlockByBlockHashHandler.Handle(getBlockRequest, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains($"No block found with height {nonExistentBlockHeight} and hash prefix", result.Errors.First().Message);
    }

    [Fact]
    public async Task GetBlockByBlockHash_Fails_For_NonExistent_Ledger()
    {
        // Arrange
        var nonExistentLedgerType = LedgerType.UnknownLedger; // Assuming this ledger hasn't been created
        var blockHeight = 1;
        var blockHash = Hash.CreateFrom(new byte[] { 1, 4, 1, 13});

        var getBlockRequest = new GetBlockByBlockHashRequest(blockHeight, BlockEntity.CalculateBlockHashPrefix(blockHash.Value), nonExistentLedgerType);

        // Act
        var result = await _getBlockByBlockHashHandler.Handle(getBlockRequest, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains($"No block found with height {blockHeight} and hash prefix", result.Errors.First().Message);
    }
}