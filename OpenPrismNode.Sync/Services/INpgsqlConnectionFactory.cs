namespace OpenPrismNode.Sync.Services;

using Core.Common;
using Microsoft.Extensions.Options;
using Npgsql;

public interface INpgsqlConnectionFactory
{
    NpgsqlConnection CreateConnection();
}

public class NpgsqlConnectionFactory : INpgsqlConnectionFactory
{
    private readonly string _connectionString;

    public NpgsqlConnectionFactory(IOptions<AppSettings> appSettings)
    {
        _connectionString = appSettings.Value.PrismLedger.DbSyncPostgresConnectionString;
    }

    public NpgsqlConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }
}