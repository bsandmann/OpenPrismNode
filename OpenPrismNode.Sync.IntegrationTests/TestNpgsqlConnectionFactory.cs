namespace OpenPrismNode.Sync.IntegrationTests;

using Npgsql;
using Services;

public class TestNpgsqlConnectionFactory : INpgsqlConnectionFactory
{
    private readonly string _connectionString;

    public TestNpgsqlConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public NpgsqlConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }
}