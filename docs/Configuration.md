# OpenPrismNode Configuration Documentation

This document provides a comprehensive overview of the configuration parameters used in OpenPrismNode. The configuration
is primarily defined in the `appsettings.json` file and can be overwritten in the docker-compose file. See the [Guide_blockfrost.md](Guide_blockfrost.md) or [Guide_DbSync.md](Guide_DbSync.md) for the required patterns.

## Core Configuration Parameters

| Parameter                          | Type    | Default | Description                                                                                                                                                                                                                              |
|------------------------------------|---------|---------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `DisableSync`                      | boolean | `false` | Disables the blockchain synchronization process. Useful for testing purposes when you don't want the application to actively sync with the blockchain.                                                                                   |
| `SkipMigration`                    | boolean | `false` | When set to true, skips the automatic database migration at startup. Use this if you want to manage migrations manually or during specific maintenance windows.                                                                          |
| `WalletCacheSize`                  | integer | `10000` | Sets the size of the in-memory cache for wallet and staking addresses. This cache improves performance by reducing database queries for repeated address lookups. Recommended minimum value is 1000.                                     |
| `DelayBetweenSyncsInMs`            | integer | `5000`  | Defines the time interval (in milliseconds) between blockchain synchronization cycles. Lower values increase sync frequency but may increase system load.                                                                                |
| `FastSyncBlockDistanceRequirement` | integer | `150`   | The number of blocks that the current database must be behind the network tip before the node will start fast syncing through batching.                                                                                                  |
| `FastSyncBatchSize`                | integer | `1000`  | The number of blocks to process together in a batch when performing a fast sync operation. Higher values may improve sync speed but require more memory.                                                                                 |
| `RequiredConfirmationDepth`        | integer | `2`     | The minimum number of block confirmations required before an operation is considered confirmed and returned to clients. This affects when operations are reported as completed, but doesn't affect when they're written to the database. |
| `MetadataKey`                      | integer | `21325` | The key used to identify PRISM-related metadata in Cardano transactions (not visible in appsettings.json but has a default value in AppSettings.cs).                                                                                     |
| `AdminAuthorizationKey`            | string  | N/A     | API key for administrative access to control the sync process and access the admin interface. Should be a strong, unique value that is kept secret.                                                                                      |
| `UserAuthorizationKey`             | string  | N/A     | API key that provides access to the wallet interface for creating new wallets and viewing wallet status. Should be a strong, unique value that is kept secret.                                                                           |

## API and Service Endpoints

| Parameter                           | Type    | Default | Description                                                                                                                                                            |
|-------------------------------------|---------|---------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `IngestionEndpoint`                 | string  | `""`    | URL endpoint where newly created DIDs are sent to. Should be a valid URI if DID ingestion functionality is required.                                                   |
| `IngestionEndpointAuthorizationKey` | string  | `""`    | API key for authenticating with the DID ingestion endpoint. Required if the ingestion endpoint uses API key authentication.                                            |
| `CardanoWalletApiEndpoint`          | string  | `""`    | Base URL of the Cardano Wallet API service. This should point to your Cardano wallet backend service.                                                                  |
| `CardanoWalletApiEndpointPort`      | integer | `8090`  | Port number for the Cardano Wallet API service.                                                                                                                        |
| `ApiHttpsPort`                      | integer | `5001`  | Port for the HTTPS API endpoint.                                                                                                                                       |
| `ApiHttpPortUi`                     | integer | `5001`  | Display port for the HTTP API endpoint in the UI. Useful when using port forwarding or reverse proxies.                                                                |
| `GrpcPort`                          | integer | `50053` | Port for the gRPC service endpoint.                                                                                                                                    |
| `GrpcPortUi`                        | integer | `50053` | Display port for the gRPC endpoint in the UI. Useful when using port forwarding or reverse proxies.                                                                    |
| `DefaultWalletIdForGrpc`            | string  | N/A     | Wallet ID to use as the default wallet for gRPC operations (e.g. when usign Identus cloud agent) when no specific wallet is specified. Format is a hexadecimal string. |

## PrismLedger Configuration

This section configures the PRISM network connection parameters.

| Parameter                                    | Type    | Default     | Description                                                                                                                                                        |
|----------------------------------------------|---------|-------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `PrismLedger.Name`                           | string  | `"mainnet"` | Name of the PRISM network for syncing (e.g., "mainnet", "preprod").                                                                                                |
| `PrismLedger.PrismPostgresConnectionString`  | string  | `""`        | Connection string for the PostgreSQL database that stores PRISM data. Format: `"Host=hostname;Database=dbname;Username=user;Password=pass"`                        |
| `PrismLedger.DbSyncPostgresConnectionString` | string  | `""`        | Connection string for the PostgreSQL database used by dbSync to write PRISM blockchain data. Format: `"Host=hostname;Database=dbname;Username=user;Password=pass"` |
| `PrismLedger.StartAtEpochNumber`             | integer | `1`         | The epoch number from which to start syncing. Useful for skipping epochs with no PRISM data.                                                                       |
| `PrismLedger.IncludeNetworkIdentifier`       | boolean | `false`     | When true, includes the network identifier in DID documents (e.g., `did:prism:mainnet:123` or `did:prism:preprod:123`).                                             |

## Data Source Configuration

| Parameter                 | Type   | Default    | Description                                                                                        |
|---------------------------|--------|------------|----------------------------------------------------------------------------------------------------|
| `SyncDataSource.Provider` | string | `"DbSync"` | The provider to use for blockchain data retrieval. Valid options are `"DbSync"` or `"Blockfrost"`. |

## Blockfrost Configuration

Configuration for the Blockfrost API service, used when `SyncDataSource.Provider` is set to `"Blockfrost"`.

| Parameter            | Type   | Default | Description                                                                                                                                                                   |
|----------------------|--------|---------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Blockfrost.BaseUrl` | string | `""`    | Base URL for the Blockfrost API. Should be set to the appropriate endpoint for the network you're using (e.g., `"https://cardano-mainnet.blockfrost.io/api/v0"` for mainnet). |
| `Blockfrost.ApiKey`  | string | `""`    | API key for authenticating with the Blockfrost service. Required to access the Blockfrost API. Should be obtained from the Blockfrost dashboard.                              |


## Seq Logging Configuration

Configuration for the Seq logging service. 

| Parameter          | Type   | Default                   | Description                                                                                                                                        |
|--------------------|--------|---------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------|
| `Seq.ServerUrl`    | string | `"http://localhost:5341"` | URL of the Seq logging server.                                                                                                                     |
| `Seq.ApiKey`       | string | `""`                      | API key for authenticating with the Seq server. Required if the Seq server uses API key authentication.                                            |
| `Seq.MinimumLevel` | string | `"Warning"`               | Minimum logging level for events sent to Seq. Valid values: `"Trace"`, `"Debug"`, `"Information"`, `"Warning"`, `"Error"`, `"Critical"`, `"None"`. |


## Example Configuration

```json
{
  "AppSettings": {
    "DisableSync": false,
    "SkipMigration": false,
    "WalletCacheSize": 10000,
    "DelayBetweenSyncsInMs": 5000,
    "FastSyncBlockDistanceRequirement": 150,
    "FastSyncBatchSize": 1000,
    "RequiredConfirmationDepth": 2,
    "PrismLedger": {
      "Name": "mainnet",
      "PrismPostgresConnectionString": "Host=prism-db;Database=prism;Username=dbuser;Password=dbpassword",
      "DbSyncPostgresConnectionString": "Host=dbsync-db;Database=dbsync;Username=dbuser;Password=dbpassword",
      "StartAtEpochNumber": 1,
      "IncludeNetworkIdentifier": false
    },
    "IngestionEndpoint": "https://ingestion.example.com/api/dids",
    "IngestionEndpointAuthorizationKey": "your-ingestion-api-key",
    "CardanoWalletApiEndpoint": "http://cardano-wallet",
    "CardanoWalletApiEndpointPort": 8090,
    "DefaultWalletIdForGrpc": "a4e8d89055c08493b297883392e730008153b6c1",
    "ApiHttpsPort": 5001,
    "ApiHttpPortUi": 5001,
    "GrpcPort": 50053,
    "GrpcPortUi": 50053,
    "SyncDataSource": {
      "Provider": "Blockfrost"
    },
    "Blockfrost": {
      "BaseUrl": "https://cardano-mainnet.blockfrost.io/api/v0",
      "ApiKey": "your-blockfrost-api-key"
    }
  }
}
```