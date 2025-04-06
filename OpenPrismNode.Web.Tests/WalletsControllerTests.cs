using FluentAssertions;
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OpenPrismNode.Core.Commands.CreateCardanoWallet;
using OpenPrismNode.Core.Commands.GetWallet;
using OpenPrismNode.Core.Commands.GetWallets;
using OpenPrismNode.Core.Commands.GetWalletTransactions;
using OpenPrismNode.Core.Commands.RestoreWallet;
using OpenPrismNode.Core.Commands.Withdrawal;
using OpenPrismNode.Core.Commands.WriteTransaction;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Core.Models;
using OpenPrismNode.Core.Services;
using OpenPrismNode.Web.Controller;
using OpenPrismNode.Web.Models;
using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Google.Protobuf;

namespace OpenPrismNode.Web.Tests;

public class WalletsControllerTests
{
    // Mocks
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ICardanoWalletService> _mockWalletService;
    private readonly Mock<IOptions<AppSettings>> _mockAppSettings;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<ILogger<LedgersController>> _mockLogger;
    private readonly BackgroundSyncService _mockBackgroundSyncService;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;

    // System Under Test
    private readonly WalletsController _controller;

    // Test Data
    private readonly string _testWalletId = "wallet-123";
    private readonly string _testWalletName = "Test Wallet";
    private readonly List<string> _testMnemonic = new List<string> { "word1", "word2", "word3", "word4", "word5", "word6", "word7", "word8", "word9", "word10", "word11", "word12" };
    private readonly string _testFundingAddress = "addr1test12345";
    private readonly long _testBalance = 1000000;

    public WalletsControllerTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockWalletService = new Mock<ICardanoWalletService>();
        _mockAppSettings = new Mock<IOptions<AppSettings>>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockLogger = new Mock<ILogger<LedgersController>>();
        // Create mock objects for the BackgroundSyncService dependencies
        var mockSyncServiceLogger = Mock.Of<ILogger<BackgroundSyncService>>();
        var mockSyncAppSettings = new Mock<IOptions<AppSettings>>();
        mockSyncAppSettings.Setup(x => x.Value).Returns(new AppSettings());
        var mockSyncServiceScopeFactory = Mock.Of<IServiceScopeFactory>();

        // Create the actual BackgroundSyncService with mocked dependencies
        _mockBackgroundSyncService = new BackgroundSyncService(
            mockSyncAppSettings.Object,
            mockSyncServiceLogger,
            mockSyncServiceScopeFactory
        );
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();

        _mockAppSettings.Setup(ap => ap.Value).Returns(new AppSettings());

        _controller = new WalletsController(
            _mockMediator.Object,
            _mockHttpContextAccessor.Object,
            _mockAppSettings.Object,
            _mockLogger.Object,
            _mockBackgroundSyncService,
            _mockHttpClientFactory.Object,
            _mockWalletService.Object
        );
    }

    [Fact]
    public async Task CreateWallet_Success_ReturnsWalletIdAndMnemonic()
    {
        // Arrange
        var request = new CreateWalletRequestModel { Name = _testWalletName };
        
        _mockMediator
            .Setup(m => m.Send(It.Is<CreateCardanoWalletRequest>(r => r.Name == _testWalletName), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new CreateCardanoWalletResponse { WalletId = _testWalletId, Mnemonic = _testMnemonic }));

        // Act
        var result = await _controller.CreateWallet(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as CreateWalletResponseModel;
        
        response.Should().NotBeNull();
        response!.WalletId.Should().Be(_testWalletId);
        response.Mnemonic.Should().BeEquivalentTo(_testMnemonic);
    }

    [Fact]
    public async Task CreateWallet_Failure_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateWalletRequestModel { Name = _testWalletName };
        var errorMessage = "Failed to create wallet";
        
        _mockMediator
            .Setup(m => m.Send(It.IsAny<CreateCardanoWalletRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(errorMessage));

        // Act
        var result = await _controller.CreateWallet(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().Be(errorMessage);
    }

    [Fact]
    public async Task RestoreWallet_Success_ReturnsWalletId()
    {
        // Arrange
        var mnemonicList = _testMnemonic;
        var request = new RestoreWalletRequestModel 
        { 
            Name = _testWalletName,
            Mnemonic = mnemonicList
        };
        
        _mockMediator
            .Setup(m => m.Send(It.Is<RestoreCardanoWalletRequest>(r => 
                r.Name == _testWalletName && r.Mnemonic.SequenceEqual(mnemonicList)), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new RestoreCardanoWalletResponse { WalletId = _testWalletId }));

        // Act
        var result = await _controller.RestoreWallet(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as RestoreWalletResponseModel;
        
        response.Should().NotBeNull();
        response!.WalletId.Should().Be(_testWalletId);
    }

    [Fact]
    public async Task RestoreWallet_Failure_ReturnsBadRequest()
    {
        // Arrange
        var mnemonicList = _testMnemonic;
        var request = new RestoreWalletRequestModel 
        { 
            Name = _testWalletName,
            Mnemonic = mnemonicList
        };
        var errorMessage = "Invalid mnemonic";
        
        _mockMediator
            .Setup(m => m.Send(It.IsAny<RestoreCardanoWalletRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(errorMessage));

        // Act
        var result = await _controller.RestoreWallet(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().Be(errorMessage);
    }

    [Fact]
    public async Task GetWallet_Success_ReturnsWalletDetails()
    {
        // Arrange
        _mockMediator
            .Setup(m => m.Send(It.Is<GetWalletRequest>(r => r.WalletId == _testWalletId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new GetWalletResponse 
            { 
                WalletId = _testWalletId,
                Balance = _testBalance,
                FundingAddress = _testFundingAddress,
                SyncingComplete = true,
                SyncProgress = 100
            }));

        // Act
        var result = await _controller.GetWallet(_testWalletId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as GetWalletResponseModel;
        
        response.Should().NotBeNull();
        response!.WalletId.Should().Be(_testWalletId);
        response.Balance.Should().Be(_testBalance);
        response.FundingAddress.Should().Be(_testFundingAddress);
        response.SyncingComplete.Should().BeTrue();
        response.SyncProgress.Should().Be(100);
    }

    [Fact]
    public async Task GetWallet_Failure_ReturnsBadRequest()
    {
        // Arrange
        var errorMessage = "Wallet not found";
        
        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetWalletRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(errorMessage));

        // Act
        var result = await _controller.GetWallet(_testWalletId);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().Be(errorMessage);
    }

    [Fact]
    public async Task GetWallets_Success_ReturnsAllWallets()
    {
        // Arrange
        var wallet1 = new GetWalletResponse
        {
            WalletId = _testWalletId,
            Balance = _testBalance,
            FundingAddress = _testFundingAddress,
            SyncingComplete = true,
            SyncProgress = 100
        };
        
        var wallet2 = new GetWalletResponse
        {
            WalletId = "wallet-456",
            Balance = 500000,
            FundingAddress = "addr1test67890",
            SyncingComplete = false,
            SyncProgress = 50
        };
        
        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetWalletsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new List<GetWalletResponse> { wallet1, wallet2 }));

        // Act
        var result = await _controller.GetWallets();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as List<GetWalletResponseModel>;
        
        response.Should().NotBeNull();
        response!.Count.Should().Be(2);
        response[0].WalletId.Should().Be(_testWalletId);
        response[1].WalletId.Should().Be("wallet-456");
    }



    [Fact]
    public async Task ExecuteTransaction_EmptyBody_ReturnsBadRequest()
    {
        // Arrange
        // Setup mock for Request.Body with empty string
        var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(""));
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = memoryStream;
        
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        
        // Set up controller with the mock context
        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = httpContext
        };

        // Act
        var result = await _controller.ExecuteTransaction(_testWalletId);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().Be("Input string is empty or null");
    }

    [Fact]
    public async Task ExecuteTransaction_InvalidInput_ReturnsBadRequest()
    {
        // Arrange
        // Setup mock for Request.Body with invalid input
        var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes("invalid-input"));
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = memoryStream;
        
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        
        // Set up controller with the mock context
        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = httpContext
        };

        // Act
        var result = await _controller.ExecuteTransaction(_testWalletId);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.ToString().Should().Contain("Error parsing the input");
    }

    [Fact]
    public async Task GetTransactions_Success_ReturnsTransactions()
    {
        // Arrange
        var transaction1 = new GetWalletTransactionsReponse
        {
            WalletId = _testWalletId,
            TransactionId = "tx1",
            OperationStatusId = new byte[] { 1, 2, 3 },
            OperationHash = new byte[] { 4, 5, 6 },
            OperationType = OperationTypeEnum.CreateDid,
            Status = OperationStatusEnum.ConfirmedAndApplied,
            Fee = 1000
        };
        
        var transaction2 = new GetWalletTransactionsReponse
        {
            WalletId = _testWalletId,
            TransactionId = "tx2",
            OperationStatusId = new byte[] { 7, 8, 9 },
            OperationHash = new byte[] { 10, 11, 12 },
            OperationType = OperationTypeEnum.UpdateDid,
            Status = OperationStatusEnum.PendingSubmission,
            Fee = 2000
        };
        
        _mockMediator
            .Setup(m => m.Send(It.Is<GetWalletTransactionsRequest>(r => r.WalletId == _testWalletId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new List<GetWalletTransactionsReponse> { transaction1, transaction2 }));

        // Act
        var result = await _controller.GetTransactions(_testWalletId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as IEnumerable<GetWalletTransactionsResponseModel>;
        
        response.Should().NotBeNull();
        response!.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTransactions_Failure_ReturnsBadRequest()
    {
        // Arrange
        var errorMessage = "Failed to get transactions";
        
        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetWalletTransactionsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(errorMessage));

        // Act
        var result = await _controller.GetTransactions(_testWalletId);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().Be(errorMessage);
    }

    [Fact]
    public async Task Withdrawal_Success_ReturnsOk()
    {
        // Arrange
        var withdrawalAddress = "addr_withdrawal_test123";
        
        _mockMediator
            .Setup(m => m.Send(It.Is<WithdrawalRequest>(r => 
                r.WalletId == _testWalletId && r.WithdrawalAddress == withdrawalAddress), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await _controller.Withdrawal(_testWalletId, withdrawalAddress);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task Withdrawal_Failure_ReturnsBadRequest()
    {
        // Arrange
        var withdrawalAddress = "addr_withdrawal_test123";
        var errorMessage = "Insufficient funds";
        
        _mockMediator
            .Setup(m => m.Send(It.IsAny<WithdrawalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(errorMessage));

        // Act
        var result = await _controller.Withdrawal(_testWalletId, withdrawalAddress);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().Be(errorMessage);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void IsValidBase64_WithEmptyString_ReturnsFalse(string input)
    {
        // Use reflection to test the private method
        var methodInfo = typeof(WalletsController).GetMethod("IsValidBase64", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        // Act
        var result = methodInfo != null ? (bool)methodInfo.Invoke(_controller, new object[] { input }) : false;
        
        // Assert
        result.Should().BeFalse();
    }
}