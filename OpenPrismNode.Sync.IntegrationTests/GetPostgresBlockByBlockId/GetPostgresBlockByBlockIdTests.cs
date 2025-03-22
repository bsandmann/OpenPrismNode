using FluentAssertions;
using FluentResults.Extensions.FluentAssertions;
using OpenPrismNode.Sync.Commands.DbSync.GetPostgresBlockByBlockId;
using OpenPrismNode.Sync.IntegrationTests;

public class GetPostgresBlockByBlockId
{
    [Fact]
    public async Task Getting_a_specific_block_from_the_dbsync_database_succeeds()
    {
        // Arrange
        var testFactory = new TestNpgsqlConnectionFactory(TestSetup.PostgreSqlConnectionString);
        var getPostgresBlockByBlockIdHandler = new GetPostgresBlockByBlockIdHandler(testFactory);
        var blockId = 1000;

        // Act
        var result = await getPostgresBlockByBlockIdHandler.Handle(new GetPostgresBlockByBlockIdRequest(blockId), new CancellationToken());

        // Assert
        result.Should().BeSuccess();
        result.Value.block_no.Should().BeGreaterThan(900);
        result.Value.epoch_no.Should().BePositive();
        result.Value.id.Should().Be(1000);
        result.Value.tx_count.Should().BeGreaterOrEqualTo(0);
        result.Value.time.Should().BeAfter(DateTime.MinValue);
        result.Value.time.Should().BeBefore(DateTime.UtcNow);
    }
}