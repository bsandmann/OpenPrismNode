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
    public PrismNetwork PrismNetwork { get; set; }
    
    /// <summary>
    /// API Key to access the interface to controll the sync-process
    /// </summary>
    public string AuthorizationKey { get; set; }
    
}
