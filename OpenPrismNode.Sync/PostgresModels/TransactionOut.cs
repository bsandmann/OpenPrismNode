namespace OpenPrismNode.Sync.PostgresModels;
#pragma warning disable CS8618

/// <summary>
/// Entitiy for the tx_out postgres table
/// </summary>
public class TransactionOut
{
    public long id { get; set; }
    public long tx_id { get; set; }
    public int index { get; set; }

    /// <summary>
    /// Lesabres adressen format "addr_test1....."
    /// </summary>
    public string address { get; set; }

    /// <summary>
    /// Keine Ahnung wie ich das Umwandel in die Adress, scheint mir aber das geeignetere Format zu sein
    /// </summary>
    public byte[] address_raw { get; set; }

    /// <summary>
    /// Verweis aud den Stake-adress-table
    /// </summary>
    public long stake_address_id { get; set; }
    
    /// <summary>
    /// Ausgehender betrag
    /// </summary>
    public long value { get; set; }
}