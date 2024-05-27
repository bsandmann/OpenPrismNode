namespace OpenPrismNode.Sync.PostgresModels;
#pragma warning disable CS8618

public class StakeAddress
{
    public long id { get; set; }
    public byte[] hash_raw { get; set; }
    /// <summary>
    /// stake-address ("stake_test1.....")
    /// </summary>
    public string view { get; set; }

    /// <summary>
    /// transaction-Id
    /// </summary>
    public long tx_id { get; set; }
}