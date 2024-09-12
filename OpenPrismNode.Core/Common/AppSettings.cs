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
    /// API Key to access the interface to controll the sync-process
    /// </summary>
    public string AuthorizationKey { get; set; }

    /// <summary>
    /// The MetadataKey used for the PRISM-network 
    /// </summary>
    public int MetadataKey { get; set; } = 21325;

    /// <summary>
    /// The number of blocks that the current database tip must be behind the network tip before the node will start fast syncing through batching
    /// </summary>
    public int FastSyncBlockDistanceRequirement { get; set; } = 150;

    /// <summary>
    /// The number of blocks to batch together when fast syncing
    /// </summary>
    public  int FastSyncBatchSize { get; set; }= 2000;
}