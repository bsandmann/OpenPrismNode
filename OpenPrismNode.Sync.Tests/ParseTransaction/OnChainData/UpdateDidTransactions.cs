namespace OpenPrismNode.Sync.Tests.ParseTransaction.OnChainData;

using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using OpenPrismNode.Core.Crypto;
using OpenPrismNode.Sync.Commands.ParseTransaction;

public class UpdateDidTransactions
{
    private ParseTransactionHandler _parseTransactionHandler;
    private readonly ISha256Service _sha256Service;
    private readonly IEcService _ecService;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly ILogger<ParseTransactionHandler> _logger;

    public UpdateDidTransactions()
    {
        _mediatorMock = new Mock<IMediator>();
        _sha256Service = new Sha256ServiceBouncyCastle();
        _ecService = new EcServiceBouncyCastle();
        _logger = new Mock<ILogger<ParseTransactionHandler>>().Object;
    }
   
    // TODO Adding single key
    // TODO Adding single key (master?)
    // TODO adding multiple keys
    // todo removing Key (not master)
    // todo removing Key (master) - fails
    // todo removing multiple keys (not master)
    // todo removing including keys (master) - fail
    // todo adding and removing key (not master)
    // todo adding and removing key (master)
    // todo adding services
    // todo removing services
    // todo updating services
    // todo adding and removing service in single operation - also fail scenarios
    // todo removing and adding service in single operation - also fail scenarios
    // todo patching the context
    
    
    //   [Fact]
    // public async Task UpdateDid_Operation_parsed_with_services()
    // {
    //     // Arrange
    //     var serializedTransaction = TransactionSampleData.PrismV2_UpdateDid_Transaction_updating_services;
    //     var decodeTransactionRequest = new DecodeTransactionRequest(serializedTransaction);
    //     var handler = new DecodeTransactionHandler();
    //     
    //     // Act
    //     var decodedResult = await handler.Handle(decodeTransactionRequest, new CancellationToken());
    //     var mockedEcService = new Mock<IEcService>();
    //     mockedEcService.Setup(p => p.VerifyData(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(true);
    //     
    //     var parseTransactionRequest = new ParseTransactionRequest(
    //         decodedResult.Value.Single(),
    //         0,
    //         resolveMode: new ResolveMode(ParserResolveMode.NoResolveNoSignatureVerication)
    //     );
    //     
    //     // Act
    //     _parseTransactionHandler = new ParseTransactionHandler(_mediatorMock.Object, _sha256Service, mockedEcService.Object, _logger);
    //     var result = await _parseTransactionHandler.Handle(parseTransactionRequest, CancellationToken.None);
    //     
    //     // Assert
    //     result.Should().BeSuccess();
    //     result.Value.AsUpdateDid().updateDidActionResults.Count.Should().Be(1);
    //     result.Value.AsUpdateDid().updateDidActionResults[0].UpdateDidActionType.Should().Be(UpdateDidActionType.UpdateService);
    //     result.Value.AsUpdateDid().updateDidActionResults[0].Contexts.Should().BeNull();
    //     result.Value.AsUpdateDid().updateDidActionResults[0].PrismPublicKey.Should().BeNull();
    //     result.Value.AsUpdateDid().updateDidActionResults[0].RemovedKeyId.Should().BeNull();
    //     result.Value.AsUpdateDid().updateDidActionResults[0].PrismService.Should().NotBeNull();
    //     result.Value.AsUpdateDid().updateDidActionResults[0].PrismService!.ServiceId.Should().Be("https://update.com");
    //     result.Value.AsUpdateDid().updateDidActionResults[0].PrismService!.Type.Should().Be("LinkedDomains");
    //     result.Value.AsUpdateDid().updateDidActionResults[0].PrismService!.PrismServiceEndpoints.Should().NotBeNull();
    //     result.Value.AsUpdateDid().updateDidActionResults[0].PrismService!.PrismServiceEndpoints.Uri.Should().BeNull();
    //     result.Value.AsUpdateDid().updateDidActionResults[0].PrismService!.PrismServiceEndpoints.Json.Should().BeNull();
    //     result.Value.AsUpdateDid().updateDidActionResults[0].PrismService!.PrismServiceEndpoints.ListOfUris.Should().NotBeNull();
    // }
}