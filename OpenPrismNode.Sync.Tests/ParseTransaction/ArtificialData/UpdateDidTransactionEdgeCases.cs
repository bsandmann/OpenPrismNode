namespace OpenPrismNode.Sync.Tests.ParseTransaction.ArtificialData;

using FluentResults.Extensions.FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using OpenPrismNode.Core.Crypto;
using OpenPrismNode.Core.Models;
using OpenPrismNode.Sync.Commands.ParseTransaction;

public class UpdateDidTransactionEdgeCases
{
    private ParseTransactionHandler _parseTransactionHandler;
    private readonly ISha256Service _sha256Service;
    private readonly IEcService _ecService;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly ILogger<ParseTransactionHandler> _logger;

    public UpdateDidTransactionEdgeCases()
    {
        _mediatorMock = new Mock<IMediator>();
        _sha256Service = new Sha256ServiceBouncyCastle();
        _ecService = new EcServiceBouncyCastle();
        _logger = new Mock<ILogger<ParseTransactionHandler>>().Object;
    }

    [Fact]
    public async Task UpdateDid_TransactionHandler_succeds_for_multiple_add_key_operations_for_different_Ids()
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
                    UpdateDid = new UpdateDIDOperation
                    {
                        Id = "did:prism:someDid",
                        PreviousOperationHash = PrismEncoding.Utf8StringToByteString("previousOperationHash"),
                        Actions =
                        {
                            new UpdateDIDAction()
                            {
                                AddKey = new AddKeyAction()
                                {
                                    Key = new PublicKey()
                                    {
                                        Id = "key-1",
                                        Usage = KeyUsage.IssuingKey,
                                        CompressedEcKeyData = new CompressedECKeyData()
                                        {
                                            Curve = "secp256k1",
                                            Data = publicKeyTestData.CompressedEcKeyData.Data
                                        }
                                    },
                                }
                            },
                            new UpdateDIDAction()
                            {
                                AddKey = new AddKeyAction()
                                {
                                    Key = new PublicKey()
                                    {
                                        Id = "key-2",
                                        Usage = KeyUsage.AuthenticationKey,
                                        CompressedEcKeyData = new CompressedECKeyData()
                                        {
                                            Curve = "secp256k1",
                                            Data = publicKeyTestData.CompressedEcKeyData.Data
                                        }
                                    },
                                }
                            },
                        },
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
    public async Task UpdateDid_TransactionHandler_fails_for_multiple_add_key_operations_for_identicalId()
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
                    UpdateDid = new UpdateDIDOperation
                    {
                        Id = "did:prism:someDid",
                        PreviousOperationHash = PrismEncoding.Utf8StringToByteString("previousOperationHash"),
                        Actions =
                        {
                            new UpdateDIDAction()
                            {
                                AddKey = new AddKeyAction()
                                {
                                    Key = new PublicKey()
                                    {
                                        Id = "key-1",
                                        Usage = KeyUsage.IssuingKey,
                                        CompressedEcKeyData = new CompressedECKeyData()
                                        {
                                            Curve = "secp256k1",
                                            Data = publicKeyTestData.CompressedEcKeyData.Data
                                        }
                                    },
                                }
                            },
                            new UpdateDIDAction()
                            {
                                AddKey = new AddKeyAction()
                                {
                                    Key = new PublicKey()
                                    {
                                        Id = "key-1",
                                        Usage = KeyUsage.AuthenticationKey,
                                        CompressedEcKeyData = new CompressedECKeyData()
                                        {
                                            Curve = "secp256k1",
                                            Data = publicKeyTestData.CompressedEcKeyData.Data
                                        }
                                    },
                                }
                            },
                        },
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
        result.Should().BeFailure().And.Match(n => n.Errors.FirstOrDefault().Message.Contains("Duplicate key IDs detected. Each key ID must be unique"));
    }

    [Fact]
    public async Task UpdateDid_TransactionHandler_succeds_for_multiple_different_key_operations_for_identicalId_in_correct_order()
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
                    UpdateDid = new UpdateDIDOperation
                    {
                        Id = "did:prism:someDid",
                        PreviousOperationHash = PrismEncoding.Utf8StringToByteString("previousOperationHash"),
                        Actions =
                        {
                            new UpdateDIDAction()
                            {
                                AddKey = new AddKeyAction()
                                {
                                    Key = new PublicKey()
                                    {
                                        Id = "key-1",
                                        Usage = KeyUsage.IssuingKey,
                                        CompressedEcKeyData = new CompressedECKeyData()
                                        {
                                            Curve = "secp256k1",
                                            Data = publicKeyTestData.CompressedEcKeyData.Data
                                        }
                                    },
                                }
                            },
                            new UpdateDIDAction()
                            {
                                RemoveKey = new RemoveKeyAction()
                                {
                                    KeyId = "key-1"
                                }
                            },
                        },
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
    public async Task UpdateDid_TransactionHandler_succeds_for_multiple_remove_key_operations_for_different_keys()
    {
        // Arrange
        var mockedEcService = new Mock<IEcService>();
        mockedEcService.Setup(p => p.VerifyData(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(true);

        var parseTransactionRequest = new ParseTransactionRequest(
            new SignedAtalaOperation
            {
                Operation = new AtalaOperation
                {
                    UpdateDid = new UpdateDIDOperation
                    {
                        Id = "did:prism:someDid",
                        PreviousOperationHash = PrismEncoding.Utf8StringToByteString("previousOperationHash"),
                        Actions =
                        {
                            new UpdateDIDAction()
                            {
                                RemoveKey = new RemoveKeyAction()
                                {
                                    KeyId = "key-1"
                                }
                            },
                            new UpdateDIDAction()
                            {
                                RemoveKey = new RemoveKeyAction()
                                {
                                    KeyId = "key-2"
                                }
                            },
                        },
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
    public async Task UpdateDid_TransactionHandler_fails_for_multiple_remove_key_operations_for_the_same_key()
    {
        // Arrange
        var mockedEcService = new Mock<IEcService>();
        mockedEcService.Setup(p => p.VerifyData(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(true);

        var parseTransactionRequest = new ParseTransactionRequest(
            new SignedAtalaOperation
            {
                Operation = new AtalaOperation
                {
                    UpdateDid = new UpdateDIDOperation
                    {
                        Id = "did:prism:someDid",
                        PreviousOperationHash = PrismEncoding.Utf8StringToByteString("previousOperationHash"),
                        Actions =
                        {
                            new UpdateDIDAction()
                            {
                                RemoveKey = new RemoveKeyAction()
                                {
                                    KeyId = "key-1"
                                }
                            },
                            new UpdateDIDAction()
                            {
                                RemoveKey = new RemoveKeyAction()
                                {
                                    KeyId = "key-1"
                                }
                            },
                        },
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
        result.Should().BeFailure().And.Match(n => n.Errors.FirstOrDefault().Message.Contains("The key was already removed from in a previous action"));
    }

    [Fact]
    public async Task UpdateDid_TransactionHandler_fails_for_removing_master_key_without_providing_a_new_master_key()
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
                    UpdateDid = new UpdateDIDOperation
                    {
                        Id = "did:prism:someDid",
                        PreviousOperationHash = PrismEncoding.Utf8StringToByteString("previousOperationHash"),
                        Actions =
                        {
                            new UpdateDIDAction()
                            {
                                RemoveKey = new RemoveKeyAction()
                                {
                                    KeyId = "dummyMaster"
                                }
                            },
                        },
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
        result.Should().BeFailure().And.Match(n => n.Errors.FirstOrDefault().Message.Contains("After the update operation at least one valid master key must exist. The last master key cannot be removed"));
    }

    [Fact]
    public async Task UpdateDid_TransactionHandler_succeds_for_removing_master_key_with_providing_a_new_master_key()
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
                    UpdateDid = new UpdateDIDOperation
                    {
                        Id = "did:prism:someDid",
                        PreviousOperationHash = PrismEncoding.Utf8StringToByteString("previousOperationHash"),
                        Actions =
                        {
                            new UpdateDIDAction()
                            {
                                AddKey = new AddKeyAction()
                                {
                                    Key = new PublicKey()
                                    {
                                        Id = "newMaster",
                                        Usage = KeyUsage.MasterKey,
                                        CompressedEcKeyData = new CompressedECKeyData()
                                        {
                                            Curve = "secp256k1",
                                            Data = publicKeyTestData.CompressedEcKeyData.Data
                                        }
                                    },
                                }
                            },
                            new UpdateDIDAction()
                            {
                                RemoveKey = new RemoveKeyAction()
                                {
                                    KeyId = "dummyMaster"
                                }
                            },
                        },
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
    public async Task UpdateDid_TransactionHandler_fails_for_removing_master_key_with_providing_a_new_master_key_but_in_wrong_order()
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
                    UpdateDid = new UpdateDIDOperation
                    {
                        Id = "did:prism:someDid",
                        PreviousOperationHash = PrismEncoding.Utf8StringToByteString("previousOperationHash"),
                        Actions =
                        {
                            new UpdateDIDAction()
                            {
                                RemoveKey = new RemoveKeyAction()
                                {
                                    KeyId = "dummyMaster"
                                }
                            },
                            new UpdateDIDAction()
                            {
                                AddKey = new AddKeyAction()
                                {
                                    Key = new PublicKey()
                                    {
                                        Id = "newMaster",
                                        Usage = KeyUsage.MasterKey,
                                        CompressedEcKeyData = new CompressedECKeyData()
                                        {
                                            Curve = "secp256k1",
                                            Data = publicKeyTestData.CompressedEcKeyData.Data
                                        }
                                    },
                                }
                            },
                        },
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
        result.Should().BeFailure().And.Match(n => n.Errors.FirstOrDefault().Message.Contains("After the update operation at least one valid master key must exist. The last master key cannot be removed"));
    }

    [Fact]
    public async Task UpdateDid_TransactionHandler_fails_for_removing_master_key_also_removing_new_master_key()
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
                    UpdateDid = new UpdateDIDOperation
                    {
                        Id = "did:prism:someDid",
                        PreviousOperationHash = PrismEncoding.Utf8StringToByteString("previousOperationHash"),
                        Actions =
                        {
                            new UpdateDIDAction()
                            {
                                AddKey = new AddKeyAction()
                                {
                                    Key = new PublicKey()
                                    {
                                        Id = "newMaster",
                                        Usage = KeyUsage.MasterKey,
                                        CompressedEcKeyData = new CompressedECKeyData()
                                        {
                                            Curve = "secp256k1",
                                            Data = publicKeyTestData.CompressedEcKeyData.Data
                                        }
                                    },
                                }
                            },
                            new UpdateDIDAction()
                            {
                                RemoveKey = new RemoveKeyAction()
                                {
                                    KeyId = "dummyMaster"
                                }
                            },
                            new UpdateDIDAction()
                            {
                                RemoveKey = new RemoveKeyAction()
                                {
                                    KeyId = "newMaster"
                                }
                            },
                        },
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
        result.Should().BeFailure().And.Match(n => n.Errors.FirstOrDefault().Message.Contains("After the update operation at least one valid master key must exist. The last master key cannot be removed"));
    }

    [Fact]
    public async Task UpdateDid_TransactionHandler_succeds_for_removing_master_key_also_adding_new_masterkeys_and_removing_one_of_them()
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
                    UpdateDid = new UpdateDIDOperation
                    {
                        Id = "did:prism:someDid",
                        PreviousOperationHash = PrismEncoding.Utf8StringToByteString("previousOperationHash"),
                        Actions =
                        {
                            new UpdateDIDAction()
                            {
                                AddKey = new AddKeyAction()
                                {
                                    Key = new PublicKey()
                                    {
                                        Id = "newMaster",
                                        Usage = KeyUsage.MasterKey,
                                        CompressedEcKeyData = new CompressedECKeyData()
                                        {
                                            Curve = "secp256k1",
                                            Data = publicKeyTestData.CompressedEcKeyData.Data
                                        }
                                    },
                                }
                            },
                            new UpdateDIDAction()
                            {
                                RemoveKey = new RemoveKeyAction()
                                {
                                    KeyId = "dummyMaster"
                                }
                            },
                            new UpdateDIDAction()
                            {
                                AddKey = new AddKeyAction()
                                {
                                    Key = new PublicKey()
                                    {
                                        Id = "newMaster2",
                                        Usage = KeyUsage.MasterKey,
                                        CompressedEcKeyData = new CompressedECKeyData()
                                        {
                                            Curve = "secp256k1",
                                            Data = publicKeyTestData.CompressedEcKeyData.Data
                                        }
                                    },
                                }
                            },
                            new UpdateDIDAction()
                            {
                                RemoveKey = new RemoveKeyAction()
                                {
                                    KeyId = "newMaster"
                                }
                            },
                        },
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
    public async Task UpdateDid_TransactionHandler_fails_for_adding_the_same_service_twice()
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
                    UpdateDid = new UpdateDIDOperation
                    {
                        Id = "did:prism:someDid",
                        PreviousOperationHash = PrismEncoding.Utf8StringToByteString("previousOperationHash"),
                        Actions =
                        {
                            new UpdateDIDAction()
                            {
                                AddService = new AddServiceAction()
                                {
                                    Service = new Service()
                                    {
                                        Id = "service0",
                                        Type = "myService",
                                        ServiceEndpoint = "http://myServiceEndpoint"
                                    }
                                }
                            },
                            new UpdateDIDAction()
                            {
                                AddService = new AddServiceAction()
                                {
                                    Service = new Service()
                                    {
                                        Id = "service0",
                                        Type = "myService",
                                        ServiceEndpoint = "http://myServiceEndpoint"
                                    }
                                }
                            },
                        },
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
        result.Should().BeFailure().And.Match(n => n.Errors.FirstOrDefault().Message.Contains("The service was already added to the DID"));
    }

    [Fact]
    public async Task UpdateDid_TransactionHandler_fails_for_removing_the_same_service_twice()
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
                    UpdateDid = new UpdateDIDOperation
                    {
                        Id = "did:prism:someDid",
                        PreviousOperationHash = PrismEncoding.Utf8StringToByteString("previousOperationHash"),
                        Actions =
                        {
                            new UpdateDIDAction()
                            {
                                RemoveService = new RemoveServiceAction()
                                {
                                    ServiceId = "service0"
                                }
                            },
                            new UpdateDIDAction()
                            {
                                RemoveService = new RemoveServiceAction()
                                {
                                    ServiceId = "service0"
                                }
                            },
                        },
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
        result.Should().BeFailure().And.Match(n => n.Errors.FirstOrDefault().Message.Contains("The service was already removed from the DID in a previous action"));
    }


    [Fact]
    public async Task UpdateDid_TransactionHandler_fails_for_adding_and_removing_service_inside_one_operation_in_wrong_order()
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
                    UpdateDid = new UpdateDIDOperation
                    {
                        Id = "did:prism:someDid",
                        PreviousOperationHash = PrismEncoding.Utf8StringToByteString("previousOperationHash"),
                        Actions =
                        {
                            new UpdateDIDAction()
                            {
                                RemoveService = new RemoveServiceAction()
                                {
                                    ServiceId = "service0"
                                }
                            },
                            new UpdateDIDAction()
                            {
                                AddService = new AddServiceAction()
                                {
                                    Service = new Service()
                                    {
                                        Id = "service0",
                                        Type = "myService",
                                        ServiceEndpoint = "http://myServiceEndpoint"
                                    }
                                }
                            }
                        },
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
        result.Should().BeFailure();
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