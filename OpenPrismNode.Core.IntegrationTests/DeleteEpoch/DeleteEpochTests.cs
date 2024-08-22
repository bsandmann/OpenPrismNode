using OpenPrismNode.Core.Commands.CreateBlock;
using OpenPrismNode.Core.Commands.CreateEpoch;
using OpenPrismNode.Core.Commands.CreateLedger;
using OpenPrismNode.Core.Commands.DeleteEpoch;
using OpenPrismNode.Core.Commands.GetEpoch;
using OpenPrismNode.Core.Commands.GetBlockByBlockHeight;
using OpenPrismNode.Core.Models;
using System;
using OpenPrismNode.Core.Entities;

public partial class IntegrationTests
{
    [Fact]
    public async Task DeleteEpoch_Succeeds_For_Empty_Epoch()
    {
        // Arrange
        var ledgerType = LedgerType.CardanoPreprod;
        var epochNumber = 10;

        await _createLedgerHandler.Handle(new CreateLedgerRequest(ledgerType), CancellationToken.None);
        await _createEpochHandler.Handle(new CreateEpochRequest(ledgerType, epochNumber), CancellationToken.None);

        var deleteEpochRequest = new DeleteEpochRequest(epochNumber, ledgerType);

        // Act
        var result = await _deleteEpochHandler.Handle(deleteEpochRequest, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify that the epoch no longer exists
        var getEpochRequest = new GetEpochRequest(ledgerType, epochNumber);
        var getEpochResult = await _getEpochHandler.Handle(getEpochRequest, CancellationToken.None);
        Assert.True(getEpochResult.IsFailed);
    }

    [Fact]
    public async Task DeleteEpoch_Succeeds_For_Epoch_With_Blocks()
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

        var deleteEpochRequest = new DeleteEpochRequest(epochNumber, ledgerType);

        // Act
        var result = await _deleteEpochHandler.Handle(deleteEpochRequest, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify that the epoch no longer exists
        var getEpochRequest = new GetEpochRequest(ledgerType, epochNumber);
        var getEpochResult = await _getEpochHandler.Handle(getEpochRequest, CancellationToken.None);
        Assert.True(getEpochResult.IsFailed);

        // Verify that the block no longer exists
        var getBlockRequest = new GetBlockByBlockHeightRequest(ledgerType, blockHeight);
        var getBlockResult = await _getBlockByBlockHeightHandler.Handle(getBlockRequest, CancellationToken.None);
        Assert.True(getBlockResult.IsFailed);
    }
}