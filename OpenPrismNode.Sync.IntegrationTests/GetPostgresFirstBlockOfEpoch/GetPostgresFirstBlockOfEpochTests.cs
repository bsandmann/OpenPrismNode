namespace OpenPrismNode.Sync.IntegrationTests.GetPostgresFirstBlockOfEpoch;

using Commands.DbSync.GetPostgresBlockByBlockNo;
using Commands.DbSync.GetPostgresFirstBlockOfEpoch;
using FluentAssertions;
using FluentResults.Extensions.FluentAssertions;
using OpenPrismNode.Core.Models;

public class GetPostgresFirstBlockOfEpochTests
{
    private readonly TestNpgsqlConnectionFactory _testFactory;
    private readonly GetPostgresFirstBlockOfEpochHandler _handler;
    private readonly GetPostgresBlockByBlockNoHandler _getBlockHandler;

    public GetPostgresFirstBlockOfEpochTests()
    {
        _testFactory = new TestNpgsqlConnectionFactory(TestSetup.PostgreSqlConnectionString);
        _handler = new GetPostgresFirstBlockOfEpochHandler(_testFactory);
        _getBlockHandler = new GetPostgresBlockByBlockNoHandler(_testFactory);
    }

    [Fact]
    public async Task Getting_first_block_of_valid_epoch_succeeds()
    {
        // Arrange
        int epochNumber = 10; 

        // Act
        var result = await _handler.Handle(new GetPostgresFirstBlockOfEpochRequest(epochNumber), CancellationToken.None);

        // Assert
        result.Should().BeSuccess();
        result.Value.Should().NotBeNull();
        result.Value.epoch_no.Should().Be(epochNumber);
        result.Value.block_no.Should().BePositive();
        result.Value.time.Should().BeAfter(DateTime.MinValue);
        result.Value.time.Should().BeBefore(DateTime.UtcNow);
        result.Value.hash.Should().NotBeNullOrEmpty();

        var priorBlock = await _getBlockHandler.Handle(new GetPostgresBlockByBlockNoRequest(result.Value.block_no - 1), CancellationToken.None);
        priorBlock.Should().BeSuccess();
        priorBlock.Value.epoch_no.Should().Be(epochNumber - 1);
    }

    [Fact]
    public async Task Getting_first_block_of_nonexistent_epoch_fails()
    {
        // Arrange
        int nonexistentEpochNumber = int.MaxValue; 

        // Act
        var result = await _handler.Handle(new GetPostgresFirstBlockOfEpochRequest(nonexistentEpochNumber), CancellationToken.None);

        // Assert
        result.Should().BeFailure();
        result.Errors.Should().ContainSingle().Which.Message.Should().Contain($"No block found for epoch {nonexistentEpochNumber}");
    }

    [Fact]
    public async Task Getting_first_block_of_epoch_zero_succeeds()
    {
        // Arrange
        int epochNumber = 0;

        // Act
        var result = await _handler.Handle(new GetPostgresFirstBlockOfEpochRequest(epochNumber), CancellationToken.None);

        // Assert
        result.Should().BeSuccess();
        result.Value.Should().NotBeNull();
        result.Value.epoch_no.Should().Be(epochNumber);
        result.Value.block_no.Should().Be(1); // Assuming block 1 is the first block of epoch 0
    }

    [Fact]
    public async Task Getting_first_block_of_consecutive_epochs_returns_different_blocks()
    {
        // Arrange
        int firstEpoch = 1;
        int secondEpoch = 2;

        // Act
        var resultFirstEpoch = await _handler.Handle(new GetPostgresFirstBlockOfEpochRequest(firstEpoch), CancellationToken.None);
        var resultSecondEpoch = await _handler.Handle(new GetPostgresFirstBlockOfEpochRequest(secondEpoch), CancellationToken.None);

        // Assert
        resultFirstEpoch.Should().BeSuccess();
        resultSecondEpoch.Should().BeSuccess();

        resultFirstEpoch.Value.block_no.Should().BeLessThan(resultSecondEpoch.Value.block_no);
        resultFirstEpoch.Value.time.Should().BeBefore(resultSecondEpoch.Value.time);
    }
}