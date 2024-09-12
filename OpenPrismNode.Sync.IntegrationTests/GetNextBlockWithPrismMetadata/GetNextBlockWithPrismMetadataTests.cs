namespace OpenPrismNode.Sync.IntegrationTests.GetNextBlockWithPrismMetadata;

using FluentAssertions;
using FluentResults;
using FluentResults.Extensions.FluentAssertions;
using OpenPrismNode.Core.Models;
using OpenPrismNode.Core;
using Moq;
using LazyCache;
using LazyCache.Testing.Moq;
using Microsoft.Extensions.Logging;

public class GetNextBlockWithPrismMetadataTests
{
    private readonly TestNpgsqlConnectionFactory _testFactory;
    private readonly GetNextBlockWithPrismMetadataHandler _handler;
    private readonly IAppCache _mockCache;
    private readonly Mock<ILogger<GetNextBlockWithPrismMetadataHandler>> _mockLogger;

    public GetNextBlockWithPrismMetadataTests()
    {
        _testFactory = new TestNpgsqlConnectionFactory(TestSetup.PostgreSqlConnectionString);
        _mockCache = Create.MockedCachingService();
        _mockLogger = new Mock<ILogger<GetNextBlockWithPrismMetadataHandler>>();
        _handler = new GetNextBlockWithPrismMetadataHandler(_testFactory, _mockLogger.Object, _mockCache);
    }

    [Fact]
    public async Task Getting_next_block_with_prism_metadata_succeeds()
    {
        // Arrange
        int startBlockHeight = 1;
        int metadataKey = 21325;
        int maxBlockHeight = 1000;
        var request = new GetNextBlockWithPrismMetadataRequest(startBlockHeight, metadataKey, maxBlockHeight, LedgerType.CardanoPreprod);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeSuccess();
        result.Value.Should().NotBeNull();
        result.Value.BlockHeight.Should().Be(179967);
        result.Value.EpochNumber.Should().Be(28);
    }

    [Fact]
    public async Task Getting_next_block_returns_consistent_results_for_consecutive_calls()
    {
        // Arrange
        int startBlockHeight = 1;
        int metadataKey = 21325;
        int maxBlockHeight = 1000;
        var request = new GetNextBlockWithPrismMetadataRequest(startBlockHeight, metadataKey, maxBlockHeight, LedgerType.CardanoPreprod);

        // Act
        var result1 = await _handler.Handle(request, CancellationToken.None);
        var result2 = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result1.Should().BeSuccess();
        result2.Should().BeSuccess();
        result1.Value.Should().NotBeNull();
        result2.Value.Should().NotBeNull();
        result1.Value.BlockHeight.Should().Be(result2.Value.BlockHeight);
        result1.Value.EpochNumber.Should().Be(result2.Value.EpochNumber);
    }
}