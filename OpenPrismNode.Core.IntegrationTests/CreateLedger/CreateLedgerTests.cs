using OpenPrismNode.Core.Commands.CreateLedger;
using OpenPrismNode.Core.Models;


public partial class IntegrationTests
{
    [Fact]
    public async Task CreateLedger_succeeds_for_default_case()
    {
        // Arrange
        var request = new CreateLedgerRequest(LedgerType.CardanoPreprod);

        // Act
        var result = await _createLedgerHandler.Handle(request, CancellationToken.None);

        // Assert
        Assert.True((bool)result.IsSuccess);
    }
    
    [Fact]
    public async Task CreateLedger_succeeds_for_already_created_ledger()
    {
        // Arrange
        var request = new CreateLedgerRequest(LedgerType.CardanoPreprod);

        // Act
        var result = await _createLedgerHandler.Handle(request, CancellationToken.None);
        var repeatedResult = await _createLedgerHandler.Handle(request, CancellationToken.None);

        // Assert
        Assert.True((bool)result.IsSuccess);
        Assert.True((bool)repeatedResult.IsSuccess);
    }
}