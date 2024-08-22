using OpenPrismNode.Core.Commands.CreateEpoch;
using OpenPrismNode.Core.Commands.CreateLedger;
using OpenPrismNode.Core.Models;


public partial class IntegrationTests
{
    [Fact]
    public async Task CreateEpoch_succeds_for_default_case()
    {
        // Arrange
        var requestCreateLedger = new CreateLedgerRequest(LedgerType.CardanoPreprod);
        var resultCreateLedger = await _createLedgerHandler.Handle(requestCreateLedger, CancellationToken.None);
        var request = new CreateEpochRequest(LedgerType.CardanoPreprod,1);

        // Act
        var result = await _createEpochHandler.Handle(request, CancellationToken.None);

        // Assert
        Assert.True((bool)result.IsSuccess);
    }
    
    [Fact]
    public async Task CreateEpoch_succeeds_for_already_created_epoch()
    {
        // Arrange
        var requestCreateLedger = new CreateLedgerRequest(LedgerType.CardanoPreprod);
        var resultCreateLedger = await _createLedgerHandler.Handle(requestCreateLedger, CancellationToken.None);
        var request = new CreateLedgerRequest(LedgerType.CardanoPreprod);

        // Act
        var result = await _createLedgerHandler.Handle(request, CancellationToken.None);
        var repeatedResult = await _createLedgerHandler.Handle(request, CancellationToken.None);

        // Assert
        Assert.True((bool)result.IsSuccess);
        Assert.True((bool)repeatedResult.IsSuccess);
    }
}