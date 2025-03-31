namespace OpenPrismNode.Sync.Tests.ParseTransaction.ArtificialData;

using Core.Commands.ResolveDid;
using Core.Common;
using FluentResults;
using FluentResults.Extensions.FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using OpenPrismNode.Core.Crypto;
using OpenPrismNode.Core.Models;
using OpenPrismNode.Sync.Commands.ParseTransaction;

public class DeactivateDidTransaction
{
    private ParseTransactionHandler _parseTransactionHandler;
    private readonly ISha256Service _sha256Service;
    private readonly ICryptoService _cryptoService;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly ILogger<ParseTransactionHandler> _logger;

    public DeactivateDidTransaction()
    {
        _mediatorMock = new Mock<IMediator>();
        _sha256Service = new Sha256ServiceBouncyCastle();
        _cryptoService = new CryptoServiceBouncyCastle();
        _logger = new Mock<ILogger<ParseTransactionHandler>>().Object;
    }

    [Fact]
    public async Task DeactivateDid_TransactionHandler_succeeds_for_well_constructed_request()
    {
        // Arrange
        var mockedEcService = new Mock<ICryptoService>();
        mockedEcService.Setup(p => p.VerifyDataSecp256k1(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(true);
        var previousOperationHash = PrismEncoding.Utf8StringToByteString("previousOperationHash");

        var parseTransactionRequest = new ParseTransactionRequest(
            new SignedAtalaOperation
            {
                Operation = new AtalaOperation
                {
                    DeactivateDid = new DeactivateDIDOperation()
                    {
                        PreviousOperationHash = previousOperationHash,
                        Id = "someId",
                    }
                },
                SignedWith = "master0",
                Signature = PrismEncoding.Utf8StringToByteString("someSignature")
            },
            LedgerType.UnknownLedger,
            0,
            resolveMode: new ResolveMode(0, 0, 0)
        );

        _mediatorMock.Setup(p => p.Send(It.IsAny<ResolveDidRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Result.Ok(new ResolveDidResponse(new InternalDidDocument("did:prism:someDid", new List<PrismPublicKey>()
            {
                new PrismPublicKey(PrismKeyUsage.MasterKey, "master0", "secp256k1", new byte[32], new byte[32]),
            }, new List<PrismService>(), new List<string>(), DateTime.UtcNow, String.Empty, 0,0,String.Empty), Hash.CreateFrom(previousOperationHash.ToByteArray())))));

        // Act
        _parseTransactionHandler = new ParseTransactionHandler(_mediatorMock.Object, _sha256Service, mockedEcService.Object, _logger);
        var result = await _parseTransactionHandler.Handle(parseTransactionRequest, CancellationToken.None);

        // Assert
        result.Should().BeSuccess();
    }

    [Fact]
    public async Task DeactivateDid_TransactionHandler_fails_with_missing_previous_OperationHash()
    {
        // Arrange
        var mockedEcService = new Mock<ICryptoService>();
        mockedEcService.Setup(p => p.VerifyDataSecp256k1(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(true);

        var parseTransactionRequest = new ParseTransactionRequest(
            new SignedAtalaOperation
            {
                Operation = new AtalaOperation
                {
                    DeactivateDid = new DeactivateDIDOperation()
                    {
                        Id = "someId",
                    }
                },
                SignedWith = "master0",
                Signature = PrismEncoding.Utf8StringToByteString("someSignature")
            },
            LedgerType.UnknownLedger,
            0,
            resolveMode: new ResolveMode(0, 0, 0)
        );

        // Act
        _parseTransactionHandler = new ParseTransactionHandler(_mediatorMock.Object, _sha256Service, mockedEcService.Object, _logger);
        var result = await _parseTransactionHandler.Handle(parseTransactionRequest, CancellationToken.None);

        // Assert
        result.Should().BeFailure().And.Match(n => n.Errors.FirstOrDefault().Message.Contains("nvalid previous operation hash"));
    }
}