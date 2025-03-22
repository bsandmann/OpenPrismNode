namespace OpenPrismNode.Sync.IntegrationTests.GetPostgresBlockByBlockNo;

using Commands.DbSync.GetPostgresBlockByBlockNo;
using FluentAssertions;
using FluentResults.Extensions.FluentAssertions;

public class GetPostgresBlockByBlockNo
{
    [Fact]
    public async Task Getting_a_specific_block_from_the_dbsync_database_succeeds()
    {
        // Arrange
        var testFactory = new TestNpgsqlConnectionFactory(TestSetup.PostgreSqlConnectionString);
        var getPostgresBlockByBlockNoHandler = new GetPostgresBlockByBlockNoHandler(testFactory);
        var blocknumber = 1000;

        // Act
        var result = await getPostgresBlockByBlockNoHandler.Handle(new GetPostgresBlockByBlockNoRequest(blocknumber), new CancellationToken());

        // Assert
        result.Should().BeSuccess();
        result.Value.block_no.Should().Be(1000);
        result.Value.epoch_no.Should().BePositive();
        result.Value.id.Should().BePositive();
        result.Value.tx_count.Should().BeGreaterOrEqualTo(0);
        result.Value.time.Should().BeAfter(DateTime.MinValue);
        result.Value.time.Should().BeBefore(DateTime.UtcNow);
        result.Value.previous_id.Should().NotBe(null);
        result.Value.previousHash.Should().NotBeNull();
    }
}