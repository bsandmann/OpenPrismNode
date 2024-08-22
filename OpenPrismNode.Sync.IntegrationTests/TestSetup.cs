namespace OpenPrismNode.Sync.IntegrationTests;

using System.Reflection;

public static class TestSetup
{
// Note: The environment variable PostgreSqlConnectionString for Rider is
// set in the test-settings of the Test Runner.
    public static string PostgreSqlConnectionString =>
        System.Environment.GetEnvironmentVariable("DbSyncPostgresConnectionString");
}