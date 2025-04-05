// using directives for your project's namespaces and testing libraries

using FluentAssertions;
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OpenPrismNode.Core.Commands.GetOperationStatus;
using OpenPrismNode.Core.Commands.Registrar;
using OpenPrismNode.Core.Commands.Registrar.CreateSignedAtalaOperationForCreateDid;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Web.Controller;
using OpenPrismNode.Web.Models;


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

        _mockMediator.Verify(m => m.Send(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()), Times.Never); // Should fail before Mediator call
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
        // Check details if needed
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
        // Check details if needed
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
        // Check details if needed
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
        // Check error message relates to hex parsing
    }

    [Fact]
    public async Task CreateDid_ExistingJob_GetOperationStatusFails_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateValidCreateRequest(jobId: _testJobIdHex);

        _mockMediator // GetOperationStatus
            .Setup(m => m.Send(It.Is<GetOperationStatusRequest>(r => r.OperationStatusId.SequenceEqual(_testJobIdBytes)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail("DB Error fetching status"));

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
}