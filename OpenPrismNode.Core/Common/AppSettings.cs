namespace OpenPrismNode.Core.Common;

#pragma warning disable CS8618
/// <summary>
/// AppSettings-Configuration for the app
/// </summary>
public class AppSettings
{
    // /// <summary>
    // /// Time between the Postgres-DB is checked for new entries
    // /// </summary>
    public int DelayBetweenSyncsInMs { get; set; } = 2500;

    /// <summary>
    /// Sets the size of the wallet-cache, to see if wallet or staking-addresses already
    /// exits in the database. If the cache is hit, the prevents the sync-part of the
    /// software to ask the database for the same address again. Minor performance improvement.
    /// Recommended size at least: 1000
    /// </summary>
    public int WalletCacheSize { get; set; } = 1000;

    /// <summary>
    /// Configuration of all supported PRISM-networks for which data is in the postgres-db
    /// </summary>
    public PrismLedger PrismLedger { get; set; }

    /// <summary>
    /// API Key to access the interface to controll the sync-process as well as the Admin-Interface
    /// to see all Wallets
    /// </summary>
    public string AdminAuthorizationKey { get; set; }

    /// <summary>
    /// API Key to access the Wallet-Interface to create a new Wallet and to see the Wallet-Status
    /// </summary>
    public string UserAuthorizationKey { get; set; }


    /// <summary>
    /// If a operation/block has reached the specific depth (evaluated by the Cardano-Wallet),
    /// It is returned back by the GetOperationStatusHandler to the Client (e.g. Idenuts via gRPC)
    /// The depth has no relavance for when a operation is written into the database and for the syncing
    /// in general. It is just a information for client which just wrote a operation.
    /// </summary>
    public int? RequiredConfirmationDepth { get; set; }

    /// <summary>
    /// The MetadataKey used for the PRISM-network 
    /// </summary>
    public int MetadataKey { get; set; } = 21325;

    /// <summary>
    /// The number of blocks that the current database tip must be behind the network tip before the node will start fast syncing through batching
    /// </summary>
    public int FastSyncBlockDistanceRequirement { get; set; } = 1500;

    /// <summary>
    /// The number of blocks to batch together when fast syncing
    /// </summary>
    public int FastSyncBatchSize { get; set; } = 2000;

    /// <summary>
    /// Endpoint where newly created DIDs are sent to
    /// </summary>
    public Uri? IngestionEndpoint { get; set; }

    /// <summary>
    /// API key for the ingestion endpoint
    /// </summary>
    public string? IngestionEndpointAuthorizationKey { get; set; }

    /// <summary>
    /// Endpoint for the Cardano-Wallet-API
    /// e.g. "http://1.2.3.4"
    /// </summary>
    public string CardanoWalletApiEndpoint { get; set; }
    
    /// <summary>
    /// Port for the Cardano-Wallet-API
    /// eg. 8090
    /// </summary>
    public int? CardanoWalletApiEndpointPort { get; set; }
    
    /// <summary>
    /// Port for the API
    /// </summary>
    public int ApiHttpsPort { get; set; } = 5001;
    
    /// <summary>
    /// Display of Port for the HTTP API endpoint in the UI
    /// Useful if some redirection happens because of a firewall
    /// </summary>
    public int ApiHttpPortUi { get; set; } = 5001;

    /// <summary>
    /// Port for the gRPC endpoint
    /// </summary>
    public int GrpcPort { get; set; } = 50053;

    /// <summary>
    /// Display of Port for the gRPC endpoint in the UI
    /// Useful if some redirection happens because of a firewall
    /// </summary>
    public int GrpcPortUi { get; set; } = 50053;

    /// <summary>
    /// The data source provider to use for blockchain data retrieval
    /// </summary>
    public SyncDataSourceOptions SyncDataSource { get; set; } = new();
    
    /// <summary>
    /// Configuration for the Blockfrost API
    /// </summary>
    public BlockfrostOptions Blockfrost { get; set; } = new();
}

/// <summary>
/// Configuration options for the sync data source
/// </summary>
#pragma warning disable CS8618
public class SyncDataSourceOptions
{
    /// <summary>
    /// The provider to use for blockchain data retrieval: "DbSync" or "Blockfrost"
    /// </summary>
    public string Provider { get; set; } = "DbSync";
}

/// <summary>
/// Configuration options for the Blockfrost API
/// </summary>
#pragma warning disable CS8618
public class BlockfrostOptions
{
    /// <summary>
    /// The base URL for the Blockfrost API
    /// </summary>
    public string? BaseUrl { get; set; }
    
    /// <summary>
    /// The API key for the Blockfrost API
    /// </summary>
    public string? ApiKey { get; set; }
}