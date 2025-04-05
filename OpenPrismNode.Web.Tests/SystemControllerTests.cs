using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Web.Common;
using OpenPrismNode.Web.Controller;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace OpenPrismNode.Web.Tests;

public class SystemControllerTests
{
    // Mocks
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<IOptions<AppSettings>> _mockAppSettings;
    private readonly Mock<ILogger<LedgersController>> _mockLogger;
    private readonly BackgroundSyncService _mockBackgroundSyncService;

    // System Under Test
    private readonly SystemController _controller;

    public SystemControllerTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockAppSettings = new Mock<IOptions<AppSettings>>();
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

        _mockAppSettings.Setup(ap => ap.Value).Returns(new AppSettings
        {
            PrismLedger = new PrismLedger { Name = "testnet" }
        });

        _controller = new SystemController(
            _mockMediator.Object,
            _mockHttpContextAccessor.Object,
            _mockAppSettings.Object,
            _mockLogger.Object,
            _mockBackgroundSyncService
        );
    }

    [Fact]
    public async Task HealthCheck_ReturnsOkResult()
    {
        // Act
        var result = await _controller.HealthCheck();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task HealthCheck_ReturnsCorrectVersionFormat()
    {
        // Act
        var result = await _controller.HealthCheck();

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        
        var value = okResult!.Value as string;
        value.Should().NotBeNull();
        value.Should().Contain("OpenPrismNode - Version ");

        // Verify version format (should be in format X.Y.Z)
        var versionRegex = new Regex(@"Version (\d+\.\d+\.\d+|unknown)$");
        versionRegex.IsMatch(value!).Should().BeTrue();
    }
}