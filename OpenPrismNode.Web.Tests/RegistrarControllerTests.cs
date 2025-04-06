// using directives for your project's namespaces and testing libraries

using FluentAssertions;
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OpenPrismNode.Core.Commands.GetDidByOperationHash;
using OpenPrismNode.Core.Commands.GetOperationStatus;
using OpenPrismNode.Core.Commands.GetVerificationMethodSecrets;
using OpenPrismNode.Core.Commands.GetWallet;
using OpenPrismNode.Core.Commands.GetWalletByOperationStatus;
using OpenPrismNode.Core.Commands.Registrar;
using OpenPrismNode.Core.Commands.Registrar.CreateSignedAtalaOperationForCreateDid;
using OpenPrismNode.Core.Commands.Registrar.CreateSignedAtalaOperationForDeactivateDid;
using OpenPrismNode.Core.Commands.Registrar.CreateSignedAtalaOperationForUpdateDid;
using OpenPrismNode.Core.Commands.ResolveDid;
using OpenPrismNode.Core.Commands.WriteTransaction;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Core.Crypto;
using OpenPrismNode.Core.Entities;
using OpenPrismNode.Core.Models;
using OpenPrismNode.Core.Services.Did;
using OpenPrismNode.Sync.Commands.ParseTransaction;
using OpenPrismNode.Web.Controller;
using OpenPrismNode.Web.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Google.Protobuf;

namespace OpenPrismNode.Web.Tests;

public class RegistrarControllerTests
{
    // Mocks
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<RegistrarController>> _mockLogger;
    private readonly Mock<IOptions<AppSettings>> _mockAppSettings;

    // System Under Test
    private readonly RegistrarController _controller;

    // Shared Test Data
    private readonly AppSettings _defaultAppSettings;
    private readonly string _testWalletId = "test-wallet-123";
    private readonly string _testDid = "did:prism:test123abc";
    private readonly string _testJobIdHex = "AABBCCDDEEFF"; // Example hex JobId
    private readonly byte[] _testJobIdBytes = PrismEncoding.TryHexToByteArray("AABBCCDDEEFF").Value; // Corresponding bytes


    public RegistrarControllerTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<RegistrarController>>();
        _mockAppSettings = new Mock<IOptions<AppSettings>>();

        // Setup default AppSettings mock - adjust "preprod" or "mainnet" as needed for default test case
        _defaultAppSettings = new AppSettings
        {
            PrismLedger = new PrismLedger { Name = "preprod" }, // Example default
            // Add other necessary default settings if the controller uses them directly
        };
        _mockAppSettings.Setup(ap => ap.Value).Returns(_defaultAppSettings);

        _controller = new RegistrarController(
            _mockMediator.Object,
            _mockLogger.Object,
            _mockAppSettings.Object
        );

    }

    private static RegistrarCreateRequestModel CreateValidCreateRequest(string? jobId = null)
    {
        return new RegistrarCreateRequestModel
        {
            Method = "prism",
            Did = null, // Must be null for prism create
            Options = new RegistrarOptions { WalletId = "test-wallet-123", Network = "preprod" }, // Match app settings network
            Secret = null, // Assuming registrar generates secrets
            DidDocument = new RegistrarDidDocument { { "key1", "value1" } }, // Minimal valid doc
            JobId = jobId
        };
    }

    private static RegistrarUpdateRequestModel CreateValidUpdateRequest(string? jobId = null)
    {
        return new RegistrarUpdateRequestModel
        {
            Options = new RegistrarOptions { WalletId = "test-wallet-123", Network = "preprod" },
            Secret = null, // Provide if needed for update auth, depends on handler logic
            DidDocumentOperation = new List<string> { "setDidDocument" },
            DidDocument = new List<RegistrarDidDocument> { new RegistrarDidDocument { { "key2", "value2" } } },
            JobId = jobId
        };
    }

    private static RegistrarDeactivateRequestModel CreateValidDeactivateRequest(string? jobId = null)
    {
        return new RegistrarDeactivateRequestModel
        {
            Options = new RegistrarOptions { WalletId = "test-wallet-123", Network = "preprod" },
            Secret = null,
            JobId = jobId
        };
    }


    [Fact]
    public async Task CreateDid_WithClientSecretModeTrue_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateValidCreateRequest();
        request.Options!.ClientSecretMode = true;

        // Act
        var result = await _controller.CreateDid(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Title.Should().Be("Unsupported Operation");
        problemDetails.Detail.Should().Be("Client-managed secret mode is not supported.");

        _mockMediator.Verify(m => m.Send(It.IsAny<CreateSignedAtalaOperationForCreateDidRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateDid_WithInvalidMethod_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateValidCreateRequest();
        request.Method = "invalid-method";

        // Act
        var result = await _controller.CreateDid(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().Be("Invalid method. Only 'prism' is supported.");
    }

    [Fact]
    public async Task CreateDid_WithDidProvided_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateValidCreateRequest();
        request.Did = "did:prism:someprovideddid"; // Not allowed for prism create

        // Act
        var result = await _controller.CreateDid(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().Be("DID should not be provided for creation for did:prism operations. It will be generated.");
    }

    [Fact]
    public async Task CreateDid_WithMissingWalletId_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateValidCreateRequest();
        request.Options!.WalletId = null!; // Make it invalid

        // Act
        var result = await _controller.CreateDid(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().Be("WalletId must be provided for creation.");
    }

    [Fact]
    public async Task CreateDid_WithInvalidNetwork_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateValidCreateRequest();
        request.Options!.Network = "mainnet"; // Different from settings (preprod)

        // Act
        var result = await _controller.CreateDid(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().Be($"Invalid network. The specified network does not match the settings of OPN ({_defaultAppSettings.PrismLedger.Name})");
    }

    [Fact]
    public async Task CreateDid_WithInvalidJobIdHex_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateValidCreateRequest(jobId: "Invalid-Hex-Job-Id");

        // Act
        var result = await _controller.CreateDid(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        // Check that the error is related to hex parsing
        var badRequestResult = result as BadRequestObjectResult;
        if (badRequestResult?.Value != null)
        {
            badRequestResult.Value.ToString().Should().Contain("hex");
        }
    }

    [Fact]
    public async Task CreateDid_ExistingJob_GetOperationStatusFails_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateValidCreateRequest(jobId: _testJobIdHex);

        // Setup mediator to return failed result for GetOperationStatus
        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetOperationStatusRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail<GetOperationStatusResponse>("DB Error fetching status"));

        // Act
        var result = await _controller.CreateDid(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().Be("DB Error fetching status");
    }

    [Fact]
    public async Task UpdateDid_WithNullOrEmptyDid_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateValidUpdateRequest();

        // Act
        var result = await _controller.UpdateDid(" ", request); // Empty DID

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeOfType<ProblemDetails>()
            .Which.Detail.Should().Be("DID must be provided in the URL path.");
    }

    [Fact]
    public async Task UpdateDid_WithInvalidDidFormat_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateValidUpdateRequest();

        // Act
        var result = await _controller.UpdateDid("invalid-did-format", request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeOfType<ProblemDetails>()
            .Which.Detail.Should().Be("The DID must be in the format 'did:prism:...'.");
    }
    
    [Fact]
    public async Task UpdateDid_WithClientSecretModeTrue_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateValidUpdateRequest();
        request.Options!.ClientSecretMode = true;

        // Act
        var result = await _controller.UpdateDid(_testDid, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeOfType<ProblemDetails>()
            .Which.Detail.Should().Be("Client-managed secret mode is not supported.");

        _mockMediator.Verify(m => m.Send(It.IsAny<CreateSignedAtalaOperationForUpdateDidRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }
    
    [Fact]
    public async Task UpdateDid_WithMissingWalletId_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateValidUpdateRequest();
        request.Options!.WalletId = null!;

        // Act
        var result = await _controller.UpdateDid(_testDid, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().Be("WalletId must be provided for updates.");
    }
    
    [Fact]
    public async Task UpdateDid_WithInvalidNetwork_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateValidUpdateRequest();
        request.Options!.Network = "mainnet"; // Different from settings (preprod)

        // Act
        var result = await _controller.UpdateDid(_testDid, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().Be($"Invalid network. The specified network does not match the settings of OPN ({_defaultAppSettings.PrismLedger.Name})");
    }


    [Fact]
    public async Task DeactivateDid_WithNullOrEmptyDid_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateValidDeactivateRequest();

        // Act
        var result = await _controller.DeactivateDid(" ", request); // Empty DID

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeOfType<ProblemDetails>()
            .Which.Detail.Should().Be("DID must be provided in the URL path.");
    }

    [Fact]
    public async Task DeactivateDid_WithClientSecretModeTrue_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateValidDeactivateRequest();
        request.Options!.ClientSecretMode = true;

        // Act
        var result = await _controller.DeactivateDid(_testDid, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeOfType<ProblemDetails>()
            .Which.Detail.Should().Be("Client-managed secret mode is not supported.");
    }

    [Fact]
    public async Task DeactivateDid_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = CreateValidDeactivateRequest();
        
        // Create a mock SignedAtalaOperation for the tests
        var signedAtalaOperation = new SignedAtalaOperation
        {
            Operation = new AtalaOperation(),
            SignedWith = "test-signer",
            Signature = ByteString.CopyFrom(new byte[] { 1, 2, 3 })
        };
        
        // Setup mediator responses for the chain of calls
        _mockMediator
            .Setup(m => m.Send(It.IsAny<CreateSignedAtalaOperationForDeactivateDidRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new CreateSignedAtalaOperationForDeactivateDidResponse(
                signedAtalaOperation
            )));
            
        // Mock ParseTransactionRequest to use a dummy OperationResultWrapper
        _mockMediator
            .Setup(m => m.Send(It.IsAny<ParseTransactionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(CreateTestOperationResultWrapper(OperationResultType.DeactivateDid)));
            
        // Create a mock WriteTransactionResponse
        var writeTransactionResponse = new WriteTransactionResponse
        {
            OperationStatusId = new byte[] { 10, 11, 12 },
            OperationType = OperationTypeEnum.DeactivateDid,
            DidSuffix = "testsuffix"
        };
        
        _mockMediator
            .Setup(m => m.Send(It.IsAny<WriteTransactionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(writeTransactionResponse));
        
        // Act
        var result = await _controller.DeactivateDid(_testDid, request);
        
        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as RegistrarResponseDto;
        
        response.Should().NotBeNull();
        response!.JobId.Should().Be(PrismEncoding.ByteArrayToHex(writeTransactionResponse.OperationStatusId));
        response.DidState.Should().NotBeNull();
        response.DidState.State.Should().Be(RegistrarDidState.WaitState);
    }



    [Fact]
    public async Task CreateDid_ExistingJob_ConfirmedAndApplied_ReturnsFinishedState()
    {
        // Arrange
        var request = CreateValidCreateRequest(jobId: _testJobIdHex);

        // Mock the GetOperationStatus call to return a response rather than an entity
        var getOpStatusResponse = new GetOperationStatusResponse
        {
            OperationStatusId = _testJobIdBytes,
            Status = OperationStatusEnum.ConfirmedAndApplied,
            OperationType = OperationTypeEnum.CreateDid,
            OperationHash = new byte[] { 20, 21, 22 } // Example operation hash
        };
        
        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetOperationStatusRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(getOpStatusResponse));

        // Mock all the dependent calls needed by ProcessExistingJobAsync for a successful case
        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetVerificationMethodSecretsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new List<VerificationMethodSecret>
            {
                new VerificationMethodSecret("masterKey", "masterKey", "secp256k1", new byte[] { 1, 2, 3 }, false, "word1 word2 word3"),
                new VerificationMethodSecret("authentication", "key1", "secp256k1", new byte[] { 4, 5, 6 }, false, null)
            }));

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetDidByOperationHashRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(_testDid));

        // For the InternalDidDocument and Hash classes, we need to create them properly
        var hash = Hash.CreateFrom(new byte[] { 30, 31, 32 });

        // The service endpoints need proper initialization
        var serviceEndpoints = new ServiceEndpoints
        {
            Uri = new Uri("https://test.com")
        };

        // Create proper 32-byte arrays for the PrismPublicKey constructor
        var xBytes1 = new byte[32]; 
        var yBytes1 = new byte[32]; 
        var xBytes2 = new byte[32]; 
        var yBytes2 = new byte[32];
        
        // Set some values to make them unique but valid
        xBytes1[0] = 10; xBytes1[1] = 11;
        yBytes1[0] = 12; yBytes1[1] = 13;
        xBytes2[0] = 14; xBytes2[1] = 15;
        yBytes2[0] = 16; yBytes2[1] = 17;
        
        // Create a valid internal DID document with necessary constructor parameters
        var internalDidDocument = new InternalDidDocument(
            didIdentifierIdentifier: "test123abc",
            publicKeys: new List<PrismPublicKey>
            {
                new PrismPublicKey(PrismKeyUsage.MasterKey, "masterKey", "secp256k1", xBytes1, yBytes1),
                new PrismPublicKey(PrismKeyUsage.AuthenticationKey, "key1", "secp256k1", xBytes2, yBytes2)
            },
            prismServices: new List<PrismService>
            {
                new PrismService("service1", "TestService", serviceEndpoints)
            },
            contexts: new List<string> { "https://www.w3.org/ns/did/v1" },
            created: DateTime.UtcNow,
            versionId: "version1",
            cardanoTransactionPosition: 1,
            operationPosition: 1,
            originTxId: "originTx"
        );

        _mockMediator
            .Setup(m => m.Send(It.IsAny<ResolveDidRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new ResolveDidResponse(internalDidDocument, hash)));

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetWalletByOperationStatusIdRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok<GetWalletResponse?>(new GetWalletResponse { WalletId = _testWalletId }));

        // Act
        var result = await _controller.CreateDid(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as RegistrarResponseDto;
        
        response.Should().NotBeNull();
        response!.JobId.Should().Be(_testJobIdHex);
        response.DidState.State.Should().Be(RegistrarDidState.FinishedState);
        response.DidState.Did.Should().Be(_testDid);
        response.DidDocumentMetadata.Should().NotBeNull();
        response.DidRegistrationMetadata.Should().NotBeNull();
        response.DidRegistrationMetadata["walletId"].Should().Be(_testWalletId);
        response.DidRegistrationMetadata.Should().ContainKey("mnemonic");
    }

    [Fact]
    public async Task CreateDid_ExistingJob_Pending_ReturnsWaitState()
    {
        // Arrange
        var request = CreateValidCreateRequest(jobId: _testJobIdHex);

        // Mock the GetOperationStatus call
        var getOpStatusResponse = new GetOperationStatusResponse
        {
            OperationStatusId = _testJobIdBytes,
            Status = OperationStatusEnum.PendingSubmission, // Pending status
            OperationType = OperationTypeEnum.CreateDid
        };
        
        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetOperationStatusRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(getOpStatusResponse));

        // Act
        var result = await _controller.CreateDid(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as RegistrarResponseDto;
        
        response.Should().NotBeNull();
        response!.JobId.Should().Be(_testJobIdHex);
        response.DidState.State.Should().Be(RegistrarDidState.WaitState);
    }

    [Fact]
    public async Task CreateDid_ExistingJob_Failed_ReturnsFailedState()
    {
        // Arrange
        var request = CreateValidCreateRequest(jobId: _testJobIdHex);

        // Mock the GetOperationStatus call
        var getOpStatusResponse = new GetOperationStatusResponse
        {
            OperationStatusId = _testJobIdBytes,
            Status = OperationStatusEnum.ConfirmedAndRejected, // Failed status
            OperationType = OperationTypeEnum.CreateDid
        };
        
        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetOperationStatusRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(getOpStatusResponse));

        // Act
        var result = await _controller.CreateDid(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as RegistrarResponseDto;
        
        response.Should().NotBeNull();
        response!.JobId.Should().Be(_testJobIdHex);
        response.DidState.State.Should().Be(RegistrarDidState.FailedState);
        response.DidState.Reason.Should().Contain(OperationStatusEnum.ConfirmedAndRejected.ToString());
    }
    
    [Fact]
    public async Task UpdateDid_ExistingJob_ReturnsProcessedJob()
    {
        // Arrange
        var request = CreateValidUpdateRequest(jobId: _testJobIdHex);

        // Mock the GetOperationStatus call
        var getOpStatusResponse = new GetOperationStatusResponse
        {
            OperationStatusId = _testJobIdBytes,
            Status = OperationStatusEnum.ConfirmedAndApplied,
            OperationType = OperationTypeEnum.UpdateDid,
            OperationHash = new byte[] { 20, 21, 22 } // Example operation hash
        };
        
        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetOperationStatusRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(getOpStatusResponse));

        // Mock all the dependent calls needed by ProcessExistingJobAsync for a successful case
        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetVerificationMethodSecretsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new List<VerificationMethodSecret>
            {
                new VerificationMethodSecret("authentication", "key1", "secp256k1", new byte[] { 4, 5, 6 }, false, null)
            }));

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetDidByOperationHashRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(_testDid));

        // Create valid hash
        var hash = Hash.CreateFrom(new byte[] { 30, 31, 32 });

        // Create proper 32-byte arrays for the PrismPublicKey constructor
        var xBytes1 = new byte[32]; 
        var yBytes1 = new byte[32]; 
        var xBytes2 = new byte[32]; 
        var yBytes2 = new byte[32];
        
        // Set some values to make them unique but valid
        xBytes1[0] = 10; xBytes1[1] = 11;
        yBytes1[0] = 12; yBytes1[1] = 13;
        xBytes2[0] = 14; xBytes2[1] = 15;
        yBytes2[0] = 16; yBytes2[1] = 17;
        
        // Create a valid internal DID document with necessary constructor parameters
        var internalDidDocument = new InternalDidDocument(
            didIdentifierIdentifier: "test123abc",
            publicKeys: new List<PrismPublicKey>
            {
                new PrismPublicKey(PrismKeyUsage.MasterKey, "masterKey", "secp256k1", xBytes1, yBytes1),
                new PrismPublicKey(PrismKeyUsage.AuthenticationKey, "key1", "secp256k1", xBytes2, yBytes2)
            },
            prismServices: new List<PrismService>(),
            contexts: new List<string> { "https://www.w3.org/ns/did/v1" },
            created: DateTime.UtcNow,
            versionId: "version1",
            cardanoTransactionPosition: 1,
            operationPosition: 1,
            originTxId: "originTx"
        );

        _mockMediator
            .Setup(m => m.Send(It.IsAny<ResolveDidRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new ResolveDidResponse(internalDidDocument, hash)));

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetWalletByOperationStatusIdRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok<GetWalletResponse?>(new GetWalletResponse { WalletId = _testWalletId }));

        // Act
        var result = await _controller.UpdateDid(_testDid, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as RegistrarResponseDto;
        
        response.Should().NotBeNull();
        response!.JobId.Should().Be(_testJobIdHex);
        response.DidState.State.Should().Be(RegistrarDidState.FinishedState);
        response.DidState.Did.Should().Be(_testDid);
    }
    
    [Fact]
    public async Task DeactivateDid_ThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var request = CreateValidDeactivateRequest();
        
        // Reset all previous setups
        _mockMediator.Reset();
        
        // Set up initial validation to pass
        _mockMediator
            .Setup(m => m.Send(It.Is<GetOperationStatusRequest>(req => true), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail<GetOperationStatusResponse>("Not found"));
            
        // Force an exception during the actual operation 
        _mockMediator
            .Setup(m => m.Send(It.Is<CreateSignedAtalaOperationForDeactivateDidRequest>(req => true), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new System.InvalidOperationException("Simulated exception"));
        
        // Set up a more specific mock for the initial request validation to pass
        if (request.Options != null)
        {
            // Make sure these pass validation
            request.Options.WalletId = _testWalletId;
            request.Options.Network = _defaultAppSettings.PrismLedger.Name;
        }
        
        // Act
        var result = await _controller.DeactivateDid(_testDid, request);
        
        // Assert
        result.Should().BeOfType<ObjectResult>();
        var serverErrorResult = result as ObjectResult;
        serverErrorResult!.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        serverErrorResult.Value.Should().BeOfType<ProblemDetails>();
    }


    // Helper method to create a test OperationResultWrapper for testing
    private static OperationResultWrapper CreateTestOperationResultWrapper(OperationResultType type = OperationResultType.CreateDid)
    {
        // We need to create an instance that satisfies the AsCreateDid method return requirements
        // Create a valid internal DID document - simplified for tests
        
        // Create proper 32-byte arrays for the PrismPublicKey constructor
        var xBytes = new byte[32]; // Create a 32-byte array filled with zeros
        var yBytes = new byte[32]; // Create a 32-byte array filled with zeros
        
        // Set some values to make them unique but valid
        xBytes[0] = 10;
        xBytes[1] = 11;
        yBytes[0] = 12;
        yBytes[1] = 13;
        
        var internalDidDocument = new InternalDidDocument(
            didIdentifierIdentifier: "test123abc",
            publicKeys: new List<PrismPublicKey>
            {
                new PrismPublicKey(PrismKeyUsage.MasterKey, "masterKey", "secp256k1", xBytes, yBytes)
            },
            prismServices: new List<PrismService>(),
            contexts: new List<string> { "https://www.w3.org/ns/did/v1" },
            created: DateTime.UtcNow,
            versionId: "version1",
            cardanoTransactionPosition: 1,
            operationPosition: 1,
            originTxId: "originTx"
        );
        
        if (type == OperationResultType.CreateDid)
        {
            return new OperationResultWrapper(
                operationResultType: type,
                operationSequenceNumber: 1,
                internalDidDocument: internalDidDocument,
                signingKeyId: "signingKey1"
            );
        }
        else if (type == OperationResultType.UpdateDid)
        {
            var hash = Hash.CreateFrom(new byte[] { 1, 2, 3 });
            var actionResults = new List<UpdateDidActionResult>
            {
                new UpdateDidActionResult("keyId", false) // Using the constructor for RemoveKey
            };
            
            return new OperationResultWrapper(
                operationResultType: type,
                operationSequenceNumber: 1,
                didIdentifier: "test123abc",
                previousOperationHash: hash,
                updateDidActionResults: actionResults,
                operationBytes: new byte[32], // Use proper size
                signature: new byte[32], // Use proper size
                signingKeyId: "signingKey1"
            );
        }
        else // DeactivateDid
        {
            var hash = Hash.CreateFrom(new byte[] { 1, 2, 3 });
            
            return new OperationResultWrapper(
                operationResultType: type,
                operationSequenceNumber: 1,
                didIdentifier: "test123abc", 
                previousOperationHash: hash,
                operationBytes: new byte[32], // Use proper size
                signature: new byte[32], // Use proper size
                signingKeyId: "signingKey1"
            );
        }
    }
}