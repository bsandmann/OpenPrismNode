namespace OpenPrismNode.Sync.Tests.ParseTransaction.OnChainData;

using Commands.DecodeTransaction;
using Core.Models;
using FluentAssertions;
using FluentResults.Extensions.FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using OpenPrismNode.Core.Crypto;
using OpenPrismNode.Sync.Commands.ParseTransaction;
using TestDocuments;

public class DeactivateDidTransactions
{
    private ParseTransactionHandler _parseTransactionHandler;
    private readonly ISha256Service _sha256Service;
    private readonly IEcService _ecService;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly ILogger<ParseTransactionHandler> _logger;

    public DeactivateDidTransactions()
    {
        _mediatorMock = new Mock<IMediator>();
        _sha256Service = new Sha256ServiceBouncyCastle();
        _ecService = new EcServiceBouncyCastle();
        _logger = new Mock<ILogger<ParseTransactionHandler>>().Object;
    }

    [Fact]
    public async Task Deactivate_Operation()
    {
        // Arrange
        var serializedTransaction = TransactionSampleData.PrismV2_DeactivateDid_Transaction;
        var decodeTransactionRequest = new DecodeTransactionRequest(serializedTransaction);
        var handler = new DecodeTransactionHandler();

        // Act
        var decodedResult = await handler.Handle(decodeTransactionRequest, new CancellationToken());
        var mockedEcService = new Mock<IEcService>();
        mockedEcService.Setup(p => p.VerifyData(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(true);

        var parseTransactionRequest = new ParseTransactionRequest(
            decodedResult.Value.Single(),
            0
        );

        // Act
        _parseTransactionHandler = new ParseTransactionHandler(_mediatorMock.Object, _sha256Service, mockedEcService.Object, _logger);
        var result = await _parseTransactionHandler.Handle(parseTransactionRequest, CancellationToken.None);

        // Assert
        result.Should().BeSuccess();
        result.Value.AsDeactivateDid().deactivatedDid.Should().Be("7af5a9f0c36ace08f5885aa069721003cde040b39c7f3c20d8fa4d87273d38cd");
    }
}