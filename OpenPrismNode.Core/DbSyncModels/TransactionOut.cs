namespace OpenPrismNode.Core.DbSyncModels;

/// <summary>
/// Entitiy for the tx_out postgres table
/// </summary>
public class TransactionOut
{
    public int index { get; set; }

    /// <summary>
    /// Address of the wallet
    /// </summary>
    public string address { get; set; }

    /// <summary>
    /// Reference to the stake address table 
    /// </summary>
    public int stake_address_id { get; set; }

    /// <summary>
    /// Value / Amount
    /// </summary>
    public long value { get; set; }
}