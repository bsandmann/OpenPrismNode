namespace OpenPrismNode.Sync.Tests.ParseTransaction;

using Commands.ParseTransaction;
using Core.Crypto;
using Core.Models;
using FluentResults.Extensions.FluentAssertions;
using Google.Protobuf;
using Google.Protobuf.Collections;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

public class UpdateDidTransaction
{
    private ParseTransactionHandler _parseTransactionHandler;
    private readonly ISha256Service _sha256Service;
    private readonly IEcService _ecService;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly ILogger<ParseTransactionHandler> _logger;

    public UpdateDidTransaction()
    {
        _mediatorMock = new Mock<IMediator>();
        _sha256Service = new Sha256ServiceBouncyCastle();
        _ecService = new EcServiceBouncyCastle();
        _logger = new Mock<ILogger<ParseTransactionHandler>>().Object;
    }

    [Fact]
    public async Task UpdateDid_TransactionHandler_succeds_for_well_constructed_UpdateDid_AddKeyAction()
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
                                        Id = "master0",
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
            resolveMode: new ResolveMode(ParserResolveMode.NoResolveNoSignatureVerication)
        );

        // Act
        _parseTransactionHandler = new ParseTransactionHandler(_mediatorMock.Object, _sha256Service, mockedEcService.Object, _logger);
        var result = await _parseTransactionHandler.Handle(parseTransactionRequest, CancellationToken.None);

        // Assert
        result.Should().BeSuccess();
    }

    [Fact]
    public async Task UpdateDid_TransactionHandler_fails_with_AddKeyAction_on_emtpy_key()
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
                            },
                        },
                    }
                },
                SignedWith = "master0",
                Signature = PrismEncoding.Utf8StringToByteString("someSignature")
            },
            0,
            resolveMode: new ResolveMode(ParserResolveMode.NoResolveNoSignatureVerication)
        );

        // Act
        _parseTransactionHandler = new ParseTransactionHandler(_mediatorMock.Object, _sha256Service, mockedEcService.Object, _logger);
        var result = await _parseTransactionHandler.Handle(parseTransactionRequest, CancellationToken.None);

        // Assert
        result.Should().BeFailure().And.Match(n => n.Errors.FirstOrDefault().Message.Contains("Invalid action construction"));
    }

    [Fact]
    public async Task UpdateDid_TransactionHandler_succeeds_with_RemoveKeyAction_and_well_constructed_request()
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
                                    KeyId = "someId",
                                }
                            }
                        }
                    }
                },
                SignedWith = "master0",
                Signature = PrismEncoding.Utf8StringToByteString("someSignature")
            },
            0,
            resolveMode: new ResolveMode(ParserResolveMode.NoResolveNoSignatureVerication)
        );

        // Act
        _parseTransactionHandler = new ParseTransactionHandler(_mediatorMock.Object, _sha256Service, mockedEcService.Object, _logger);
        var result = await _parseTransactionHandler.Handle(parseTransactionRequest, CancellationToken.None);

        // Assert
        result.Should().BeSuccess();
    }

    [Fact]
    public async Task UpdateDid_TransactionHandler_succeeds_with_AddServiceAction_and_well_constructed_service()
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
                        }
                    }
                },
                SignedWith = "master0",
                Signature = PrismEncoding.Utf8StringToByteString("someSignature")
            },
            0,
            resolveMode: new ResolveMode(ParserResolveMode.NoResolveNoSignatureVerication)
        );

        // Act
        _parseTransactionHandler = new ParseTransactionHandler(_mediatorMock.Object, _sha256Service, mockedEcService.Object, _logger);
        var result = await _parseTransactionHandler.Handle(parseTransactionRequest, CancellationToken.None);

        // Assert
        result.Should().BeSuccess();
    }

    [Fact]
    public async Task UpdateDid_TransactionHandler_succeeds_with_RemoveServiceAction_and_well_constructed_service()
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
                                RemoveService = new RemoveServiceAction()
                                {
                                    ServiceId = "someId",
                                }
                            }
                        }
                    }
                },
                SignedWith = "master0",
                Signature = PrismEncoding.Utf8StringToByteString("someSignature")
            },
            0,
            resolveMode: new ResolveMode(ParserResolveMode.NoResolveNoSignatureVerication)
        );

        // Act
        _parseTransactionHandler = new ParseTransactionHandler(_mediatorMock.Object, _sha256Service, mockedEcService.Object, _logger);
        var result = await _parseTransactionHandler.Handle(parseTransactionRequest, CancellationToken.None);

        // Assert
        result.Should().BeSuccess();
    }

    [Fact]
    public async Task UpdateDid_TransactionHandler_succeeds_with_UpdateServiceAction_and_well_constructed_service()
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
                                UpdateService = new UpdateServiceAction()
                                {
                                    ServiceId = "someId",
                                    ServiceEndpoints = "https://someUpdatedServiceEndpoint.com"
                                }
                            }
                        }
                    }
                },
                SignedWith = "master0",
                Signature = PrismEncoding.Utf8StringToByteString("someSignature")
            },
            0,
            resolveMode: new ResolveMode(ParserResolveMode.NoResolveNoSignatureVerication)
        );

        // Act
        _parseTransactionHandler = new ParseTransactionHandler(_mediatorMock.Object, _sha256Service, mockedEcService.Object, _logger);
        var result = await _parseTransactionHandler.Handle(parseTransactionRequest, CancellationToken.None);

        // Assert
        result.Should().BeSuccess();
    }

    [Fact]
    public async Task UpdateDid_TransactionHandler_succeeds_with_PatchContextAction_and_well_constructed_contexts()
    {
        // Arrange
        var mockedEcService = new Mock<IEcService>();
        mockedEcService.Setup(p => p.VerifyData(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(true);

        var patchedContextAction = new PatchContextAction();
        patchedContextAction.Context.Add("someContext");

        var parseTransactionRequest = new ParseTransactionRequest(
            new SignedAtalaOperation
            {
                Operation = new AtalaOperation
                {
                    UpdateDid = new UpdateDIDOperation()
                    {
                        PreviousOperationHash = PrismEncoding.Utf8StringToByteString("previousOperationHash"),
                        Actions =
                        {
                            // see constrction below. Constructor is not supported here by the protobuf generator
                        }
                    }
                },
                SignedWith = "master0",
                Signature = PrismEncoding.Utf8StringToByteString("someSignature")
            },
            0,
            resolveMode: new ResolveMode(ParserResolveMode.NoResolveNoSignatureVerication)
        );
        parseTransactionRequest.SignedAtalaOperation.Operation.UpdateDid.Actions.Add(new UpdateDIDAction() { PatchContext = patchedContextAction });

        // Act
        _parseTransactionHandler = new ParseTransactionHandler(_mediatorMock.Object, _sha256Service, mockedEcService.Object, _logger);
        var result = await _parseTransactionHandler.Handle(parseTransactionRequest, CancellationToken.None);

        // Assert
        result.Should().BeSuccess();
    }
    
    [Fact]
    public async Task UpdateDid_TransactionHandler_fails_with_PatchContextAction_and_missing_previous_operationHash()
    {
        // Arrange
        var mockedEcService = new Mock<IEcService>();
        mockedEcService.Setup(p => p.VerifyData(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(true);

        var patchedContextAction = new PatchContextAction();
        patchedContextAction.Context.Add("someContext");

        var parseTransactionRequest = new ParseTransactionRequest(
            new SignedAtalaOperation
            {
                Operation = new AtalaOperation
                {
                    UpdateDid = new UpdateDIDOperation()
                    {
                        Actions =
                        {
                            // see constrction below. Constructor is not supported here by the protobuf generator
                        }
                    }
                },
                SignedWith = "master0",
                Signature = PrismEncoding.Utf8StringToByteString("someSignature")
            },
            0,
            resolveMode: new ResolveMode(ParserResolveMode.NoResolveNoSignatureVerication)
        );
        parseTransactionRequest.SignedAtalaOperation.Operation.UpdateDid.Actions.Add(new UpdateDIDAction() { PatchContext = patchedContextAction });

        // Act
        _parseTransactionHandler = new ParseTransactionHandler(_mediatorMock.Object, _sha256Service, mockedEcService.Object, _logger);
        var result = await _parseTransactionHandler.Handle(parseTransactionRequest, CancellationToken.None);

        // Assert
        result.Should().BeFailure().And.Match(n => n.Errors.FirstOrDefault().Message.Contains("nvalid previous operation hash"));

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