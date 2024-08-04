namespace OpenPrismNode.Sync.Tests.ParseTransaction.ArtificialData;

using Core.Common;
using FluentResults.Extensions.FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using OpenPrismNode.Core.Crypto;
using OpenPrismNode.Core.Models;
using OpenPrismNode.Sync.Commands.ParseTransaction;

public class CreateDidTransaction
{
    private ParseTransactionHandler _parseTransactionHandler;
    private readonly ISha256Service _sha256Service;
    private readonly IEcService _ecService;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly ILogger<ParseTransactionHandler> _logger;

    public CreateDidTransaction()
    {
        _mediatorMock = new Mock<IMediator>();
        _sha256Service = new Sha256ServiceBouncyCastle();
        _ecService = new EcServiceBouncyCastle();
        _logger = new Mock<ILogger<ParseTransactionHandler>>().Object;
    }

    [Fact]
    public async Task CreateDid_TransactionHandler_succeeds_for_well_constructed_CreateDid()
    {
        // Arrange
        var publicKeyTestData = DeconstructExisitingDidForPublicKeys(TestDocuments.TransactionSampleData.PrismV2_LongForm_Did_with_Services_and_multipleKeys, KeyUsage.MasterKey);
        var mockedEcService = new Mock<IEcService>();
        mockedEcService.Setup(p => p.VerifyData(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(true);

        var parseTransactionRequest = new ParseTransactionRequest(
            new SignedAtalaOperation
            {
                Operation = new AtalaOperation
                {
                    CreateDid = new CreateDIDOperation()
                    {
                        DidData = new CreateDIDOperation.Types.DIDCreationData()
                        {
                            Context = { "myContext" },
                            PublicKeys =
                            {
                                new PublicKey()
                                {
                                    Id = "master0",
                                    Usage = KeyUsage.MasterKey,
                                    CompressedEcKeyData = new CompressedECKeyData()
                                    {
                                        Curve = "secp256k1",
                                        Data = publicKeyTestData.CompressedEcKeyData.Data
                                    }
                                },
                            },
                            Services = { new Service() { Id = "service0", Type = "myService", ServiceEndpoint = "http://myServiceEndpoint" } }
                        }
                    }
                },
                SignedWith = "master0",
                Signature = PrismEncoding.Utf8StringToByteString("someSignature")
            },
            0
        );

        // Act
        _parseTransactionHandler = new ParseTransactionHandler(_mediatorMock.Object, _sha256Service, mockedEcService.Object, _logger);
        var result = await _parseTransactionHandler.Handle(parseTransactionRequest, CancellationToken.None);

        // Assert
        result.Should().BeSuccess();
    }

    [Fact]
    public async Task CreateDid_TransactionHandler_fails_for_excessive_services()
    {
        // Arrange
        var publicKeyTestData = DeconstructExisitingDidForPublicKeys(TestDocuments.TransactionSampleData.PrismV2_LongForm_Did_with_Services_and_multipleKeys, KeyUsage.MasterKey);
        var mockedEcService = new Mock<IEcService>();
        mockedEcService.Setup(p => p.VerifyData(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(true);

        // Creating services list with exceeding number of services
        var servicesList = new List<Service>();
        for (int i = 0; i < 51; i++) // 51 > MaxServiceNumber (50)
        {
            servicesList.Add(new Service() { Id = $"service{i}", Type = "myService", ServiceEndpoint = $"http://myServiceEndpoint{i}" });
        }

        var parseTransactionRequest = new ParseTransactionRequest(
            new SignedAtalaOperation
            {
                Operation = new AtalaOperation
                {
                    CreateDid = new CreateDIDOperation()
                    {
                        DidData = new CreateDIDOperation.Types.DIDCreationData()
                        {
                            Context = { "myContext" },
                            PublicKeys =
                            {
                                new PublicKey()
                                {
                                    Id = "master0",
                                    Usage = KeyUsage.MasterKey,
                                    CompressedEcKeyData = new CompressedECKeyData()
                                    {
                                        Curve = "secp256k1",
                                        Data = publicKeyTestData.CompressedEcKeyData.Data
                                    }
                                },
                            },
                            Services = { servicesList }
                        }
                    }
                },
                SignedWith = "master0",
                Signature = PrismEncoding.Utf8StringToByteString("someSignature")
            },
            0
        );

        // Act
        _parseTransactionHandler = new ParseTransactionHandler(_mediatorMock.Object, _sha256Service, mockedEcService.Object, _logger);
        var result = await _parseTransactionHandler.Handle(parseTransactionRequest, CancellationToken.None);

        // Assert
        result.Should().BeFailure().And.Match(n => n.Errors.FirstOrDefault().Message.Contains("Service number exceeds"));
    }

    [Fact]
    public async Task CreateDid_TransactionHandler_fails_for_invalid_service_type()
    {
        // Arrange
        var publicKeyTestData = DeconstructExisitingDidForPublicKeys(TestDocuments.TransactionSampleData.PrismV2_LongForm_Did_with_Services_and_multipleKeys, KeyUsage.MasterKey);
        var mockedEcService = new Mock<IEcService>();
        mockedEcService.Setup(p => p.VerifyData(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(true);

        // Creating a service with type starting and ending with a whitespace
        var service = new Service() { Id = "service0", Type = " myService ", ServiceEndpoint = "http://myServiceEndpoint" };

        var parseTransactionRequest = new ParseTransactionRequest(
            new SignedAtalaOperation
            {
                Operation = new AtalaOperation
                {
                    CreateDid = new CreateDIDOperation()
                    {
                        DidData = new CreateDIDOperation.Types.DIDCreationData()
                        {
                            Context = { "myContext" },
                            PublicKeys =
                            {
                                new PublicKey()
                                {
                                    Id = "master0",
                                    Usage = KeyUsage.MasterKey,
                                    CompressedEcKeyData = new CompressedECKeyData()
                                    {
                                        Curve = "secp256k1",
                                        Data = publicKeyTestData.CompressedEcKeyData.Data
                                    }
                                },
                            },
                            Services = { service }
                        }
                    }
                },
                SignedWith = "master0",
                Signature = PrismEncoding.Utf8StringToByteString("someSignature")
            },
            0
        );

        // Act
        _parseTransactionHandler = new ParseTransactionHandler(_mediatorMock.Object, _sha256Service, mockedEcService.Object, _logger);
        var result = await _parseTransactionHandler.Handle(parseTransactionRequest, CancellationToken.None);

        // Assert
        result.Should().BeFailure().And.Match(n => n.Errors.FirstOrDefault().Message.Contains("Service type is not valid"));
    }

    [Fact]
    public async Task CreateDid_TransactionHandler_fails_for_invalid_service_type_exceeding_number_of_characters()
    {
        // Arrange
        var publicKeyTestData = DeconstructExisitingDidForPublicKeys(TestDocuments.TransactionSampleData.PrismV2_LongForm_Did_with_Services_and_multipleKeys, KeyUsage.MasterKey);
        var mockedEcService = new Mock<IEcService>();
        mockedEcService.Setup(p => p.VerifyData(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(true);

        // Creating a service with type starting and ending with a whitespace
        var service = new Service() { Id = "service0", Type = "myServiceDescriptionOfTheTypeWhichIsWayToLongToBeConsiderValidmyServiceDescriptionOfTheTypeWhichIsWayToLongToBeConsiderValid", ServiceEndpoint = "http://myServiceEndpoint" };

        var parseTransactionRequest = new ParseTransactionRequest(
            new SignedAtalaOperation
            {
                Operation = new AtalaOperation
                {
                    CreateDid = new CreateDIDOperation()
                    {
                        DidData = new CreateDIDOperation.Types.DIDCreationData()
                        {
                            Context = { "myContext" },
                            PublicKeys =
                            {
                                new PublicKey()
                                {
                                    Id = "master0",
                                    Usage = KeyUsage.MasterKey,
                                    CompressedEcKeyData = new CompressedECKeyData()
                                    {
                                        Curve = "secp256k1",
                                        Data = publicKeyTestData.CompressedEcKeyData.Data
                                    }
                                },
                            },
                            Services = { service }
                        }
                    }
                },
                SignedWith = "master0",
                Signature = PrismEncoding.Utf8StringToByteString("someSignature")
            },
            0
        );

        // Act
        _parseTransactionHandler = new ParseTransactionHandler(_mediatorMock.Object, _sha256Service, mockedEcService.Object, _logger);
        var result = await _parseTransactionHandler.Handle(parseTransactionRequest, CancellationToken.None);

        // Assert
        result.Should().BeFailure().And.Match(n => n.Errors.FirstOrDefault().Message.Contains("Service type is not valid"));
    }

    [Fact]
    public async Task CreateDid_TransactionHandler_fails_for_invalid_service_endpoint_which_is_not_a_uri()
    {
        // Arrange
        var publicKeyTestData = DeconstructExisitingDidForPublicKeys(TestDocuments.TransactionSampleData.PrismV2_LongForm_Did_with_Services_and_multipleKeys, KeyUsage.MasterKey);
        var mockedEcService = new Mock<IEcService>();
        mockedEcService.Setup(p => p.VerifyData(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(true);

        // Creating a service with type starting and ending with a whitespace
        var service = new Service() { Id = "service0", Type = "myServiceType", ServiceEndpoint = "notAnUri" };

        var parseTransactionRequest = new ParseTransactionRequest(
            new SignedAtalaOperation
            {
                Operation = new AtalaOperation
                {
                    CreateDid = new CreateDIDOperation()
                    {
                        DidData = new CreateDIDOperation.Types.DIDCreationData()
                        {
                            Context = { "myContext" },
                            PublicKeys =
                            {
                                new PublicKey()
                                {
                                    Id = "master0",
                                    Usage = KeyUsage.MasterKey,
                                    CompressedEcKeyData = new CompressedECKeyData()
                                    {
                                        Curve = "secp256k1",
                                        Data = publicKeyTestData.CompressedEcKeyData.Data
                                    }
                                },
                            },
                            Services = { service }
                        }
                    }
                },
                SignedWith = "master0",
                Signature = PrismEncoding.Utf8StringToByteString("someSignature")
            },
            0
        );

        // Act
        _parseTransactionHandler = new ParseTransactionHandler(_mediatorMock.Object, _sha256Service, mockedEcService.Object, _logger);
        var result = await _parseTransactionHandler.Handle(parseTransactionRequest, CancellationToken.None);

        // Assert
        result.Should().BeFailure().And.Match(n => n.Errors.FirstOrDefault().Message.Contains("Invalid ServiceEndpointUri"));
    }

    [Fact]
    public async Task CreateDid_TransactionHandler_succeeds_with_json_array_of_EndpointUris()
    {
        // Arrange
        var publicKeyTestData = DeconstructExisitingDidForPublicKeys(TestDocuments.TransactionSampleData.PrismV2_LongForm_Did_with_Services_and_multipleKeys, KeyUsage.MasterKey);
        var mockedEcService = new Mock<IEcService>();
        mockedEcService.Setup(p => p.VerifyData(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(true);

        // Creating a service with type starting and ending with a whitespace
        var service = new Service() { Id = "service0", Type = "myServiceType", ServiceEndpoint = """["https://someServiceEndpoint1.com", "https://someServiceEndpoint2.com"]""" };

        var parseTransactionRequest = new ParseTransactionRequest(
            new SignedAtalaOperation
            {
                Operation = new AtalaOperation
                {
                    CreateDid = new CreateDIDOperation()
                    {
                        DidData = new CreateDIDOperation.Types.DIDCreationData()
                        {
                            Context = { "myContext" },
                            PublicKeys =
                            {
                                new PublicKey()
                                {
                                    Id = "master0",
                                    Usage = KeyUsage.MasterKey,
                                    CompressedEcKeyData = new CompressedECKeyData()
                                    {
                                        Curve = "secp256k1",
                                        Data = publicKeyTestData.CompressedEcKeyData.Data
                                    }
                                },
                            },
                            Services = { service }
                        }
                    }
                },
                SignedWith = "master0",
                Signature = PrismEncoding.Utf8StringToByteString("someSignature")
            },
            0
        );

        // Act
        _parseTransactionHandler = new ParseTransactionHandler(_mediatorMock.Object, _sha256Service, mockedEcService.Object, _logger);
        var result = await _parseTransactionHandler.Handle(parseTransactionRequest, CancellationToken.None);

        // Assert
        result.Should().BeSuccess();
    }

    [Fact]
    public async Task CreateDid_TransactionHandler_fails_with_json_array_of_invalid_serviceEndpoint_uris()
    {
        // Arrange
        var publicKeyTestData = DeconstructExisitingDidForPublicKeys(TestDocuments.TransactionSampleData.PrismV2_LongForm_Did_with_Services_and_multipleKeys, KeyUsage.MasterKey);
        var mockedEcService = new Mock<IEcService>();
        mockedEcService.Setup(p => p.VerifyData(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(true);

        // Creating a service with type starting and ending with a whitespace
        var service = new Service() { Id = "service0", Type = "myServiceType", ServiceEndpoint = """["notAnUri", "notAnUri"]""" };

        var parseTransactionRequest = new ParseTransactionRequest(
            new SignedAtalaOperation
            {
                Operation = new AtalaOperation
                {
                    CreateDid = new CreateDIDOperation()
                    {
                        DidData = new CreateDIDOperation.Types.DIDCreationData()
                        {
                            Context = { "myContext" },
                            PublicKeys =
                            {
                                new PublicKey()
                                {
                                    Id = "master0",
                                    Usage = KeyUsage.MasterKey,
                                    CompressedEcKeyData = new CompressedECKeyData()
                                    {
                                        Curve = "secp256k1",
                                        Data = publicKeyTestData.CompressedEcKeyData.Data
                                    }
                                },
                            },
                            Services = { service }
                        }
                    }
                },
                SignedWith = "master0",
                Signature = PrismEncoding.Utf8StringToByteString("someSignature")
            },
            0
        );

        // Act
        _parseTransactionHandler = new ParseTransactionHandler(_mediatorMock.Object, _sha256Service, mockedEcService.Object, _logger);
        var result = await _parseTransactionHandler.Handle(parseTransactionRequest, CancellationToken.None);

        // Assert
        result.Should().BeFailure().And.Match(n => n.Errors.FirstOrDefault().Message.Contains("Invalid ServiceEndpointUri"));
    }

    [Fact]
    public async Task CreateDid_TransactionHandler_fails_with_json_array_of_invalid_serviceEndpoint_json()
    {
        // Arrange
        var publicKeyTestData = DeconstructExisitingDidForPublicKeys(TestDocuments.TransactionSampleData.PrismV2_LongForm_Did_with_Services_and_multipleKeys, KeyUsage.MasterKey);
        var mockedEcService = new Mock<IEcService>();
        mockedEcService.Setup(p => p.VerifyData(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(true);

        // Creating a service with type starting and ending with a whitespace
        var service = new Service() { Id = "service0", Type = "myServiceType", ServiceEndpoint = """{invalid}""" };

        var parseTransactionRequest = new ParseTransactionRequest(
            new SignedAtalaOperation
            {
                Operation = new AtalaOperation
                {
                    CreateDid = new CreateDIDOperation()
                    {
                        DidData = new CreateDIDOperation.Types.DIDCreationData()
                        {
                            Context = { "myContext" },
                            PublicKeys =
                            {
                                new PublicKey()
                                {
                                    Id = "master0",
                                    Usage = KeyUsage.MasterKey,
                                    CompressedEcKeyData = new CompressedECKeyData()
                                    {
                                        Curve = "secp256k1",
                                        Data = publicKeyTestData.CompressedEcKeyData.Data
                                    }
                                },
                            },
                            Services = { service }
                        }
                    }
                },
                SignedWith = "master0",
                Signature = PrismEncoding.Utf8StringToByteString("someSignature")
            },
            0
        );

        // Act
        _parseTransactionHandler = new ParseTransactionHandler(_mediatorMock.Object, _sha256Service, mockedEcService.Object, _logger);
        var result = await _parseTransactionHandler.Handle(parseTransactionRequest, CancellationToken.None);

        // Assert
        result.Should().BeFailure().And.Match(n => n.Errors.FirstOrDefault().Message.Contains("Invalid ServiceEndpointUri"));
    }

    [Fact]
    public async Task CreateDid_TransactionHandler_fails_with_serviceEndpoint_string_which_exceed_characterLimit()
    {
        // Arrange
        var publicKeyTestData = DeconstructExisitingDidForPublicKeys(TestDocuments.TransactionSampleData.PrismV2_LongForm_Did_with_Services_and_multipleKeys, KeyUsage.MasterKey);
        var mockedEcService = new Mock<IEcService>();
        mockedEcService.Setup(p => p.VerifyData(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(true);

        // Creating a service with type starting and ending with a whitespace
        var service = new Service()
        {
            Id = "service0", Type = "myServiceType",
            ServiceEndpoint =
                """["https://thisIsAVeryLongDomainNameWhichIsNotValid.com/andWhichAlsoContainsAnOverlyLongPathTo/SomeRessources/WhichAllInAllExceedTheLimitOfWhatIsAllowed/thisIsAVeryLongDomainNameWhichIsNotValid/andWhichAlsoContainsAnOverlyLongPathTo/SomeRessources/WhichAllInAllExceedTheLimitOfWhatIsAllowed/AllInAllThisSHouldBeLonderThan300CharactersAndThereforeFail"]"""
        };

        var parseTransactionRequest = new ParseTransactionRequest(
            new SignedAtalaOperation
            {
                Operation = new AtalaOperation
                {
                    CreateDid = new CreateDIDOperation()
                    {
                        DidData = new CreateDIDOperation.Types.DIDCreationData()
                        {
                            Context = { "myContext" },
                            PublicKeys =
                            {
                                new PublicKey()
                                {
                                    Id = "master0",
                                    Usage = KeyUsage.MasterKey,
                                    CompressedEcKeyData = new CompressedECKeyData()
                                    {
                                        Curve = "secp256k1",
                                        Data = publicKeyTestData.CompressedEcKeyData.Data
                                    }
                                },
                            },
                            Services = { service }
                        }
                    }
                },
                SignedWith = "master0",
                Signature = PrismEncoding.Utf8StringToByteString("someSignature")
            },
            0
        );

        // Act
        _parseTransactionHandler = new ParseTransactionHandler(_mediatorMock.Object, _sha256Service, mockedEcService.Object, _logger);
        var result = await _parseTransactionHandler.Handle(parseTransactionRequest, CancellationToken.None);

        // Assert
        result.Should().BeFailure().And.Match(n => n.Errors.FirstOrDefault().Message.Contains("Service endpoint is not valid. It must not exceed the maximum allowed size"));
    }

    [Fact]
    public async Task CreateDid_TransactionHandler_fails_with_missing_MasterKeyType()
    {
        // Arrange
        var publicKeyTestData = DeconstructExisitingDidForPublicKeys(TestDocuments.TransactionSampleData.PrismV2_LongForm_Did_with_Services_and_multipleKeys, KeyUsage.MasterKey);
        var mockedEcService = new Mock<IEcService>();
        mockedEcService.Setup(p => p.VerifyData(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(true);

        var parseTransactionRequest = new ParseTransactionRequest(
            new SignedAtalaOperation
            {
                Operation = new AtalaOperation
                {
                    CreateDid = new CreateDIDOperation()
                    {
                        DidData = new CreateDIDOperation.Types.DIDCreationData()
                        {
                            Context = { "myContext" },
                            PublicKeys =
                            {
                                new PublicKey()
                                {
                                    Id = "master0",
                                    Usage = KeyUsage.IssuingKey,
                                    CompressedEcKeyData = new CompressedECKeyData()
                                    {
                                        Curve = "secp256k1",
                                        Data = publicKeyTestData.CompressedEcKeyData.Data
                                    }
                                },
                            },
                            Services = { }
                        }
                    }
                },
                SignedWith = "master0",
                Signature = PrismEncoding.Utf8StringToByteString("someSignature")
            },
            0
        );

        // Act
        _parseTransactionHandler = new ParseTransactionHandler(_mediatorMock.Object, _sha256Service, mockedEcService.Object, _logger);
        var result = await _parseTransactionHandler.Handle(parseTransactionRequest, CancellationToken.None);

        // Assert
        result.Should().BeFailure().And.Match(n => n.Errors.FirstOrDefault().Message.Contains("No master key found in the createDid operation"));
    }

    [Fact]
    public async Task CreateDid_TransactionHandler_succeeds_with_MasterKey_Type_and_another_non_MasterKey_Type()
    {
        // Arrange
        var publicKeyTestData = DeconstructExisitingDidForPublicKeys(TestDocuments.TransactionSampleData.PrismV2_LongForm_Did_with_Services_and_multipleKeys, KeyUsage.MasterKey);
        var mockedEcService = new Mock<IEcService>();
        mockedEcService.Setup(p => p.VerifyData(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(true);

        var parseTransactionRequest = new ParseTransactionRequest(
            new SignedAtalaOperation
            {
                Operation = new AtalaOperation
                {
                    CreateDid = new CreateDIDOperation()
                    {
                        DidData = new CreateDIDOperation.Types.DIDCreationData()
                        {
                            Context = { "myContext" },
                            PublicKeys =
                            {
                                new PublicKey()
                                {
                                    Id = "myIssuingKey",
                                    Usage = KeyUsage.IssuingKey,
                                    CompressedEcKeyData = new CompressedECKeyData()
                                    {
                                        Curve = "secp256k1",
                                        Data = publicKeyTestData.CompressedEcKeyData.Data
                                    }
                                },
                                new PublicKey()
                                {
                                    Id = "myMasterKey",
                                    Usage = KeyUsage.MasterKey,
                                    CompressedEcKeyData = new CompressedECKeyData()
                                    {
                                        Curve = "secp256k1",
                                        Data = publicKeyTestData.CompressedEcKeyData.Data
                                    }
                                },
                            },
                            Services = { }
                        }
                    }
                },
                SignedWith = "myMasterKey",
                Signature = PrismEncoding.Utf8StringToByteString("someSignature")
            },
            0
        );

        // Act
        _parseTransactionHandler = new ParseTransactionHandler(_mediatorMock.Object, _sha256Service, mockedEcService.Object, _logger);
        var result = await _parseTransactionHandler.Handle(parseTransactionRequest, CancellationToken.None);

        // Assert
        result.Should().BeSuccess();
    }

    [Fact]
    public async Task CreateDid_TransactionHandler_fails_with_identical_named_keys()
    {
        // Arrange
        var publicKeyTestData = DeconstructExisitingDidForPublicKeys(TestDocuments.TransactionSampleData.PrismV2_LongForm_Did_with_Services_and_multipleKeys, KeyUsage.MasterKey);
        var mockedEcService = new Mock<IEcService>();
        mockedEcService.Setup(p => p.VerifyData(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(true);

        var parseTransactionRequest = new ParseTransactionRequest(
            new SignedAtalaOperation
            {
                Operation = new AtalaOperation
                {
                    CreateDid = new CreateDIDOperation()
                    {
                        DidData = new CreateDIDOperation.Types.DIDCreationData()
                        {
                            Context = { "myContext" },
                            PublicKeys =
                            {
                                new PublicKey()
                                {
                                    Id = "myMasterKey",
                                    Usage = KeyUsage.MasterKey,
                                    CompressedEcKeyData = new CompressedECKeyData()
                                    {
                                        Curve = "secp256k1",
                                        Data = publicKeyTestData.CompressedEcKeyData.Data
                                    }
                                },
                                new PublicKey()
                                {
                                    Id = "myIssuingKey",
                                    Usage = KeyUsage.IssuingKey,
                                    CompressedEcKeyData = new CompressedECKeyData()
                                    {
                                        Curve = "secp256k1",
                                        Data = publicKeyTestData.CompressedEcKeyData.Data
                                    }
                                },
                                new PublicKey()
                                {
                                    Id = "myIssuingKey",
                                    Usage = KeyUsage.AuthenticationKey,
                                    CompressedEcKeyData = new CompressedECKeyData()
                                    {
                                        Curve = "secp256k1",
                                        Data = publicKeyTestData.CompressedEcKeyData.Data
                                    }
                                },
                            },
                            Services = { }
                        }
                    }
                },
                SignedWith = "myMasterKey",
                Signature = PrismEncoding.Utf8StringToByteString("someSignature")
            },
            0
        );

        // Act
        _parseTransactionHandler = new ParseTransactionHandler(_mediatorMock.Object, _sha256Service, mockedEcService.Object, _logger);
        var result = await _parseTransactionHandler.Handle(parseTransactionRequest, CancellationToken.None);

        // Assert
        result.Should().BeFailure().And.Match(n => n.Errors.FirstOrDefault().Message.Contains("Duplicate key IDs detected. Each key ID must be unique"));
    }

    [Fact]
    public async Task CreateDid_TransactionHandler_fails_with_unkonwn_keyType()
    {
        // Arrange
        var publicKeyTestData = DeconstructExisitingDidForPublicKeys(TestDocuments.TransactionSampleData.PrismV2_LongForm_Did_with_Services_and_multipleKeys, KeyUsage.MasterKey);
        var mockedEcService = new Mock<IEcService>();
        mockedEcService.Setup(p => p.VerifyData(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(true);

        var parseTransactionRequest = new ParseTransactionRequest(
            new SignedAtalaOperation
            {
                Operation = new AtalaOperation
                {
                    CreateDid = new CreateDIDOperation()
                    {
                        DidData = new CreateDIDOperation.Types.DIDCreationData()
                        {
                            Context = { "myContext" },
                            PublicKeys =
                            {
                                new PublicKey()
                                {
                                    Id = "myUnknownKey",
                                    Usage = KeyUsage.UnknownKey,
                                    CompressedEcKeyData = new CompressedECKeyData()
                                    {
                                        Curve = "secp256k1",
                                        Data = publicKeyTestData.CompressedEcKeyData.Data
                                    }
                                },
                                new PublicKey()
                                {
                                    Id = "myMasterKey",
                                    Usage = KeyUsage.MasterKey,
                                    CompressedEcKeyData = new CompressedECKeyData()
                                    {
                                        Curve = "secp256k1",
                                        Data = publicKeyTestData.CompressedEcKeyData.Data
                                    }
                                },
                            },
                            Services = { }
                        }
                    }
                },
                SignedWith = "myMasterKey",
                Signature = PrismEncoding.Utf8StringToByteString("someSignature")
            },
            0
        );

        // Act
        _parseTransactionHandler = new ParseTransactionHandler(_mediatorMock.Object, _sha256Service, mockedEcService.Object, _logger);
        var result = await _parseTransactionHandler.Handle(parseTransactionRequest, CancellationToken.None);

        // Assert
        result.Should().BeFailure().And.Match(n => n.Errors.FirstOrDefault().Message.Contains("The UnknownKey is not a valid key usage."));
    }


    private PublicKey DeconstructExisitingDidForPublicKeys(string longform, KeyUsage keyUsage)
    {
        var bytesLongForm = PrismEncoding.Base64UrlDecode(longform);
        var byteString = PrismEncoding.ByteArrayToByteString(bytesLongForm);
        var atalaOperation = AtalaOperation.Parser.ParseFrom(byteString);
        var createDidOperation = atalaOperation.CreateDid;
        var publicKey = createDidOperation.DidData.PublicKeys.First(p => p.Usage == keyUsage);
        return publicKey;
    }
}