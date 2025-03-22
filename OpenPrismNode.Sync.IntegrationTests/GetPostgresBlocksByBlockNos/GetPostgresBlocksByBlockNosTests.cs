namespace OpenPrismNode.Sync.IntegrationTests.GetPostgresBlocksByBlockNos;

using Commands.DbSync.GetPostgresBlocksByBlockNos;
using FluentAssertions;
using FluentResults.Extensions.FluentAssertions;
using OpenPrismNode.Core.Models;

public class GetPostgresBlocksByBlockNosTests
{
    private readonly TestNpgsqlConnectionFactory _testFactory;
    private readonly GetPostgresBlocksByBlockNosHandler _handler;

    public GetPostgresBlocksByBlockNosTests()
    {
        _testFactory = new TestNpgsqlConnectionFactory(TestSetup.PostgreSqlConnectionString);
        _handler = new GetPostgresBlocksByBlockNosHandler(_testFactory);
    }

    [Fact]
    public async Task Getting_multiple_blocks_succeeds()
    {
        // Arrange
        var startBlockNo = 1000;
        var count = 10;

        // Act
        var result = await _handler.Handle(new GetPostgresBlocksByBlockNosRequest(startBlockNo, count), CancellationToken.None);

        // Assert
        result.Should().BeSuccess();
        result.Value.Should().HaveCount(count);
        result.Value.Should().AllSatisfy(block =>
        {
            block.block_no.Should().BeGreaterOrEqualTo(startBlockNo);
            block.block_no.Should().BeLessThan(startBlockNo + count);
            block.epoch_no.Should().BePositive();
            block.id.Should().BePositive();
            block.tx_count.Should().BeGreaterOrEqualTo(0);
            block.time.Should().BeAfter(DateTime.MinValue);
            block.time.Should().BeBefore(DateTime.UtcNow);
            block.hash.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public async Task Getting_blocks_with_some_missing_returns_available_blocks()
    {
        // Arrange
        var startBlockNo = 1_000_000_000; // Assuming this is a high number that does not exist in the test database
        var count = 10;

        // Act
        var result = await _handler.Handle(new GetPostgresBlocksByBlockNosRequest(startBlockNo, count), CancellationToken.None);

        // Assert
        result.Should().BeFailure();
        result.Errors.Should().ContainSingle().Which.Message.Should().Be("No blocks found in the specified range");
    }


    [Fact]
    public async Task Getting_blocks_returns_correct_order()
    {
        // Arrange
        var startBlockNo = 1000;
        var count = 10;

        // Act
        var result = await _handler.Handle(new GetPostgresBlocksByBlockNosRequest(startBlockNo, count), CancellationToken.None);

        // Assert
        result.Should().BeSuccess();
        result.Value.Should().BeInAscendingOrder(block => block.block_no);
    }
}