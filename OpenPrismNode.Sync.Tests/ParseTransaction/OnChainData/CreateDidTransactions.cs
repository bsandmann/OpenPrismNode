namespace OpenPrismNode.Sync.Tests.ParseTransaction.OnChainData;

using FluentAssertions;
using FluentResults.Extensions.FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using OpenPrismNode.Core.Crypto;
using OpenPrismNode.Core.Models;
using OpenPrismNode.Sync.Commands.DecodeTransaction;
using OpenPrismNode.Sync.Commands.ParseTransaction;
using OpenPrismNode.Sync.Tests.TestDocuments;

public class CreateDidTransactions
{
    private ParseTransactionHandler _parseTransactionHandler;
    private readonly ISha256Service _sha256Service;
    private readonly IEcService _ecService;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly ILogger<ParseTransactionHandler> _logger;

    public CreateDidTransactions()
    {
        _mediatorMock = new Mock<IMediator>();
        _sha256Service = new Sha256ServiceBouncyCastle();
        _ecService = new EcServiceBouncyCastle();
        _logger = new Mock<ILogger<ParseTransactionHandler>>().Object;
    }

    [Fact]
    public async Task CreateDid_Operation_parsed_without_services()
    {
        // Arrange
        var serializedTransaction = TransactionSampleData.PrismV2_CreateDid_Transaction;
        var decodeTransactionRequest = new DecodeTransactionRequest(serializedTransaction);
        var handler = new DecodeTransactionHandler();

        // Act
        var decodedResult = await handler.Handle(decodeTransactionRequest, new CancellationToken());
        var mockedEcService = new Mock<IEcService>();
        mockedEcService.Setup(p => p.VerifyData(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(true);

        var parseTransactionRequest = new ParseTransactionRequest(
            decodedResult.Value.Single(),
            LedgerType.UnknownLedger,
            0
        );

        // Act
        _parseTransactionHandler = new ParseTransactionHandler(_mediatorMock.Object, _sha256Service, mockedEcService.Object, _logger);
        var result = await _parseTransactionHandler.Handle(parseTransactionRequest, CancellationToken.None);

        // Assert
        result.Should().BeSuccess();
        result.Value.AsCreateDid().didDocument.Contexts.Should().BeEmpty();
        result.Value.AsCreateDid().didDocument.DidIdentifier.Should().Be("8116dd43298d87b622fbc8e198f93367af6121cc356d8223938be902d176a505");
        result.Value.AsCreateDid().didDocument.PublicKeys.Count.Should().Be(3);
        result.Value.AsCreateDid().didDocument.PublicKeys[0].KeyId.Should().Be("key1");
        result.Value.AsCreateDid().didDocument.PublicKeys[0].KeyUsage.Should().Be(PrismKeyUsage.AuthenticationKey);
        result.Value.AsCreateDid().didDocument.PublicKeys[0].Curve.Should().Be("secp256k1");
        result.Value.AsCreateDid().didDocument.PublicKeys[0].Hex.Should().Be("04f84061c89dd21df4706244cae7f40ec97dd7ffd7d783d8eb32ebaba7d14e907ae51d4f976d3a45fcba2c02532185381e4176cacc5336d1c7c5cbf52276625634");
        result.Value.AsCreateDid().didDocument.PublicKeys[0].KeyX.Should().NotBeEmpty();
        result.Value.AsCreateDid().didDocument.PublicKeys[0].KeyY.Should().NotBeEmpty();
        result.Value.AsCreateDid().didDocument.PublicKeys[0].LongByteArray.Should().NotBeEmpty();
        result.Value.AsCreateDid().didDocument.PublicKeys[1].KeyId.Should().Be("key2");
        result.Value.AsCreateDid().didDocument.PublicKeys[1].KeyUsage.Should().Be(PrismKeyUsage.IssuingKey);
        result.Value.AsCreateDid().didDocument.PublicKeys[1].Curve.Should().Be("secp256k1");
        result.Value.AsCreateDid().didDocument.PublicKeys[1].Hex.Should().Be("04b1658ba508309a371ec333fedfa034894979734ce7ea47274b02804168258f6f5aeb7cf8cb4a82e5567453e728e251987f9a0747ef13976eaa64c4b451f9918c");
        result.Value.AsCreateDid().didDocument.PublicKeys[1].KeyX.Should().NotBeEmpty();
        result.Value.AsCreateDid().didDocument.PublicKeys[1].KeyY.Should().NotBeEmpty();
        result.Value.AsCreateDid().didDocument.PublicKeys[1].LongByteArray.Should().NotBeEmpty();
        result.Value.AsCreateDid().didDocument.PublicKeys[2].KeyId.Should().Be("master0");
        result.Value.AsCreateDid().didDocument.PublicKeys[2].KeyUsage.Should().Be(PrismKeyUsage.MasterKey);
        result.Value.AsCreateDid().didDocument.PublicKeys[2].Curve.Should().Be("secp256k1");
        result.Value.AsCreateDid().didDocument.PublicKeys[2].Hex.Should().Be("04e894412721c7a42a01a20f1a6edf58b3f8c6ee3d221512b1742012eaae1f7d4f3c0287be76afc4e1d5cec2795c331d013f764681cf560ad5eaf84b3d5d4958ea");
        result.Value.AsCreateDid().didDocument.PublicKeys[2].KeyX.Should().NotBeEmpty();
        result.Value.AsCreateDid().didDocument.PublicKeys[2].KeyY.Should().NotBeEmpty();
        result.Value.AsCreateDid().didDocument.PublicKeys[2].LongByteArray.Should().NotBeEmpty();
        result.Value.AsCreateDid().didDocument.PrismServices.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateDid_Operation_parsed_with_services()
    {
        // Arrange
        var serializedTransaction = TransactionSampleData.PrismV2_CreateDid_Transaction_with_services;
        var decodeTransactionRequest = new DecodeTransactionRequest(serializedTransaction);
        var handler = new DecodeTransactionHandler();

        // Act
        var decodedResult = await handler.Handle(decodeTransactionRequest, new CancellationToken());
        var mockedEcService = new Mock<IEcService>();
        mockedEcService.Setup(p => p.VerifyData(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(true);

        var parseTransactionRequest = new ParseTransactionRequest(
            decodedResult.Value.Single(),
            LedgerType.UnknownLedger,
            0
        );

        // Act
        _parseTransactionHandler = new ParseTransactionHandler(_mediatorMock.Object, _sha256Service, mockedEcService.Object, _logger);
        var result = await _parseTransactionHandler.Handle(parseTransactionRequest, CancellationToken.None);

        // Assert
        result.Should().BeSuccess();
        result.Value.AsCreateDid().didDocument.Contexts.Should().BeEmpty();
        result.Value.AsCreateDid().didDocument.DidIdentifier.Should().Be("7818333872399d98d2264aa07a27f0e49c5446cc0aeff337c842fb8a48944fc0");
        result.Value.AsCreateDid().didDocument.PublicKeys.Count.Should().Be(3);
        result.Value.AsCreateDid().didDocument.PublicKeys[0].KeyId.Should().Be("key-1");
        result.Value.AsCreateDid().didDocument.PublicKeys[0].KeyUsage.Should().Be(PrismKeyUsage.AuthenticationKey);
        result.Value.AsCreateDid().didDocument.PublicKeys[0].Curve.Should().Be("secp256k1");
        result.Value.AsCreateDid().didDocument.PublicKeys[0].Hex.Should().Be("04447a1746ccc1032b8471f3906b9b1ab507aa6ec6d2f25903a82ccce3d98a27510fda3e44cf13ca257200711cab40b603d012cc49ab15a20e58f6411c136a3cbe");
        result.Value.AsCreateDid().didDocument.PublicKeys[0].KeyX.Should().NotBeEmpty();
        result.Value.AsCreateDid().didDocument.PublicKeys[0].KeyY.Should().NotBeEmpty();
        result.Value.AsCreateDid().didDocument.PublicKeys[0].LongByteArray.Should().NotBeEmpty();
        result.Value.AsCreateDid().didDocument.PublicKeys[1].KeyId.Should().Be("key-2");
        result.Value.AsCreateDid().didDocument.PublicKeys[1].KeyUsage.Should().Be(PrismKeyUsage.IssuingKey);
        result.Value.AsCreateDid().didDocument.PublicKeys[1].Curve.Should().Be("secp256k1");
        result.Value.AsCreateDid().didDocument.PublicKeys[1].Hex.Should().Be("04b07d9cade268680a502e3d20aacb79bd283ab2cf8b5703d3cc5e7be9626bcb7ba95617dfdd20a6a6f27de84d1662683a70da8e975ebaffd037136f94ecbb30ef");
        result.Value.AsCreateDid().didDocument.PublicKeys[1].KeyX.Should().NotBeEmpty();
        result.Value.AsCreateDid().didDocument.PublicKeys[1].KeyY.Should().NotBeEmpty();
        result.Value.AsCreateDid().didDocument.PublicKeys[1].LongByteArray.Should().NotBeEmpty();
        result.Value.AsCreateDid().didDocument.PublicKeys[2].KeyId.Should().Be("master0");
        result.Value.AsCreateDid().didDocument.PublicKeys[2].KeyUsage.Should().Be(PrismKeyUsage.MasterKey);
        result.Value.AsCreateDid().didDocument.PublicKeys[2].Curve.Should().Be("secp256k1");
        result.Value.AsCreateDid().didDocument.PublicKeys[2].Hex.Should().Be("04888a4ccec8d6309d3c2e4adcb046b7808608660c69340968a927013aa7b2002c3339a18dc79a07f45c7a21a22d2afbfe8a02edc4577a013196a1c7a197305484");
        result.Value.AsCreateDid().didDocument.PublicKeys[2].KeyX.Should().NotBeEmpty();
        result.Value.AsCreateDid().didDocument.PublicKeys[2].KeyY.Should().NotBeEmpty();
        result.Value.AsCreateDid().didDocument.PublicKeys[2].LongByteArray.Should().NotBeEmpty();
        result.Value.AsCreateDid().didDocument.PrismServices.Count.Should().Be(1);
        result.Value.AsCreateDid().didDocument.PrismServices[0].ServiceId.Should().Be("service-1");
        result.Value.AsCreateDid().didDocument.PrismServices[0].Type.Should().Be("LinkedDomains");
        result.Value.AsCreateDid().didDocument.PrismServices[0].ServiceEndpoints.Should().NotBeNull();
        result.Value.AsCreateDid().didDocument.PrismServices[0].ServiceEndpoints.Json.Should().BeNull();
        result.Value.AsCreateDid().didDocument.PrismServices[0].ServiceEndpoints.Uri.Should().BeNull();
        result.Value.AsCreateDid().didDocument.PrismServices[0].ServiceEndpoints.ListOfUris.Should().NotBeNull();
        result.Value.AsCreateDid().didDocument.PrismServices[0].ServiceEndpoints.ListOfUris!.Count.Should().Be(1);
        result.Value.AsCreateDid().didDocument.PrismServices[0].ServiceEndpoints.ListOfUris[0]!.AbsoluteUri.Should().Be(new Uri("https://m4.csign.io/").AbsoluteUri);
    }
    
     [Fact]
    public async Task CreateDid_Operation_parsed_with_problematic_services()
    {
        // Arrange
        var serializedTransaction = TransactionSampleData.PrismV2_CreateDid_Transaction_with_problematic_services;
        var decodeTransactionRequest = new DecodeTransactionRequest(serializedTransaction);
        var handler = new DecodeTransactionHandler();

        // Act
        var decodedResult = await handler.Handle(decodeTransactionRequest, new CancellationToken());
        var mockedEcService = new Mock<IEcService>();
        mockedEcService.Setup(p => p.VerifyData(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(true);

        var parseTransactionRequest = new ParseTransactionRequest(
            decodedResult.Value.Single(),
            LedgerType.UnknownLedger,
            0
        );

        // Act
        _parseTransactionHandler = new ParseTransactionHandler(_mediatorMock.Object, _sha256Service, mockedEcService.Object, _logger);
        var result = await _parseTransactionHandler.Handle(parseTransactionRequest, CancellationToken.None);

        // Assert
        result.Should().BeFailure();
        result.Errors.Single().Message.Should().StartWith("Invalid ServiceEndpointUri: Could not parse as Uri or List of Uris:");
    }
    
     [Fact]
    public async Task CreateDid_Operation_parsed_with_ED25519()
    {
        // Arrange
        var serializedTransaction = TransactionSampleData.PrismV2_With_ED25519;
        var decodeTransactionRequest = new DecodeTransactionRequest(serializedTransaction);
        var handler = new DecodeTransactionHandler();

        // Act
        var decodedResult = await handler.Handle(decodeTransactionRequest, new CancellationToken());
        var mockedEcService = new Mock<IEcService>();
        mockedEcService.Setup(p => p.VerifyData(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(true);

        var parseTransactionRequest = new ParseTransactionRequest(
            decodedResult.Value.First(),
            LedgerType.UnknownLedger,
            0
        );

        // Act
        _parseTransactionHandler = new ParseTransactionHandler(_mediatorMock.Object, _sha256Service, mockedEcService.Object, _logger);
        var result = await _parseTransactionHandler.Handle(parseTransactionRequest, CancellationToken.None);

        // Assert
        result.Should().BeSuccess();
        result.Value.AsCreateDid().didDocument.Contexts.Should().BeEmpty();
        result.Value.AsCreateDid().didDocument.DidIdentifier.Should().Be("9ecca3f8981ab8baa7aa0d8a9a98d5f648615a1ef668d3037ab84ad6d5dd5d0a");
        result.Value.AsCreateDid().didDocument.PublicKeys.Count.Should().Be(3);
        result.Value.AsCreateDid().didDocument.PublicKeys[0].KeyId.Should().Be("my-key-authentication");
        result.Value.AsCreateDid().didDocument.PublicKeys[0].KeyUsage.Should().Be(PrismKeyUsage.AuthenticationKey);
        result.Value.AsCreateDid().didDocument.PublicKeys[0].Curve.Should().Be("Ed25519");
        result.Value.AsCreateDid().didDocument.PublicKeys[0].Hex.Should().Be("04df87ffdaeff525513b9e0ff5312b050b900b87d46753930bf23da4a6dd5d08e8");
        result.Value.AsCreateDid().didDocument.PublicKeys[0].KeyX.Should().NotBeEmpty();
        result.Value.AsCreateDid().didDocument.PublicKeys[0].KeyY.Should().BeNull();
        result.Value.AsCreateDid().didDocument.PublicKeys[0].LongByteArray.Should().NotBeEmpty();
        result.Value.AsCreateDid().didDocument.PublicKeys[1].KeyId.Should().Be("my-key-assertionMethod");
        result.Value.AsCreateDid().didDocument.PublicKeys[1].KeyUsage.Should().Be(PrismKeyUsage.IssuingKey);
        result.Value.AsCreateDid().didDocument.PublicKeys[1].Curve.Should().Be("Ed25519");
        result.Value.AsCreateDid().didDocument.PublicKeys[1].Hex.Should().Be("04cbb9f14e5e7821059a3af3b930c69e485699d154695f59f41e9ae5e7235fa118");
        result.Value.AsCreateDid().didDocument.PublicKeys[1].KeyX.Should().NotBeEmpty();
        result.Value.AsCreateDid().didDocument.PublicKeys[1].KeyY.Should().BeNull();
        result.Value.AsCreateDid().didDocument.PublicKeys[1].LongByteArray.Should().NotBeEmpty();
        result.Value.AsCreateDid().didDocument.PublicKeys[2].KeyId.Should().Be("master0");
        result.Value.AsCreateDid().didDocument.PublicKeys[2].KeyUsage.Should().Be(PrismKeyUsage.MasterKey);
        result.Value.AsCreateDid().didDocument.PublicKeys[2].Curve.Should().Be("secp256k1");
        result.Value.AsCreateDid().didDocument.PublicKeys[2].Hex.Should().Be("04058f73950ee81e219ec3939cbb3a2934f5b5069694c741917e60884418a612d3a4f2fad10090c040f9993040580e816138d78c0769c0c7bb42c3769ea766b242");
        result.Value.AsCreateDid().didDocument.PublicKeys[2].KeyX.Should().NotBeEmpty();
        result.Value.AsCreateDid().didDocument.PublicKeys[2].KeyY.Should().NotBeEmpty();
        result.Value.AsCreateDid().didDocument.PublicKeys[2].LongByteArray.Should().NotBeEmpty();
        result.Value.AsCreateDid().didDocument.PrismServices.Should().BeEmpty();
    }
    
      [Fact]
    public async Task CreateDid_Operation_parsed_with_ED25519_and_X25519()
    {
        // Arrange
        var serializedTransaction = TransactionSampleData.PrismV2_With_X25519;
        var decodeTransactionRequest = new DecodeTransactionRequest(serializedTransaction);
        var handler = new DecodeTransactionHandler();

        // Act
        var decodedResult = await handler.Handle(decodeTransactionRequest, new CancellationToken());
        var mockedEcService = new Mock<IEcService>();
        mockedEcService.Setup(p => p.VerifyData(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(true);

        var parseTransactionRequest = new ParseTransactionRequest(
            decodedResult.Value.First(),
            LedgerType.UnknownLedger,
            0
        );

        // Act
        _parseTransactionHandler = new ParseTransactionHandler(_mediatorMock.Object, _sha256Service, mockedEcService.Object, _logger);
        var result = await _parseTransactionHandler.Handle(parseTransactionRequest, CancellationToken.None);

        // Assert
        result.Should().BeSuccess();
        result.Value.AsCreateDid().didDocument.Contexts.Should().BeEmpty();
        result.Value.AsCreateDid().didDocument.DidIdentifier.Should().Be("2f67ab9d78f808b6f6403b102961b2698f8fbb119404e9ee458a2a81101ccb82");
        result.Value.AsCreateDid().didDocument.PublicKeys.Count.Should().Be(3);
        result.Value.AsCreateDid().didDocument.PublicKeys[0].KeyId.Should().Be("my-key-authentication");
        result.Value.AsCreateDid().didDocument.PublicKeys[0].KeyUsage.Should().Be(PrismKeyUsage.AuthenticationKey);
        result.Value.AsCreateDid().didDocument.PublicKeys[0].Curve.Should().Be("Ed25519");
        result.Value.AsCreateDid().didDocument.PublicKeys[0].Hex.Should().Be("046c6cd7c82333729b6dce28f488ef2fd5c09935fd91ab12b22a27bbac4476d9a5");
        result.Value.AsCreateDid().didDocument.PublicKeys[0].KeyX.Should().NotBeEmpty();
        result.Value.AsCreateDid().didDocument.PublicKeys[0].KeyY.Should().BeNull();
        result.Value.AsCreateDid().didDocument.PublicKeys[0].LongByteArray.Should().NotBeEmpty();
        result.Value.AsCreateDid().didDocument.PublicKeys[1].KeyId.Should().Be("my-key-agreement");
        result.Value.AsCreateDid().didDocument.PublicKeys[1].KeyUsage.Should().Be(PrismKeyUsage.KeyAgreementKey);
        result.Value.AsCreateDid().didDocument.PublicKeys[1].Curve.Should().Be("X25519");
        result.Value.AsCreateDid().didDocument.PublicKeys[1].Hex.Should().Be("0474acac7c81a1000924590170a449f5e556b03696e60a7f9db55c768430d85f00");
        result.Value.AsCreateDid().didDocument.PublicKeys[1].KeyX.Should().NotBeEmpty();
        result.Value.AsCreateDid().didDocument.PublicKeys[1].KeyY.Should().BeNull();
        result.Value.AsCreateDid().didDocument.PublicKeys[1].LongByteArray.Should().NotBeEmpty();
        result.Value.AsCreateDid().didDocument.PublicKeys[2].KeyId.Should().Be("master0");
        result.Value.AsCreateDid().didDocument.PublicKeys[2].KeyUsage.Should().Be(PrismKeyUsage.MasterKey);
        result.Value.AsCreateDid().didDocument.PublicKeys[2].Curve.Should().Be("secp256k1");
        result.Value.AsCreateDid().didDocument.PublicKeys[2].Hex.Should().Be("04f3ea04017727027334c06537d329dfb2bc6117dc0132c32ba2b75b4126d032935009a2efaec29083c22daccdd6a0f3052a918cb820f2a74f9b1832f084f6e09d");
        result.Value.AsCreateDid().didDocument.PublicKeys[2].KeyX.Should().NotBeEmpty();
        result.Value.AsCreateDid().didDocument.PublicKeys[2].KeyY.Should().NotBeEmpty();
        result.Value.AsCreateDid().didDocument.PublicKeys[2].LongByteArray.Should().NotBeEmpty();
        result.Value.AsCreateDid().didDocument.PrismServices.Should().BeEmpty();
    }
}