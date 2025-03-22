namespace OpenPrismNode.Sync.IntegrationTests.GetPostgresBlockTip;

using Commands.DbSync.GetPostgresBlockTip;
using Core.Models;
using FluentAssertions;
using FluentResults.Extensions.FluentAssertions;

public class GetPostgresBlockTipTests
{
    [Fact]
    public async Task Getting_the_tip_of_the_dbsync_database_succeeds()
    {
        // Arrange
        var testFactory = new TestNpgsqlConnectionFactory(TestSetup.PostgreSqlConnectionString);
        var getPostgresBlockTipHandler = new GetPostgresBlockTipHandler(testFactory);

        // Act
        var result = await getPostgresBlockTipHandler.Handle(new GetPostgresBlockTipRequest(), new CancellationToken());

        // Assert
        result.Should().BeSuccess();
        result.Value.block_no.Should().BePositive();
    }
}