namespace OpenPrismNode.Core.Common;

public class PrismLedger
{
    /// <summary>
    /// Name of the PRISM network for syncing
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Connection-String of the database which holds the PRISM data
    /// </summary>
    public string PrismPostgresConnectionString { get; set; }

    /// <summary>
    /// Connectionstring to the Postgres-DB, on which the dbSync is putting the PRISM data
    /// </summary>
    public string DbSyncPostgresConnectionString { get; set; }

    /// <summary>
    /// Usually the sync starts at epoch 0, but can be set to a different epoch, to start syncing from there
    /// This can be useful, if no PRISM data exists prior to a certain epoch
    /// </summary>
    public int StartAtEpochNumber { get; set; } = 0;

    /// <summary>
    /// Shows the network-identifier in the DID-Document e.g. did:prism:mainnet:123
    /// </summary>
    public bool IncludeNetworkIdentifier { get; set; }
}