namespace OpenPrismNode.Sync.Tests.ParseTransaction.ArtificialData;

using FluentResults.Extensions.FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using OpenPrismNode.Core.Crypto;
using OpenPrismNode.Core.Models;
using OpenPrismNode.Sync.Commands.ParseTransaction;

public class ProtocolUpdateTransaction
{
    private ParseTransactionHandler _parseTransactionHandler;
    private readonly ISha256Service _sha256Service;
    private readonly IEcService _ecService;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly ILogger<ParseTransactionHandler> _logger;

    public ProtocolUpdateTransaction()
    {
        _mediatorMock = new Mock<IMediator>();
        _sha256Service = new Sha256ServiceBouncyCastle();
        _ecService = new EcServiceBouncyCastle();
        _logger = new Mock<ILogger<ParseTransactionHandler>>().Object;
    }

    [Fact]
    public async Task ProtocolVersionUpdate_TransactionHandler_succeds_for_well_constructed_request()
    {
        // Arrange
        var mockedEcService = new Mock<IEcService>();
        mockedEcService.Setup(p => p.VerifyData(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(true);

        var parseTransactionRequest = new ParseTransactionRequest(
            new SignedAtalaOperation
            {
                Operation = new AtalaOperation
                {
                    ProtocolVersionUpdate = new ProtocolVersionUpdateOperation()
                    {
                        Version = new ProtocolVersionInfo()
                        {
                            EffectiveSince = 123,
                            ProtocolVersion = new ProtocolVersion()
                            {
                                MajorVersion = 1,
                                MinorVersion = 2
                            },
                            VersionName = "someVersionName"
                        }
                    }
                },
                SignedWith = "master0",
                Signature = PrismEncoding.Utf8StringToByteString("someSignature")
            },
            0,
            resolveMode: new ResolveMode(ParserResolveMode.NoResolveNoSignatureVerification)
        );

        // Act
        _parseTransactionHandler = new ParseTransactionHandler(_mediatorMock.Object, _sha256Service, mockedEcService.Object, _logger);
        var result = await _parseTransactionHandler.Handle(parseTransactionRequest, CancellationToken.None);

        // Assert
        result.Should().BeSuccess();
    }
    
    [Fact]
    public async Task ProtocolVersionUpdate_TransactionHandler_fails_if_effectiveSince_is_missing()
    {
        // Arrange
        var mockedEcService = new Mock<IEcService>();
        mockedEcService.Setup(p => p.VerifyData(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(true);

        var parseTransactionRequest = new ParseTransactionRequest(
            new SignedAtalaOperation
            {
                Operation = new AtalaOperation
                {
                    ProtocolVersionUpdate = new ProtocolVersionUpdateOperation()
                    {
                        Version = new ProtocolVersionInfo()
                        {
                            ProtocolVersion = new ProtocolVersion()
                            {
                                MajorVersion = 1,
                                MinorVersion = 2
                            },
                            VersionName = "someVersionName"
                        }
                    }
                },
                SignedWith = "master0",
                Signature = PrismEncoding.Utf8StringToByteString("someSignature")
            },
            0,
            resolveMode: new ResolveMode(ParserResolveMode.NoResolveNoSignatureVerification)
        );

        // Act
        _parseTransactionHandler = new ParseTransactionHandler(_mediatorMock.Object, _sha256Service, mockedEcService.Object, _logger);
        var result = await _parseTransactionHandler.Handle(parseTransactionRequest, CancellationToken.None);

        // Assert
        result.Should().BeFailure().And.Match(n => n.Errors.FirstOrDefault().Message.Contains("Invalid protocol version update: The effectiveSince block must be greater than 0."));

    }
}