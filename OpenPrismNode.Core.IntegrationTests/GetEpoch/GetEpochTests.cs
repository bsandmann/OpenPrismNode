using OpenPrismNode.Core.Commands.CreateEpoch;
using OpenPrismNode.Core.Commands.CreateLedger;
using OpenPrismNode.Core.Commands.GetEpoch;
using OpenPrismNode.Core.Models;
using System;
using OpenPrismNode.Core.Entities;

public partial class IntegrationTests
{
    [Fact]
    public async Task GetEpoch_Succeeds_For_Existing_Epoch()
    {
        // Arrange
        var ledgerType = LedgerType.CardanoPreprod;
        var epochNumber = 10;

        await _createLedgerHandler.Handle(new CreateLedgerRequest(ledgerType), CancellationToken.None);
        await _createEpochHandler.Handle(new CreateEpochRequest(ledgerType, epochNumber), CancellationToken.None);

        var getEpochRequest = new GetEpochRequest(ledgerType, epochNumber);

        // Act
        var result = await _getEpochHandler.Handle(getEpochRequest, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(epochNumber, result.Value.EpochNumber);
        Assert.Equal(ledgerType, result.Value.Ledger);
    }

    [Fact]
    public async Task GetEpoch_Fails_For_NonExistent_Epoch()
    {
        // Arrange
        var ledgerType = LedgerType.CardanoPreprod;
        var nonExistentEpochNumber = 999;

        await _createLedgerHandler.Handle(new CreateLedgerRequest(ledgerType), CancellationToken.None);

        var getEpochRequest = new GetEpochRequest(ledgerType, nonExistentEpochNumber);

        // Act
        var result = await _getEpochHandler.Handle(getEpochRequest, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains($"Epoch {nonExistentEpochNumber} could not be found", result.Errors.First().Message);
    }

    [Fact]
    public async Task GetEpoch_Fails_For_NonExistent_Ledger()
    {
        // Arrange
        var nonExistentLedgerType = LedgerType.UnknownLedger; // Assuming this ledger hasn't been created
        var epochNumber = 1;

        var getEpochRequest = new GetEpochRequest(nonExistentLedgerType, epochNumber);

        // Act
        var result = await _getEpochHandler.Handle(getEpochRequest, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains($"Epoch {epochNumber} could not be found", result.Errors.First().Message);
    }
}